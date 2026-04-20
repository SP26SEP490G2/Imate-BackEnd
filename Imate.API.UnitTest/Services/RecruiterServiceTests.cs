using Moq;
using Moq;
using FluentAssertions;
using Imate.API.Business.Services.Recruiters;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Business.Exceptions;
using Imate.API.Presentation.RequestModels.Recruiters;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Interfaces.Recruiters;
using MockQueryable.Moq;
using System.Linq.Expressions;
using Xunit;
using MockQueryable;

namespace Imate.API.UnitTest.Services
{
    public class RecruiterServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IAwsS3StorageService> _mockS3Service;
        private readonly RecruiterService _service;

        public RecruiterServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockS3Service = new Mock<IAwsS3StorageService>();
            _service = new RecruiterService(_mockUnitOfWork.Object, _mockAuditLogService.Object, _mockEmailService.Object, _mockS3Service.Object);
        }

        #region Submit Recruiter Application
        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldCreateNewRecruiter_WhenRecruiterDoesNotExist()
        {
            // Arrange
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Recruiter } } }
            };
            var request = new SubmitRecruiterProfileRequest
            {
                CompanyName = "Imate Co",
                Phone = "123456789",
                Industry = "Tech"
            };

            var mockAccountRepo = new Mock<IAccountRepository>();
            var mockRecruiterRepo = new Mock<IRecruiterRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.Recruiters).Returns(mockRecruiterRepo.Object);

            mockAccountRepo.Setup(u => u.GetByIdRecruiter(accountId)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.SubmitRecruiterProfileAsync(accountId, request);

            // Assert
            mockRecruiterRepo.Verify(u => u.Create(It.Is<Recruiter>(r => r.CompanyName == "Imate Co" && r.AccountId == accountId)), Times.Once);
            account.Status.Should().Be(AccountStatus.PendingVerification);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldThrowBadRequest_WhenCompanyNameIsMissing()
        {
            // Arrange
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Recruiter } } }
            };
            var request = new SubmitRecruiterProfileRequest { CompanyName = "", Phone = "123" };

            var mockAccountRepo = new Mock<IAccountRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
            mockAccountRepo.Setup(u => u.GetByIdRecruiter(accountId)).ReturnsAsync(account);

            // Act
            var act = () => _service.SubmitRecruiterProfileAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Tên công ty không được để trống.");
        }

        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldUpdate_WhenRecruiterExists()
        {
            // Arrange
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Recruiter } } },
                Recruiter = new Recruiter { CompanyName = "Old Co", Phone = "000" }
            };
            var request = new SubmitRecruiterProfileRequest
            {
                CompanyName = "Updated Co",
                Phone = "111"
            };

            var mockAccountRepo = new Mock<IAccountRepository>();
            var mockRecruiterRepo = new Mock<IRecruiterRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.Recruiters).Returns(mockRecruiterRepo.Object);
            mockAccountRepo.Setup(u => u.GetByIdRecruiter(accountId)).ReturnsAsync(account);

            // Act
            await _service.SubmitRecruiterProfileAsync(accountId, request);

            // Assert
            account.Recruiter.CompanyName.Should().Be("Updated Co");
            account.Recruiter.Phone.Should().Be("111");
            mockRecruiterRepo.Verify(u => u.Update(account.Recruiter), Times.Once);
        }

        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldThrowBadRequest_WhenRequestIsNull()
        {
            // Act
            var act = () => _service.SubmitRecruiterProfileAsync(1, null!);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Dữ liệu hồ sơ Recruiter không hợp lệ.");
        }

        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldThrowNotFound_WhenAccountDoesNotExist()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(It.IsAny<int>())).ReturnsAsync((Account?)null);

            // Act
            var act = () => _service.SubmitRecruiterProfileAsync(1, new SubmitRecruiterProfileRequest());

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldThrowBadRequest_WhenNotARecruiter()
        {
            // Arrange
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);

            // Act
            var act = () => _service.SubmitRecruiterProfileAsync(1, new SubmitRecruiterProfileRequest());

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Chỉ tài khoản Recruiter mới có thể nộp hồ sơ Recruiter.");
        }

        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldThrowBadRequest_WhenPhoneIsMissing()
        {
            // Arrange
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Recruiter } } } };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);
            var request = new SubmitRecruiterProfileRequest { CompanyName = "Co", Phone = "" };

            // Act
            var act = () => _service.SubmitRecruiterProfileAsync(1, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Số điện thoại không được để trống.");
        }
        #endregion

        #region CreateJobPosting
        [Fact]
        public async Task CreateJobPostAsync_ShouldThrowBadRequest_WhenApplicationDeadlineIsInThePast()
        {
            var request = new CreateUpdateJobRequest { ApplicationDeadline = DateTime.UtcNow.AddDays(-1), MinSalary = 100, MaxSalary = 200 };
            var act = () => _service.CreateJobPostAsync(1, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Dữ liệu hồ sơ Đăng tuyển không hợp lệ.");
        }

        [Fact]
        public async Task CreateJobPostAsync_ShouldThrowBadRequest_WhenMinSalaryHigherThanMaxSalary()
        {
            var request = new CreateUpdateJobRequest { ApplicationDeadline = DateTime.UtcNow.AddDays(10), MinSalary = 200, MaxSalary = 100 };
            var act = () => _service.CreateJobPostAsync(1, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Dữ liệu hồ sơ Đăng tuyển không hợp lệ.");
        }

        [Fact]
        public async Task CreateJobPostAsync_ShouldThrowNotFound_WhenAccountNotFound()
        {
            var request = new CreateUpdateJobRequest { ApplicationDeadline = DateTime.UtcNow.AddDays(10), MinSalary = 100, MaxSalary = 200 };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync((Account?)null);
            var act = () => _service.CreateJobPostAsync(1, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Không tìm thấy tài khoản.");
        }

        [Fact]
        public async Task CreateJobPostAsync_ShouldThrowBadRequest_WhenNotARecruiter()
        {
            var request = new CreateUpdateJobRequest { ApplicationDeadline = DateTime.UtcNow.AddDays(10), MinSalary = 100, MaxSalary = 200 };
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);
            var act = () => _service.CreateJobPostAsync(1, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Chỉ tài khoản Recruiter mới có thể tạo Job.");
        }

        [Fact]
        public async Task CreateJobPostAsync_ShouldCreateJob_Successfully()
        {
            var request = new CreateUpdateJobRequest 
            { 
                ApplicationDeadline = DateTime.UtcNow.AddDays(10), 
                MinSalary = 100, MaxSalary = 200, 
                Title = "Dev", Description = "Desc",
                JobPositions = new List<int> { 1 },
                JobSkills = new List<int> { 2 }
            };
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Recruiter } } } };
            var existingJob = new Job { Id = 10, Title = "Backend Developer", JobSkills = new List<JobSkill> { new JobSkill { SkillId = 1, Skill = new Skill { Name = "C#" } } }, JobPositions = new List<JobPosition> { new JobPosition { PositionId = 1, Position = new Position { Name = "Developer" } } } };
            
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.CreateJobPostAsync(It.IsAny<Job>())).ReturnsAsync(existingJob);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(10)).ReturnsAsync(existingJob);

            var result = await _service.CreateJobPostAsync(1, request);
            
            result.Should().NotBeNull();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _mockAuditLogService.Verify(a => a.CreateAuditLogAsync(1, AuditAction.Create, "Job", 10, It.IsAny<object>(), It.IsAny<object>()), Times.Once);
        }
        #endregion

        #region UpdateJobPosting
        [Fact]
        public async Task UpdateJobPostAsync_ShouldThrowNotFound_WhenJobNotFound()
        {
            var request = new CreateUpdateJobRequest { Id = 1, MinSalary = 100, MaxSalary = 200 };
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync((Job?)null);
            var act = () => _service.UpdateJobPostAsync(1, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Job with Id 1 not found");
        }

        [Fact]
        public async Task UpdateJobPostAsync_ShouldThrowBadRequest_WhenSalaryInvalid()
        {
            var existingJob = new Job { Id = 1, JobSkills = new List<JobSkill> { new JobSkill { SkillId = 1, Skill = new Skill { Name = "C#" } } }, JobPositions = new List<JobPosition> { new JobPosition { PositionId = 1, Position = new Position { Name = "Developer" } } } };
            var request = new CreateUpdateJobRequest { Id = 1, MinSalary = 300, MaxSalary = 200 };
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync(existingJob);
            var act = () => _service.UpdateJobPostAsync(1, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Dữ liệu hồ sơ Đăng tuyển không hợp lệ.");
        }

        [Fact]
        public async Task UpdateJobPostAsync_ShouldThrowNotFound_WhenAccountNotFound()
        {
            var existingJob = new Job { Id = 1, JobSkills = new List<JobSkill> { new JobSkill { SkillId = 1, Skill = new Skill { Name = "C#" } } }, JobPositions = new List<JobPosition> { new JobPosition { PositionId = 1, Position = new Position { Name = "Developer" } } } };
            var request = new CreateUpdateJobRequest { Id = 1, MinSalary = 100, MaxSalary = 200 };
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync(existingJob);
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync((Account?)null);
            
            var act = () => _service.UpdateJobPostAsync(1, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Không tìm thấy tài khoản.");
        }

        [Fact]
        public async Task UpdateJobPostAsync_ShouldThrowBadRequest_WhenRoleNotRecruiter()
        {
            var existingJob = new Job { Id = 1, JobSkills = new List<JobSkill> { new JobSkill { SkillId = 1, Skill = new Skill { Name = "C#" } } }, JobPositions = new List<JobPosition> { new JobPosition { PositionId = 1, Position = new Position { Name = "Developer" } } } };
            var request = new CreateUpdateJobRequest { Id = 1, MinSalary = 100, MaxSalary = 200 };
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } };
            
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync(existingJob);
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);

            var act = () => _service.UpdateJobPostAsync(1, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Chỉ tài khoản Recruiter mới có thể cập nhật Job.");
        }

        [Fact]
        public async Task UpdateJobPostAsync_ShouldUpdate_Successfully()
        {
            var existingJob = new Job { Id = 1, JobSkills = new List<JobSkill> { new JobSkill { SkillId = 1, Skill = new Skill { Name = "C#" } } }, JobPositions = new List<JobPosition> { new JobPosition { PositionId = 1, Position = new Position { Name = "Developer" } } } };
            var request = new CreateUpdateJobRequest 
            { 
                Id = 1, MinSalary = 100, MaxSalary = 200, 
                JobPositions = new List<int> { 1 }, JobSkills = new List<int> { 2 } 
            };
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Recruiter } } } };
            
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync(existingJob);
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.UpdateJobPostAsync(It.IsAny<Job>())).ReturnsAsync(existingJob);

            var result = await _service.UpdateJobPostAsync(1, request);
            
            result.Should().NotBeNull();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _mockAuditLogService.Verify(a => a.CreateAuditLogAsync(1, AuditAction.Update, "Job", 1, It.IsAny<object>(), It.IsAny<object>()), Times.Once);
        }
        #endregion

        #region CloseJobPosting
        [Fact]
        public async Task CloseJobPostAsync_ShouldThrowNotFound_WhenJobNotFound()
        {
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync((Job?)null);
            var act = () => _service.CloseJobPostAsync(1, 1);
            await act.Should().ThrowAsync<Exception>().WithMessage("Job with Id 1 not found");
        }

        [Fact]
        public async Task CloseJobPostAsync_ShouldThrowNotFound_WhenAccountNotFound()
        {
            var existingJob = new Job { Id = 1 };
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync(existingJob);
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync((Account?)null);
            var act = () => _service.CloseJobPostAsync(1, 1);
            await act.Should().ThrowAsync<Exception>().WithMessage("Không tìm thấy tài khoản.");
        }

        [Fact]
        public async Task CloseJobPostAsync_ShouldThrowForbidden_WhenJobDoesNotBelongToRecruiter()
        {
            var existingJob = new Job { Id = 2 };
            var account = new Account { Id = 1 };
            
            var recruiterJobs = new List<Job> { new Job { Id = 1 } }.AsQueryable().BuildMock();
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(2)).ReturnsAsync(existingJob);
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobsByRecruiterId(1)).Returns(recruiterJobs);

            var act = () => _service.CloseJobPostAsync(1, 2);
            await act.Should().ThrowAsync<Exception>().WithMessage("Đơn ứng tuyển này không hợp lệ");
        }

        [Fact]
        public async Task CloseJobPostAsync_ShouldThrowBadRequest_WhenRoleNotRecruiter()
        {
            var existingJob = new Job { Id = 1 };
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } };
            
            var recruiterJobs = new List<Job> { new Job { Id = 1 } }.AsQueryable().BuildMock();
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync(existingJob);
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobsByRecruiterId(1)).Returns(recruiterJobs);

            var act = () => _service.CloseJobPostAsync(1, 1);
            await act.Should().ThrowAsync<Exception>().WithMessage("Chỉ tài khoản Recruiter mới có thể cập nhật Job.");
        }

        [Fact]
        public async Task CloseJobPostAsync_ShouldCloseJob_Successfully()
        {
            var existingJob = new Job { Id = 1, Status = JobStatus.Open };
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Recruiter } } } };
            
            var recruiterJobs = new List<Job> { new Job { Id = 1 } }.AsQueryable().BuildMock();
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync(existingJob);
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobsByRecruiterId(1)).Returns(recruiterJobs);
            _mockUnitOfWork.Setup(u => u.Recruiters.UpdateJobPostAsync(It.IsAny<Job>())).ReturnsAsync(existingJob);

            var result = await _service.CloseJobPostAsync(1, 1);
            
            result.Status.Should().Be(JobStatus.Closed);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
        #endregion

        #region ViewJobPosting
        [Fact]
        public async Task GetListJobRecruiterAsync_ShouldReturnJobs()
        {
            var jobs = new List<Job> 
            { 
                new Job { Id = 1, Title = "Backend Developer", Location = "Thanh Hoa", EmploymentType = "Fulltime", Status = JobStatus.Open, JobSkills = new List<JobSkill> { new JobSkill { SkillId = 1, Skill = new Skill { Name = "C#" } } }, JobPositions = new List<JobPosition> { new JobPosition { PositionId = 1, Position = new Position { Name = "Developer" } } } } 
            }.AsQueryable().BuildMock();
            
            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobsByRecruiterId(1)).Returns(jobs);
            
            var filter = new RecruiterJobSearchFilterRequest { SearchTerm = "Backend Developer", Location = "Thanh Hoa", EmploymentType = "Fulltime", Status = "Open", PageNumber = 1, PageSize = 10 };
            
            var result = await _service.GetListJobRecruiterAsync(1, filter);
            result.TotalCount.Should().Be(1);
            result.Items.First().Id.Should().Be(1);
        }
        
        [Fact]
        public async Task GetListJobRecruiterAsync_ShouldThrowException_OnDbError()
        {
            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobsByRecruiterId(1)).Throws(new System.Exception("DB Error"));
            var act = () => _service.GetListJobRecruiterAsync(1, new RecruiterJobSearchFilterRequest());
            await act.Should().ThrowAsync<ApplicationException>().WithMessage("An error occurred while retrieving Jobs.");
        }


        [Fact]
        public async Task GetJobDetail_ShouldReturnJobDetail()
        {
            var jobs = new List<Job> 
            { 
                new Job 
                { 
                    Id = 1, Title = "Backend Developer", Status = JobStatus.Open,
                    JobSkills = new List<JobSkill>(),
                    JobPositions = new List<JobPosition>(),
                    Recruiter = new Account { Email = "thuan@gmail.com", Recruiter = new Recruiter() }
                } 
            }.AsQueryable().BuildMock();

            _mockUnitOfWork.Setup(u => u.Recruiters.GetAllOpenJobs()).Returns(jobs);

            var result = await _service.GetJobDetail(1);
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
        }
        #endregion

        #region ApplyJobPosting
        [Fact]
        public async Task ApplyJobApplication_ShouldThrowBadRequest_WhenRequestIsNull()
        {
            // Arrange & Act
            var act = () => _service.CreateJobApplication(1, null!);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Dữ liệu hồ sơ Đăng tuyển không hợp lệ.");
        }

        [Fact]
        public async Task ApplyJobApplication_ShouldThrowNotFound_WhenAccountDoesNotExist()
        {
            // Arrange
            var request = new Presentation.RequestModels.JobApplications.CreateJobApplicationRequest { JobId = 1, CVId = 1 };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(1)).ReturnsAsync((Account?)null);

            // Act
            var act = () => _service.CreateJobApplication(1, request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Không tìm thấy tài khoản.");
        }

        [Fact]
        public async Task ApplyJobApplication_ShouldThrowNotFound_WhenJobDoesNotExist()
        {
            // Arrange
            var request = new Presentation.RequestModels.JobApplications.CreateJobApplicationRequest { JobId = 1, CVId = 1 };
            var account = new Account { Id = 1 };

            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(1)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync((Job?)null);

            // Act
            var act = () => _service.CreateJobApplication(1, request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Không tìm thấy công việc.");
        }

        [Fact]
        public async Task ApplyJobApplication_ShouldThrowException_WhenJobDeadlineIsPassed()
        {
            // Arrange
            var request = new Presentation.RequestModels.JobApplications.CreateJobApplicationRequest { JobId = 1, CVId = 1 };
            var account = new Account { Id = 1 };
            var job = new Job { Id = 1, ApplicationDeadline = DateTime.UtcNow.AddDays(-1) };
            var applications = new List<JobApplication>().AsQueryable().BuildMock();

            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(1)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync(job);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetAllJobApplication()).Returns(applications);

            // Act
            var act = () => _service.CreateJobApplication(1, request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Công việc đã hết hạn ứng tuyển");
        }

        [Fact]
        public async Task ApplyJobApplication_ShouldThrowException_WhenAlreadyAppliedAndNotRejected()
        {
            // Arrange
            var request = new Presentation.RequestModels.JobApplications.CreateJobApplicationRequest { JobId = 1, CVId = 1 };
            var account = new Account { Id = 1 };
            var job = new Job { Id = 1, ApplicationDeadline = DateTime.UtcNow.AddDays(10) };

            var existingApplication = new JobApplication 
            { 
                CandidateId = 1, 
                JobId = 1, 
                Status = JobApplicationStatus.Waiting, 
                AppliedDate = DateTime.UtcNow 
            };
            var applications = new List<JobApplication> { existingApplication }.AsQueryable().BuildMock();

            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(1)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync(job);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetAllJobApplication()).Returns(applications);

            // Act
            var act = () => _service.CreateJobApplication(1, request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Bạn đã ứng tuyển công việc này rồi");
        }

        [Fact]
        public async Task ApplyJobApplication_ShouldSucceed_WhenAlreadyAppliedButRejected()
        {
            // Arrange
            var request = new Presentation.RequestModels.JobApplications.CreateJobApplicationRequest { JobId = 1, CVId = 1 };
            var account = new Account { Id = 1 };
            var job = new Job { Id = 1, ApplicationDeadline = DateTime.UtcNow.AddDays(10) };

            var existingApplication = new JobApplication 
            { 
                CandidateId = 1, 
                JobId = 1, 
                Status = JobApplicationStatus.Rejected, 
                AppliedDate = DateTime.UtcNow 
            };
            var applications = new List<JobApplication> { existingApplication }.AsQueryable().BuildMock();

            var newApplication = new JobApplication { Id = 10, CandidateId = 1, JobId = 1, Status = JobApplicationStatus.Waiting };

            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(1)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync(job);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetAllJobApplication()).Returns(applications);
            _mockUnitOfWork.Setup(u => u.Recruiters.CreateJobApplicationAsync(It.IsAny<JobApplication>())).ReturnsAsync(newApplication);

            // Act
            var result = await _service.CreateJobApplication(1, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(10);
            result.Status.Should().Be(JobApplicationStatus.Waiting);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _mockAuditLogService.Verify(a => a.CreateAuditLogAsync(1, AuditAction.Create, "JobApplication", 10, It.IsAny<object>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task ApplyJobApplication_ShouldSucceed_WhenFirstTimeApplying()
        {
            // Arrange
            var request = new Presentation.RequestModels.JobApplications.CreateJobApplicationRequest { JobId = 1, CVId = 1 };
            var account = new Account { Id = 1 };
            var job = new Job { Id = 1, ApplicationDeadline = DateTime.UtcNow.AddDays(10) };
            var applications = new List<JobApplication>().AsQueryable().BuildMock();

            var newApplication = new JobApplication { Id = 10, CandidateId = 1, JobId = 1, Status = JobApplicationStatus.Waiting };

            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(1)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetPostedJobByIdAsync(1)).ReturnsAsync(job);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetAllJobApplication()).Returns(applications);
            _mockUnitOfWork.Setup(u => u.Recruiters.CreateJobApplicationAsync(It.IsAny<JobApplication>())).ReturnsAsync(newApplication);

            // Act
            var result = await _service.CreateJobApplication(1, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(10);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _mockAuditLogService.Verify(a => a.CreateAuditLogAsync(1, AuditAction.Create, "JobApplication", 10, It.IsAny<object>(), It.IsAny<object>()), Times.Once);
        }
        #endregion

        #region ViewAppliedJobApplication
        [Fact]
        public async Task GetCandidateAppliedJob_ShouldReturnApplications_WhenNoFilterApplied()
        {
            var applications = new List<JobApplication>
            {
                new JobApplication
                {
                    Id = 1,
                    Status = JobApplicationStatus.Waiting,
                    Job = new Job
                    {
                        Title = "Backend Developer",
                        EmploymentType = "Fulltime",
                        Location = "HN",
                        Recruiter = new Account
                        {
                            Recruiter = new Recruiter { CompanyName = "Imate", CompanyLogo = "logoA.png" }
                        }
                    }
                }
            }.AsQueryable().BuildMock();

            _mockUnitOfWork.Setup(u => u.Recruiters.GetCandidateAppliedJob(1)).Returns(applications);
            var req = new AppliedApplicationCandidateFilterRequest { PageNumber = 1, PageSize = 10 };

            var result = await _service.GetCandidateAppliedJob(1, req);

            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.First().Title.Should().Be("Backend Developer");
        }

        [Fact]
        public async Task GetCandidateAppliedJob_ShouldFilterBySearchTerm()
        {
            var applications = new List<JobApplication>
            {
                new JobApplication { Id = 1, Job = new Job { Title = "Frontend Developer", Recruiter = new Account { Recruiter = new Recruiter() } } },
                new JobApplication { Id = 2, Job = new Job { Title = "Backend Developer", Recruiter = new Account { Recruiter = new Recruiter() } } }
            }.AsQueryable().BuildMock();

            _mockUnitOfWork.Setup(u => u.Recruiters.GetCandidateAppliedJob(1)).Returns(applications);
            var req = new AppliedApplicationCandidateFilterRequest { SearchTerm = "Back", PageNumber = 1, PageSize = 10 };

            var result = await _service.GetCandidateAppliedJob(1, req);

            result.TotalCount.Should().Be(1);
            result.Items.Single().Id.Should().Be(2);
        }

        [Fact]
        public async Task GetCandidateAppliedJob_ShouldFilterByStatus()
        {
            var applications = new List<JobApplication>
            {
                new JobApplication { Id = 1, Status = JobApplicationStatus.Waiting, Job = new Job { Title = "A", Recruiter = new Account { Recruiter = new Recruiter() } } },
                new JobApplication { Id = 2, Status = JobApplicationStatus.Approved, Job = new Job { Title = "B", Recruiter = new Account { Recruiter = new Recruiter() } } }
            }.AsQueryable().BuildMock();

            _mockUnitOfWork.Setup(u => u.Recruiters.GetCandidateAppliedJob(1)).Returns(applications);
            var req = new AppliedApplicationCandidateFilterRequest { Status = JobApplicationStatus.Approved, PageNumber = 1, PageSize = 10 };

            var result = await _service.GetCandidateAppliedJob(1, req);

            result.TotalCount.Should().Be(1);
            result.Items.Single().Id.Should().Be(2);
        }

        [Fact]
        public async Task GetCandidateAppliedJob_ShouldThrowException_WhenDbFails()
        {
            _mockUnitOfWork.Setup(u => u.Recruiters.GetCandidateAppliedJob(1)).Throws(new Exception("Db error"));
            var req = new AppliedApplicationCandidateFilterRequest();

            var act = () => _service.GetCandidateAppliedJob(1, req);

            await act.Should().ThrowAsync<Exception>().WithMessage("Db error");
        }
        #endregion

        #region ViewAppliedCandidates
        [Fact]
        public async Task GetAppliedCandidateByJobIdAsync_ShouldReturnCandidates_WhenNoFilterApplied()
        {
            var applications = new List<JobApplication>
            {
                new JobApplication
                {
                    Id = 1,
                    CandidateId = 10,
                    Status = JobApplicationStatus.Waiting,
                    Candidate = new Account { FullName = "Thuan", Email = "thuan@test.com" },
                    Cv = new UserCv { FileName = "cv_a.pdf", FileUrl = "http://cv_a.pdf", ScannedData = "{}" }
                }
            }.AsQueryable().BuildMock();

            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobApplicationsListByJobId(1)).Returns(applications);
            var req = new AppliedApplicationCandidateFilterRequest { PageNumber = 1, PageSize = 10 };

            var result = await _service.GetAppliedCandidateByJobIdAsync(1, req);

            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.First().CandidateFullName.Should().Be("Thuan");
        }

        [Fact]
        public async Task GetAppliedCandidateByJobIdAsync_ShouldFilterBySearchTerm_UsingFullNameOrEmail()
        {
            var applications = new List<JobApplication>
            {
                new JobApplication { Id = 1, Candidate = new Account { FullName = "CandA", Email = "testa@test.com" }, Cv = new UserCv() },
                new JobApplication { Id = 2, Candidate = new Account { FullName = "CandB", Email = "kw@test.com" }, Cv = new UserCv() },
                new JobApplication { Id = 3, Candidate = new Account { FullName = "CandKeyword", Email = "testc@test.com" }, Cv = new UserCv() }
            }.AsQueryable().BuildMock();

            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobApplicationsListByJobId(1)).Returns(applications);
            var req = new AppliedApplicationCandidateFilterRequest { SearchTerm = "keyword", PageNumber = 1, PageSize = 10 };
            var reqEmail = new AppliedApplicationCandidateFilterRequest { SearchTerm = "kw@", PageNumber = 1, PageSize = 10 };

            var resName = await _service.GetAppliedCandidateByJobIdAsync(1, req);
            resName.TotalCount.Should().Be(1);
            resName.Items.Single().CandidateFullName.Should().Be("CandKeyword");

            var resEmail = await _service.GetAppliedCandidateByJobIdAsync(1, reqEmail);
            resEmail.TotalCount.Should().Be(1);
            resEmail.Items.Single().CandidateFullName.Should().Be("CandB");
        }

        [Fact]
        public async Task GetAppliedCandidateByJobIdAsync_ShouldFilterByStatus()
        {
            var applications = new List<JobApplication>
            {
                new JobApplication { Id = 1, Status = JobApplicationStatus.Waiting, Candidate = new Account { FullName = "CandA", Email = "testa@test.com" }, Cv = new UserCv() },
                new JobApplication { Id = 2, Status = JobApplicationStatus.Approved, Candidate = new Account { FullName = "CandB", Email = "testb@test.com" }, Cv = new UserCv() },
            }.AsQueryable().BuildMock();

            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobApplicationsListByJobId(1)).Returns(applications);
            var req = new AppliedApplicationCandidateFilterRequest { Status = JobApplicationStatus.Approved, PageNumber = 1, PageSize = 10 };

            var result = await _service.GetAppliedCandidateByJobIdAsync(1, req);
            result.TotalCount.Should().Be(1);
            result.Items.Single().ApplicationId.Should().Be(2);
        }

        [Fact]
        public async Task GetAppliedCandidateByJobIdAsync_ShouldThrowException_WhenDbFails()
        {
            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobApplicationsListByJobId(1)).Throws(new Exception("Db failure async"));
            var req = new AppliedApplicationCandidateFilterRequest();

            var act = () => _service.GetAppliedCandidateByJobIdAsync(1, req);

            await act.Should().ThrowAsync<Exception>().WithMessage("Db failure async");
        }
        #endregion

        #region EditAppliedCandidates
        [Fact]
        public async Task UpdateJobApplication_ShouldThrowNotFound_WhenApplicationDoesNotExist()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobApplicationByIdAsync(1)).ReturnsAsync((JobApplication?)null);
            var req = new UpdateJobApplicationRequest { Id = 1, Status = JobApplicationStatus.Approved };

            // Act
            var act = () => _service.UpdateJobApplication(1, req);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Job with Id 1 not found");
        }

        [Fact]
        public async Task UpdateJobApplication_ShouldThrowNotFound_WhenAccountDoesNotExist()
        {
            // Arrange
            var app = new JobApplication { Id = 1 };
            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobApplicationByIdAsync(1)).ReturnsAsync(app);
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync((Account?)null);

            var req = new UpdateJobApplicationRequest { Id = 1, Status = JobApplicationStatus.Approved };

            // Act
            var act = () => _service.UpdateJobApplication(1, req);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Không tìm thấy tài khoản.");
        }

        [Fact]
        public async Task UpdateJobApplication_ShouldThrowBadRequest_WhenRoleIsNotRecruiter()
        {
            // Arrange
            var app = new JobApplication { Id = 1 };
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new AccountRole { Role = new Role { Name = RoleName.Candidate } } } };

            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobApplicationByIdAsync(1)).ReturnsAsync(app);
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);

            var req = new UpdateJobApplicationRequest { Id = 1, Status = JobApplicationStatus.Approved };

            // Act
            var act = () => _service.UpdateJobApplication(1, req);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Chỉ tài khoản Recruiter mới có thể cập nhật Job.");
        }

        [Fact]
        public async Task UpdateJobApplication_ShouldUpdateToApproved_AndSendEmailSuccessfully()
        {
            // Arrange
            var accountId = 1;
            var account = new Account { Id = accountId, AccountRoles = new List<AccountRole> { new AccountRole { Role = new Role { Name = RoleName.Recruiter } } } };

            var app = new JobApplication 
            { 
                Id = 1, 
                Status = JobApplicationStatus.Waiting, 
                RecruiterFeedback = null,
                Candidate = new Account { FullName = "Hieu Candidate" },
                Job = new Job { Title = "Software Engineer" }
            };

            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobApplicationByIdAsync(1)).ReturnsAsync(app);
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(accountId)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.UpdateJobApplicationStatusAsync(app)).ReturnsAsync(app);

            var req = new UpdateJobApplicationRequest { Id = 1, Status = JobApplicationStatus.Approved };

            // Act
            var result = await _service.UpdateJobApplication(accountId, req);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(JobApplicationStatus.Approved);

            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _mockEmailService.Verify(e => e.SendEmailAsync("startingimate@gmail.com", "Kết quả ứng tuyển của bạn", It.Is<string>(s => s.Contains("Chúc mừng! Hồ sơ của bạn đã được nhà tuyển dụng đánh giá phù hợp"))), Times.Once);
            _mockAuditLogService.Verify(a => a.CreateAuditLogAsync(accountId, AuditAction.Update, "JobApplication", 1, It.IsAny<object>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task UpdateJobApplication_ShouldUpdateToRejected_AndSendEmailSuccessfully()
        {
            // Arrange
            var accountId = 1;
            var account = new Account { Id = accountId, AccountRoles = new List<AccountRole> { new AccountRole { Role = new Role { Name = RoleName.Recruiter } } } };

            var app = new JobApplication 
            { 
                Id = 1, 
                Status = JobApplicationStatus.Waiting, 
                RecruiterFeedback = "Chưa đủ kinh nghiệm",
                Candidate = new Account { FullName = "John" },
                Job = new Job { Title = "PM" }
            };

            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobApplicationByIdAsync(1)).ReturnsAsync(app);
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(accountId)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.UpdateJobApplicationStatusAsync(app)).ReturnsAsync(app);

            var req = new UpdateJobApplicationRequest { Id = 1, Status = JobApplicationStatus.Rejected };

            // Act
            var result = await _service.UpdateJobApplication(accountId, req);

            // Assert
            result.Status.Should().Be(JobApplicationStatus.Rejected);
            _mockEmailService.Verify(e => e.SendEmailAsync("startingimate@gmail.com", "Kết quả ứng tuyển của bạn", It.Is<string>(s => s.Contains("Rất tiếc, hồ sơ của bạn hiện chưa phù hợp với vị trí này"))), Times.Once);
        }

        [Fact]
        public async Task UpdateJobApplication_ShouldUpdateToWaiting_AndSendEmailSuccessfully()
        {
            // Arrange
            var accountId = 1;
            var account = new Account { Id = accountId, AccountRoles = new List<AccountRole> { new AccountRole { Role = new Role { Name = RoleName.Recruiter } } } };

            var app = new JobApplication 
            { 
                Id = 1, 
                Status = JobApplicationStatus.Approved, 
                RecruiterFeedback = null,
                Candidate = new Account { FullName = "Jane" },
                Job = new Job { Title = "Tester" }
            };

            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobApplicationByIdAsync(1)).ReturnsAsync(app);
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(accountId)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.UpdateJobApplicationStatusAsync(app)).ReturnsAsync(app);

            var req = new UpdateJobApplicationRequest { Id = 1, Status = JobApplicationStatus.Waiting };

            // Act
            var result = await _service.UpdateJobApplication(accountId, req);

            // Assert
            result.Status.Should().Be(JobApplicationStatus.Waiting);
            _mockEmailService.Verify(e => e.SendEmailAsync("startingimate@gmail.com", "Kết quả ứng tuyển của bạn", It.Is<string>(s => s.Contains("quá trình được nhà tuyển dụng xem xét"))), Times.Once);
        }
        #endregion
    }
}
