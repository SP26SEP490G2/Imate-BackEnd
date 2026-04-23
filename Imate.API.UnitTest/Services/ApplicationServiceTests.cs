using Moq;
using FluentAssertions;
using MediatR;
using Imate.API.Business.Services.Applications;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Applications;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Comunity;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.Applications;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MockQueryable;
using MockQueryable.Moq;
using Xunit;

namespace Imate.API.UnitTest.Services
{
    public class ApplicationServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IAwsS3StorageService> _mockAwsS3Service;
        private readonly Mock<ICommentRepository> _mockCommentRepo;
        private readonly Mock<IVoteRepository> _mockVoteRepo;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ISystemConfigService> _mockSystemConfigService;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IApplicationRepository> _mockApplicationRepo;
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<IBookingRepository> _mockBookingRepo;
        private readonly ApplicationService _service;

        public ApplicationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAwsS3Service = new Mock<IAwsS3StorageService>();
            _mockCommentRepo = new Mock<ICommentRepository>();
            _mockVoteRepo = new Mock<IVoteRepository>();
            _mockMediator = new Mock<IMediator>();
            _mockSystemConfigService = new Mock<ISystemConfigService>();
            _mockAuditLogService = new Mock<IAuditLogService>();

            _mockApplicationRepo = new Mock<IApplicationRepository>();
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockBookingRepo = new Mock<IBookingRepository>();

            _mockUnitOfWork.Setup(u => u.Applications).Returns(_mockApplicationRepo.Object);
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(_mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.Bookings).Returns(_mockBookingRepo.Object);

            // ImateDbContext mock – chỉ cần cho constructor, các test không dùng DbContext trực tiếp
            var options = new DbContextOptionsBuilder<ImateDbContext>().Options;
            var mockContext = new Mock<ImateDbContext>(options);

            _service = new ApplicationService(
                _mockUnitOfWork.Object,
                _mockAwsS3Service.Object,
                _mockCommentRepo.Object,
                _mockVoteRepo.Object,
                mockContext.Object,
                _mockMediator.Object,
                _mockSystemConfigService.Object,
                _mockAuditLogService.Object
            );
        }

        #region View Sent Application (Candidate & Mentor)

        /// Kiểm tra lấy danh sách đơn theo userId: khi user tồn tại và có 2 đơn (TechnicalError + ReportMentor),
        /// hệ thống phải trả về đúng 2 item và gọi repository đúng 1 lần.
        [Fact]
        public async Task GetApplicationsByIdAsync_ShouldReturnApplications_WhenUserExists()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });

            var applications = new List<Application>
            {
                new Application
                {
                    Id = 1, UserId = userId, Title = "Lỗi kỹ thuật",
                    Content = "Không vào được hệ thống",
                    ApplicationType = ApplicationType.TechnicalError,
                    Status = ApplicationStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow,
                    User = new Account { Id = userId, FullName = "User 1" },
                    Reviewer = null
                },
                new Application
                {
                    Id = 2, UserId = userId, Title = "Báo cáo mentor",
                    Content = "Mentor không tham gia buổi học",
                    ApplicationType = ApplicationType.ReportMentor,
                    Status = ApplicationStatus.Approved,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    User = new Account { Id = userId, FullName = "User 1" },
                    Reviewer = new Account { Id = 100, FullName = "Staff 1", AvatarUrl = "avatar.png" }
                }
            }.AsQueryable().BuildMock();

            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);
            var appParams = new ApplicationParams { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetApplicationsByIdAsync(userId, appParams);

            // Assert
            result.Items.Should().HaveCount(2);
            _mockAccountRepo.Verify(r => r.GetByIdAsync(userId), Times.Once);
        }

        /// Kiểm tra lấy danh sách đơn khi userId không tồn tại trong DB:
        /// hệ thống phải throw NotFoundException để ngăn truy cập dữ liệu của user không hợp lệ.
        [Fact]
        public async Task GetApplicationsByIdAsync_ShouldThrowNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Account?)null);

            // Act
            var act = () => _service.GetApplicationsByIdAsync(999, new ApplicationParams());

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        /// Kiểm tra bộ lọc theo trạng thái (Status="Pending"): khi có 2 đơn (Pending + Approved),
        /// hệ thống chỉ trả về 1 đơn có status = Pending.
        [Fact]
        public async Task GetApplicationsByIdAsync_ShouldFilterByStatus_WhenStatusProvided()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });

            var applications = new List<Application>
            {
                new Application { Id = 1, UserId = userId, Content = "A", ApplicationType = ApplicationType.TechnicalError, Status = ApplicationStatus.Pending, CreatedAt = DateTimeOffset.UtcNow, User = new Account { Id = userId }, Reviewer = null },
                new Application { Id = 2, UserId = userId, Content = "B", ApplicationType = ApplicationType.TechnicalError, Status = ApplicationStatus.Approved, CreatedAt = DateTimeOffset.UtcNow, User = new Account { Id = userId }, Reviewer = null }
            }.AsQueryable().BuildMock();

            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);
            var appParams = new ApplicationParams { PageNumber = 1, PageSize = 10, Status = "Pending" };

            // Act
            var result = await _service.GetApplicationsByIdAsync(userId, appParams);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().Status.Should().Be("Pending");
        }

        /// Kiểm tra bộ lọc theo loại đơn (Type="TechnicalError"): khi có 2 đơn khác loại,
        /// hệ thống chỉ trả về đơn có ApplicationType = TechnicalError.
        [Fact]
        public async Task GetApplicationsByIdAsync_ShouldFilterByType_WhenTypeProvided()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });

            var applications = new List<Application>
            {
                new Application { Id = 1, UserId = userId, Content = "A", ApplicationType = ApplicationType.TechnicalError, Status = ApplicationStatus.Pending, CreatedAt = DateTimeOffset.UtcNow, User = new Account { Id = userId }, Reviewer = null },
                new Application { Id = 2, UserId = userId, Content = "B", ApplicationType = ApplicationType.ReportMentor, Status = ApplicationStatus.Pending, CreatedAt = DateTimeOffset.UtcNow, User = new Account { Id = userId }, Reviewer = null }
            }.AsQueryable().BuildMock();

            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);
            var appParams = new ApplicationParams { PageNumber = 1, PageSize = 10, Type = "TechnicalError" };

            // Act
            var result = await _service.GetApplicationsByIdAsync(userId, appParams);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().ApplicationType.Should().Be("TechnicalError");
        }

        /// Kiểm tra tìm kiếm theo nội dung (SearchTerm="đăng nhập"): có 2 đơn với content khác nhau,
        /// hệ thống chỉ trả về đơn có content chứa từ khóa "đăng nhập".
        [Fact]
        public async Task GetApplicationsByIdAsync_ShouldSearchByContent_WhenSearchTermProvided()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });

            var applications = new List<Application>
            {
                new Application { Id = 1, UserId = userId, Content = "Lỗi đăng nhập hệ thống", ApplicationType = ApplicationType.TechnicalError, Status = ApplicationStatus.Pending, CreatedAt = DateTimeOffset.UtcNow, User = new Account { Id = userId }, Reviewer = null },
                new Application { Id = 2, UserId = userId, Content = "Mentor không tham gia", ApplicationType = ApplicationType.ReportMentor, Status = ApplicationStatus.Pending, CreatedAt = DateTimeOffset.UtcNow, User = new Account { Id = userId }, Reviewer = null }
            }.AsQueryable().BuildMock();

            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);
            var appParams = new ApplicationParams { PageNumber = 1, PageSize = 10, SearchTerm = "đăng nhập" };

            // Act
            var result = await _service.GetApplicationsByIdAsync(userId, appParams);

            // Assert
            result.Items.Should().HaveCount(1);
        }

        /// Kiểm tra sắp xếp mặc định theo ngày tạo giảm dần: đơn mới nhất (Id=2) phải nằm ở vị trí đầu tiên
        /// trong danh sách kết quả, đảm bảo hiển thị đơn mới nhất lên trước.
        [Fact]
        public async Task GetApplicationsByIdAsync_ShouldSortByCreatedAtDescByDefault()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });

            var older = DateTimeOffset.UtcNow.AddDays(-2);
            var newer = DateTimeOffset.UtcNow;

            var applications = new List<Application>
            {
                new Application { Id = 1, UserId = userId, Content = "Cũ", ApplicationType = ApplicationType.TechnicalError, Status = ApplicationStatus.Pending, CreatedAt = older, User = new Account { Id = userId }, Reviewer = null },
                new Application { Id = 2, UserId = userId, Content = "Mới", ApplicationType = ApplicationType.TechnicalError, Status = ApplicationStatus.Pending, CreatedAt = newer, User = new Account { Id = userId }, Reviewer = null }
            }.AsQueryable().BuildMock();

            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);
            var appParams = new ApplicationParams { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetApplicationsByIdAsync(userId, appParams);

            // Assert
            result.Items.Should().HaveCount(2);
            result.Items.First().Id.Should().Be(2); // Mới nhất lên đầu
        }

        /// Kiểm tra khi truyền SortBy không hợp lệ ("invalidField"): hệ thống không biết sắp xếp theo trường nào
        /// nên phải throw NotFoundException để báo lỗi cho người dùng.
        [Fact]
        public async Task GetApplicationsByIdAsync_ShouldThrowNotFound_WhenSortByIsInvalid()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });

            var applications = new List<Application>
            {
                new Application { Id = 1, UserId = userId, Content = "A", ApplicationType = ApplicationType.TechnicalError, Status = ApplicationStatus.Pending, CreatedAt = DateTimeOffset.UtcNow, User = new Account { Id = userId }, Reviewer = null }
            }.AsQueryable().BuildMock();

            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);
            var appParams = new ApplicationParams { PageNumber = 1, PageSize = 10, SortBy = "invalidField" };

            // Act
            var act = () => _service.GetApplicationsByIdAsync(userId, appParams);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        /// Kiểm tra bộ lọc theo người review (ReviewId=100): có 2 đơn (1 có reviewer, 1 chưa),
        /// hệ thống chỉ trả về đơn được review bởi reviewer có Id=100.
        [Fact]
        public async Task GetApplicationsByIdAsync_ShouldFilterByReviewerId_WhenReviewIdProvided()
        {
            // Arrange
            var userId = 1;
            var reviewerId = 100;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });

            var applications = new List<Application>
            {
                new Application { Id = 1, UserId = userId, Content = "A", ReviewerId = reviewerId, ApplicationType = ApplicationType.TechnicalError, Status = ApplicationStatus.Approved, CreatedAt = DateTimeOffset.UtcNow, User = new Account { Id = userId }, Reviewer = new Account { Id = reviewerId, FullName = "Staff" } },
                new Application { Id = 2, UserId = userId, Content = "B", ReviewerId = null, ApplicationType = ApplicationType.TechnicalError, Status = ApplicationStatus.Pending, CreatedAt = DateTimeOffset.UtcNow, User = new Account { Id = userId }, Reviewer = null }
            }.AsQueryable().BuildMock();

            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);
            var appParams = new ApplicationParams { PageNumber = 1, PageSize = 10, ReviewId = reviewerId };

            // Act
            var result = await _service.GetApplicationsByIdAsync(userId, appParams);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().Id.Should().Be(1);
        }

        #endregion

        #region Send Technical Application (Candidate & Mentor)

        /// Kiểm tra tạo đơn báo lỗi kỹ thuật thành công: khi user hợp lệ và request có Title + Content,
        /// hệ thống tạo Application mới với status Pending, gọi AddApplication và SaveChangesAsync đúng 1 lần.
        [Fact]
        public async Task CreateTechnicalApplicationAsync_ShouldCreateSuccessfully_WhenValidRequest()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var request = new CreateTechnicalApplicationRequest
            {
                Title = "Lỗi thanh toán",
                Content = "Không thể nạp tiền vào hệ thống"
            };

            // Act
            var result = await _service.CreateTechnicalApplicationAsync(request, userId);

            // Assert
            result.Should().NotBeNull();
            result.Content.Should().Be("Không thể nạp tiền vào hệ thống");
            result.Status.Should().Be("Pending");
            _mockApplicationRepo.Verify(r => r.AddApplication(It.IsAny<Application>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        /// Kiểm tra tạo đơn khi user không tồn tại (userId=999): hệ thống phải throw NotFoundException
        /// với message "Không tìm thấy người dùng." trước khi thực hiện bất kỳ thao tác nào.
        [Fact]
        public async Task CreateTechnicalApplicationAsync_ShouldThrowNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Account?)null);

            var request = new CreateTechnicalApplicationRequest { Content = "Lỗi" };

            // Act
            var act = () => _service.CreateTechnicalApplicationAsync(request, 999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Không tìm thấy người dùng.");
        }

        /// Kiểm tra validation Content rỗng: khi Content = "", hệ thống phải throw BadRequestException
        /// vì mô tả lỗi là trường bắt buộc không được để trống.
        [Fact]
        public async Task CreateTechnicalApplicationAsync_ShouldThrowBadRequest_WhenContentIsEmpty()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });

            var request = new CreateTechnicalApplicationRequest { Content = "" };

            // Act
            var act = () => _service.CreateTechnicalApplicationAsync(request, userId);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Mô tả lỗi không được để trống.");
        }

        /// Kiểm tra validation Content chỉ chứa khoảng trắng ("   "): tương tự Content rỗng,
        /// hệ thống cần từ chối vì whitespace không phải nội dung hợp lệ.
        [Fact]
        public async Task CreateTechnicalApplicationAsync_ShouldThrowBadRequest_WhenContentIsWhitespace()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });

            var request = new CreateTechnicalApplicationRequest { Content = "   " };

            // Act
            var act = () => _service.CreateTechnicalApplicationAsync(request, userId);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>();
        }

        /// Kiểm tra upload file bằng chứng khi tạo đơn: khi request chứa 2 file ảnh (screenshot1.png, screenshot2.png),
        /// hệ thống phải gọi UploadFileAsync lên AWS S3 đúng 2 lần và trả về 2 attachment trong response.
        [Fact]
        public async Task CreateTechnicalApplicationAsync_ShouldUploadEvidenceFiles_WhenProvided()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var mockFile1 = new Mock<IFormFile>();
            mockFile1.Setup(f => f.Length).Returns(1024);
            mockFile1.Setup(f => f.FileName).Returns("screenshot1.png");

            var mockFile2 = new Mock<IFormFile>();
            mockFile2.Setup(f => f.Length).Returns(2048);
            mockFile2.Setup(f => f.FileName).Returns("screenshot2.png");

            _mockAwsS3Service.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "applications"))
                .ReturnsAsync("https://s3.example.com/evidence.png");

            var request = new CreateTechnicalApplicationRequest
            {
                Title = "Lỗi giao diện",
                Content = "Giao diện bị lỗi hiển thị",
                EvidenceFiles = new List<IFormFile> { mockFile1.Object, mockFile2.Object }
            };

            // Act
            var result = await _service.CreateTechnicalApplicationAsync(request, userId);

            // Assert
            result.Should().NotBeNull();
            result.Attachments.Should().HaveCount(2);
            _mockAwsS3Service.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "applications"), Times.Exactly(2));
        }

        /// Kiểm tra Title mặc định khi không truyền: khi Title = null, hệ thống tự động gán Title
        /// bắt đầu bằng "Báo cáo lỗi kỹ thuật" để đảm bảo đơn luôn có tiêu đề.
        [Fact]
        public async Task CreateTechnicalApplicationAsync_ShouldSetDefaultTitle_WhenTitleIsNull()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var request = new CreateTechnicalApplicationRequest
            {
                Title = null,
                Content = "Lỗi không xác định"
            };

            // Act
            var result = await _service.CreateTechnicalApplicationAsync(request, userId);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Pending");
            // AddApplication được gọi với Application có Title mặc định
            _mockApplicationRepo.Verify(r => r.AddApplication(It.Is<Application>(
                a => a.Title.StartsWith("Báo cáo lỗi kỹ thuật")
            )), Times.Once);
        }

        /// Kiểm tra event-driven: sau khi tạo đơn thành công, hệ thống phải publish notification event
        /// (thông qua MediatR) đúng 1 lần để thông báo cho Staff có đơn mới cần xử lý.
        [Fact]
        public async Task CreateTechnicalApplicationAsync_ShouldPublishEvent()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var request = new CreateTechnicalApplicationRequest
            {
                Title = "Test",
                Content = "Mô tả lỗi"
            };

            // Act
            await _service.CreateTechnicalApplicationAsync(request, userId);

            // Assert
            _mockMediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Send Report Application (Candidate & Mentor)

        /// Kiểm tra phân loại đơn tố cáo: khi Candidate tạo report, hệ thống tự động set ApplicationType = ReportMentor
        /// vì Candidate chỉ có thể báo cáo Mentor (không phải ngược lại).
        [Fact]
        public async Task CreateReportApplicationAsync_ShouldCreateAsReportMentor_WhenCandidateReports()
        {
            // Arrange
            var userId = 1;
            var bookingId = 10;
            var account = new Account
            {
                Id = userId,
                AccountRoles = new List<AccountRole>
                {
                    new() { Role = new Role { Name = RoleName.Candidate } }
                }
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(account);
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(bookingId)).ReturnsAsync(new Booking { Id = bookingId });
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Không có đơn trùng lặp
            var emptyApps = new List<Application>().AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(emptyApps);

            var request = new CreateReportApplicationRequest
            {
                Title = "Tố cáo mentor",
                Content = "Mentor không tham gia buổi học",
                BookingId = bookingId
            };

            // Act
            var result = await _service.CreateReportApplicationAsync(request, userId);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Pending");
            _mockApplicationRepo.Verify(r => r.AddApplication(It.Is<Application>(
                a => a.ApplicationType == ApplicationType.ReportMentor
            )), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        /// Kiểm tra phân loại đơn tố cáo: khi Mentor tạo report, hệ thống set ApplicationType = ReportRating
        /// vì Mentor báo cáo rating không trung thực từ candidate.
        [Fact]
        public async Task CreateReportApplicationAsync_ShouldCreateAsReportRating_WhenMentorReports()
        {
            // Arrange
            var userId = 1;
            var bookingId = 10;
            var account = new Account
            {
                Id = userId,
                AccountRoles = new List<AccountRole>
                {
                    new() { Role = new Role { Name = RoleName.Mentor } }
                }
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(account);
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(bookingId)).ReturnsAsync(new Booking { Id = bookingId });
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var emptyApps = new List<Application>().AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(emptyApps);

            var request = new CreateReportApplicationRequest
            {
                Title = "Tố cáo rating",
                Content = "Rating không trung thực",
                BookingId = bookingId
            };

            // Act
            var result = await _service.CreateReportApplicationAsync(request, userId);

            // Assert
            result.Should().NotBeNull();
            _mockApplicationRepo.Verify(r => r.AddApplication(It.Is<Application>(
                a => a.ApplicationType == ApplicationType.ReportRating
            )), Times.Once);
        }

        /// Kiểm tra tạo report khi user không tồn tại: hệ thống phải throw NotFoundException
        /// với message "Không tìm thấy tài khoản." trước khi xử lý bất kỳ logic nào.
        [Fact]
        public async Task CreateReportApplicationAsync_ShouldThrowNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Account?)null);

            var request = new CreateReportApplicationRequest { Content = "Test", BookingId = 1 };

            // Act
            var act = () => _service.CreateReportApplicationAsync(request, 999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Không tìm thấy tài khoản.");
        }

        /// Kiểm tra validation Content rỗng cho report: tương tự đơn kỹ thuật, nội dung báo cáo
        /// không được để trống. Hệ thống phải throw BadRequestException.
        [Fact]
        public async Task CreateReportApplicationAsync_ShouldThrowBadRequest_WhenContentIsEmpty()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account { Id = userId });

            var request = new CreateReportApplicationRequest { Content = "", BookingId = 1 };

            // Act
            var act = () => _service.CreateReportApplicationAsync(request, userId);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Mô tả lỗi không được để trống.");
        }

        /// Kiểm tra khi bookingId không tồn tại (bookingId=999): report phải gắn với booking hợp lệ,
        /// hệ thống throw NotFoundException "Không tìm thấy booking tương ứng.".
        [Fact]
        public async Task CreateReportApplicationAsync_ShouldThrowNotFound_WhenBookingDoesNotExist()
        {
            // Arrange
            var userId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new Account
            {
                Id = userId,
                AccountRoles = new List<AccountRole>()
            });
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(It.IsAny<int>())).ReturnsAsync((Booking?)null);

            var request = new CreateReportApplicationRequest { Content = "Mentor vắng mặt", BookingId = 999 };

            // Act
            var act = () => _service.CreateReportApplicationAsync(request, userId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Không tìm thấy booking tương ứng.");
        }

        /// Kiểm tra chống trùng lặp: khi đã có đơn report Pending cho cùng booking + cùng user,
        /// hệ thống từ chối tạo thêm và throw BadRequestException "đang được xử lý".
        [Fact]
        public async Task CreateReportApplicationAsync_ShouldThrowBadRequest_WhenDuplicatePendingExists()
        {
            // Arrange
            var userId = 1;
            var bookingId = 10;
            var account = new Account
            {
                Id = userId,
                AccountRoles = new List<AccountRole>
                {
                    new() { Role = new Role { Name = RoleName.Candidate } }
                }
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(account);
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(bookingId)).ReturnsAsync(new Booking { Id = bookingId });

            // Đã có đơn Pending cho cùng booking
            var existingApps = new List<Application>
            {
                new Application
                {
                    UserId = userId,
                    BookingId = bookingId,
                    ApplicationType = ApplicationType.ReportMentor,
                    Status = ApplicationStatus.Pending
                }
            }.AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(existingApps);

            var request = new CreateReportApplicationRequest
            {
                Content = "Duplicate report",
                BookingId = bookingId
            };

            // Act
            var act = () => _service.CreateReportApplicationAsync(request, userId);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*đang được xử lý*");
        }

        /// Kiểm tra chống trùng lặp khi đơn đang InReview: tương tự Pending, hệ thống cũng từ chối
        /// tạo đơn mới nếu đã có đơn cùng loại đang trong quá trình xem xét.
        [Fact]
        public async Task CreateReportApplicationAsync_ShouldThrowBadRequest_WhenDuplicateInReviewExists()
        {
            // Arrange
            var userId = 1;
            var bookingId = 10;
            var account = new Account
            {
                Id = userId,
                AccountRoles = new List<AccountRole>
                {
                    new() { Role = new Role { Name = RoleName.Candidate } }
                }
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(account);
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(bookingId)).ReturnsAsync(new Booking { Id = bookingId });

            // Đang InReview
            var existingApps = new List<Application>
            {
                new Application
                {
                    UserId = userId,
                    BookingId = bookingId,
                    ApplicationType = ApplicationType.ReportMentor,
                    Status = ApplicationStatus.InReview
                }
            }.AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(existingApps);

            var request = new CreateReportApplicationRequest { Content = "Duplicate", BookingId = bookingId };

            // Act
            var act = () => _service.CreateReportApplicationAsync(request, userId);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>();
        }

        /// Kiểm tra khi đơn đã được duyệt trước đó: không cho phép tạo report trùng cho booking
        /// đã có đơn Approved. Throw BadRequestException "đã được duyệt".
        [Fact]
        public async Task CreateReportApplicationAsync_ShouldThrowBadRequest_WhenAlreadyApproved()
        {
            // Arrange
            var userId = 1;
            var bookingId = 10;
            var account = new Account
            {
                Id = userId,
                AccountRoles = new List<AccountRole>
                {
                    new() { Role = new Role { Name = RoleName.Candidate } }
                }
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(account);
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(bookingId)).ReturnsAsync(new Booking { Id = bookingId });

            // Đã được duyệt trước đó
            var existingApps = new List<Application>
            {
                new Application
                {
                    UserId = userId,
                    BookingId = bookingId,
                    ApplicationType = ApplicationType.ReportMentor,
                    Status = ApplicationStatus.Approved
                }
            }.AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(existingApps);

            var request = new CreateReportApplicationRequest { Content = "Already processed", BookingId = bookingId };

            // Act
            var act = () => _service.CreateReportApplicationAsync(request, userId);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*đã được duyệt*");
        }

        /// Kiểm tra khi đơn đã bị từ chối: không cho phép tạo lại report cho booking
        /// đã có đơn Rejected. Throw BadRequestException "đã bị từ chối".
        [Fact]
        public async Task CreateReportApplicationAsync_ShouldThrowBadRequest_WhenAlreadyRejected()
        {
            // Arrange
            var userId = 1;
            var bookingId = 10;
            var account = new Account
            {
                Id = userId,
                AccountRoles = new List<AccountRole>
                {
                    new() { Role = new Role { Name = RoleName.Candidate } }
                }
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(account);
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(bookingId)).ReturnsAsync(new Booking { Id = bookingId });

            // Đã bị từ chối
            var existingApps = new List<Application>
            {
                new Application
                {
                    UserId = userId,
                    BookingId = bookingId,
                    ApplicationType = ApplicationType.ReportMentor,
                    Status = ApplicationStatus.Rejected
                }
            }.AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(existingApps);

            var request = new CreateReportApplicationRequest { Content = "Already rejected", BookingId = bookingId };

            // Act
            var act = () => _service.CreateReportApplicationAsync(request, userId);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*đã bị từ chối*");
        }

        /// Kiểm tra upload file bằng chứng cho report: khi đính kèm 1 file, hệ thống gọi
        /// UploadFileAsync lên AWS S3 đúng 1 lần và trả về 1 attachment trong response.
        [Fact]
        public async Task CreateReportApplicationAsync_ShouldUploadEvidenceFiles()
        {
            // Arrange
            var userId = 1;
            var bookingId = 10;
            var account = new Account
            {
                Id = userId,
                AccountRoles = new List<AccountRole>
                {
                    new() { Role = new Role { Name = RoleName.Candidate } }
                }
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(account);
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(bookingId)).ReturnsAsync(new Booking { Id = bookingId });
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var emptyApps = new List<Application>().AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(emptyApps);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            _mockAwsS3Service.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "applications"))
                .ReturnsAsync("https://s3.example.com/evidence.png");

            var request = new CreateReportApplicationRequest
            {
                Content = "Mentor vi phạm",
                BookingId = bookingId,
                EvidenceFiles = new List<IFormFile> { mockFile.Object }
            };

            // Act
            var result = await _service.CreateReportApplicationAsync(request, userId);

            // Assert
            result.Attachments.Should().HaveCount(1);
            _mockAwsS3Service.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "applications"), Times.Once);
        }

        /// Kiểm tra publish event sau khi tạo report: hệ thống gửi notification qua MediatR
        /// để thông báo cho Staff có đơn tố cáo mới cần xử lý.
        [Fact]
        public async Task CreateReportApplicationAsync_ShouldPublishEvent()
        {
            // Arrange
            var userId = 1;
            var bookingId = 10;
            var account = new Account
            {
                Id = userId,
                AccountRoles = new List<AccountRole>
                {
                    new() { Role = new Role { Name = RoleName.Candidate } }
                }
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(account);
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(bookingId)).ReturnsAsync(new Booking { Id = bookingId });
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var emptyApps = new List<Application>().AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(emptyApps);

            var request = new CreateReportApplicationRequest
            {
                Content = "Báo cáo",
                BookingId = bookingId
            };

            // Act
            await _service.CreateReportApplicationAsync(request, userId);

            // Assert
            _mockMediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Review Technical & Report Application (Staff)

        /// Kiểm tra từ chối đơn Pending: Staff từ chối đơn kỹ thuật → status chuyển sang Rejected,
        /// reviewer được gán, response note được lưu, và UpdatedAt được cập nhật.
        [Fact]
        public async Task RejectApplicationAsync_ShouldReject_WhenApplicationIsPending()
        {
            // Arrange
            var applicationId = 1;
            var reviewerId = 100;
            var application = new Application
            {
                Id = applicationId,
                Status = ApplicationStatus.Pending,
                ApplicationType = ApplicationType.TechnicalError,
                Response = null
            };

            var applications = new List<Application> { application }.AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.RejectApplicationAsync(applicationId, reviewerId, "Thông tin không đầy đủ");

            // Assert
            application.Status.Should().Be(ApplicationStatus.Rejected);
            application.ReviewerId.Should().Be(reviewerId);
            application.Response.Should().Be("Thông tin không đầy đủ");
            application.UpdatedAt.Should().NotBeNull();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        /// Kiểm tra từ chối đơn InReview: đơn đang xem xét (ReportMentor) cũng có thể bị từ chối,
        /// status phải chuyển sang Rejected.
        [Fact]
        public async Task RejectApplicationAsync_ShouldReject_WhenApplicationIsInReview()
        {
            // Arrange
            var applicationId = 1;
            var application = new Application
            {
                Id = applicationId,
                Status = ApplicationStatus.InReview,
                ApplicationType = ApplicationType.ReportMentor,
                Response = null
            };

            var applications = new List<Application> { application }.AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.RejectApplicationAsync(applicationId, 100, "Không hợp lệ");

            // Assert
            application.Status.Should().Be(ApplicationStatus.Rejected);
        }

        /// Kiểm tra từ chối đơn không tồn tại (id=999): hệ thống throw NotFoundException
        /// vì không tìm thấy application với id đã cho.
        [Fact]
        public async Task RejectApplicationAsync_ShouldThrowNotFound_WhenApplicationDoesNotExist()
        {
            // Arrange
            var emptyApps = new List<Application>().AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(emptyApps);

            // Act
            var act = () => _service.RejectApplicationAsync(999, 100, "Note");

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        /// Kiểm tra từ chối đơn đã được duyệt: đơn Approved không thể bị reject lại,
        /// throw BadRequestException "Đơn này đã được xử lý.".
        [Fact]
        public async Task RejectApplicationAsync_ShouldThrowBadRequest_WhenAlreadyApproved()
        {
            // Arrange
            var application = new Application
            {
                Id = 1,
                Status = ApplicationStatus.Approved,
                ApplicationType = ApplicationType.TechnicalError
            };

            var applications = new List<Application> { application }.AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);

            // Act
            var act = () => _service.RejectApplicationAsync(1, 100, "Note");

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Đơn này đã được xử lý.");
        }

        /// Kiểm tra từ chối đơn đã bị từ chối trước đó: đơn Rejected không thể reject thêm lần nữa,
        /// hệ thống throw BadRequestException để ngăn xử lý trùng.
        [Fact]
        public async Task RejectApplicationAsync_ShouldThrowBadRequest_WhenAlreadyRejected()
        {
            // Arrange
            var application = new Application
            {
                Id = 1,
                Status = ApplicationStatus.Rejected,
                ApplicationType = ApplicationType.ReportRating
            };

            var applications = new List<Application> { application }.AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);

            // Act
            var act = () => _service.RejectApplicationAsync(1, 100, "Note");

            // Assert
            await act.Should().ThrowAsync<BadRequestException>();
        }

        /// Kiểm tra ghi audit log khi từ chối đơn: hệ thống phải gọi CreateAuditLogAsync
        /// với AuditAction.RejectApplication để lưu vết thao tác của Staff phục vụ kiểm toán.
        [Fact]
        public async Task RejectApplicationAsync_ShouldCreateAuditLog()
        {
            // Arrange
            var applicationId = 1;
            var reviewerId = 100;
            var application = new Application
            {
                Id = applicationId,
                Status = ApplicationStatus.Pending,
                ApplicationType = ApplicationType.ReportMentor,
                Response = null
            };

            var applications = new List<Application> { application }.AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.RejectApplicationAsync(applicationId, reviewerId, "Từ chối");

            // Assert
            _mockAuditLogService.Verify(a => a.CreateAuditLogAsync(
                reviewerId,
                AuditAction.RejectApplication,
                "Application",
                applicationId,
                It.IsAny<object>(),
                It.IsAny<object>()
            ), Times.Once);
        }

        /// Kiểm tra publish event khi từ chối: hệ thống gửi notification qua MediatR
        /// để thông báo cho người gửi đơn biết đơn đã bị từ chối.
        [Fact]
        public async Task RejectApplicationAsync_ShouldPublishRejectedEvent()
        {
            // Arrange
            var application = new Application
            {
                Id = 1,
                Status = ApplicationStatus.Pending,
                ApplicationType = ApplicationType.TechnicalError,
                Response = null
            };

            var applications = new List<Application> { application }.AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.RejectApplicationAsync(1, 100);

            // Assert
            _mockMediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// Kiểm tra response mặc định khi không truyền ghi chú: khi responseNote = null,
        /// hệ thống set Response = "" (empty) thay vì null để tránh lỗi hiển thị trên frontend.
        [Fact]
        public async Task RejectApplicationAsync_ShouldSetEmptyResponse_WhenResponseNoteIsNull()
        {
            // Arrange
            var application = new Application
            {
                Id = 1,
                Status = ApplicationStatus.Pending,
                ApplicationType = ApplicationType.TechnicalError,
                Response = null
            };

            var applications = new List<Application> { application }.AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.RejectApplicationAsync(1, 100, null);

            // Assert
            application.Response.Should().BeEmpty();
        }

        // --- ApproveApplicationAsync: Kiểm tra validation (trước khi vào transaction) ---

        /// Kiểm tra duyệt đơn không tồn tại (id=999): tương tự reject, hệ thống throw NotFoundException
        /// khi không tìm thấy application.
        [Fact]
        public async Task ApproveApplicationAsync_ShouldThrowNotFound_WhenApplicationDoesNotExist()
        {
            // Arrange
            var emptyApps = new List<Application>().AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(emptyApps);

            // Act
            var act = () => _service.ApproveApplicationAsync(999, 100, "Duyệt");

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        /// Kiểm tra duyệt đơn đã được xử lý (status=Approved): không cho phép approve lại,
        /// throw BadRequestException "Đơn này đã được xử lý." để đảm bảo idempotency.
        [Fact]
        public async Task ApproveApplicationAsync_ShouldThrowBadRequest_WhenAlreadyProcessed()
        {
            // Arrange
            var application = new Application
            {
                Id = 1,
                Status = ApplicationStatus.Approved,
                ApplicationType = ApplicationType.TechnicalError
            };

            var applications = new List<Application> { application }.AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);

            // Act
            var act = () => _service.ApproveApplicationAsync(1, 100, "Note");

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Đơn này đã được xử lý.");
        }

        #endregion

        #region GetPendingSummaryAsync (Staff Dashboard)

        /// Kiểm tra dashboard summary: có 5 đơn (2 TechnicalError-Pending, 1 ReportMentor-Pending,
        /// 1 ReportRating-Approved, 1 ReportComment-Pending) → chỉ đếm Pending: Tech=2, Mentor=1, Comment=1.
        [Fact]
        public async Task GetPendingSummaryAsync_ShouldReturnCorrectCounts()
        {
            // Arrange
            var applications = new List<Application>
            {
                new Application { Id = 1, ApplicationType = ApplicationType.TechnicalError, Status = ApplicationStatus.Pending },
                new Application { Id = 2, ApplicationType = ApplicationType.TechnicalError, Status = ApplicationStatus.Pending },
                new Application { Id = 3, ApplicationType = ApplicationType.ReportMentor, Status = ApplicationStatus.Pending },
                new Application { Id = 4, ApplicationType = ApplicationType.ReportRating, Status = ApplicationStatus.Approved }, // Không đếm
                new Application { Id = 5, ApplicationType = ApplicationType.ReportComment, Status = ApplicationStatus.Pending }
            }.AsQueryable().BuildMock();

            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);

            // Act
            var result = await _service.GetPendingSummaryAsync();

            // Assert
            var summaries = result.ToList();
            summaries.Should().Contain(s => s.Type == "TechnicalError" && s.TotalNeedProcess == 2);
            summaries.Should().Contain(s => s.Type == "ReportMentor" && s.TotalNeedProcess == 1);
            summaries.Should().Contain(s => s.Type == "ReportComment" && s.TotalNeedProcess == 1);
        }

        /// Kiểm tra summary khi không có đơn nào: hệ thống vẫn trả về tất cả loại đơn
        /// (TechnicalError, ReportMentor, ReportRating, ReportComment) với TotalNeedProcess = 0.
        [Fact]
        public async Task GetPendingSummaryAsync_ShouldIncludeAllTypesWithZero_WhenNoPendingApplications()
        {
            // Arrange
            var emptyApps = new List<Application>().AsQueryable().BuildMock();
            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(emptyApps);

            // Act
            var result = await _service.GetPendingSummaryAsync();

            // Assert
            var summaries = result.ToList();
            summaries.Should().Contain(s => s.Type == "TechnicalError" && s.TotalNeedProcess == 0);
            summaries.Should().Contain(s => s.Type == "ReportMentor" && s.TotalNeedProcess == 0);
            summaries.Should().Contain(s => s.Type == "ReportRating" && s.TotalNeedProcess == 0);
            summaries.Should().Contain(s => s.Type == "ReportComment" && s.TotalNeedProcess == 0);
        }

        /// Kiểm tra chỉ đếm status Pending: có 3 đơn (Approved, Rejected, InReview) nhưng không có Pending,
        /// tất cả loại đơn trong summary phải có TotalNeedProcess = 0.
        [Fact]
        public async Task GetPendingSummaryAsync_ShouldNotCountNonPendingApplications()
        {
            // Arrange
            var applications = new List<Application>
            {
                new Application { Id = 1, ApplicationType = ApplicationType.TechnicalError, Status = ApplicationStatus.Approved },
                new Application { Id = 2, ApplicationType = ApplicationType.ReportMentor, Status = ApplicationStatus.Rejected },
                new Application { Id = 3, ApplicationType = ApplicationType.ReportRating, Status = ApplicationStatus.InReview }
            }.AsQueryable().BuildMock();

            _mockApplicationRepo.Setup(r => r.GetAllApplications()).Returns(applications);

            // Act
            var result = await _service.GetPendingSummaryAsync();

            // Assert
            var summaries = result.ToList();
            summaries.Should().OnlyContain(s => s.TotalNeedProcess == 0);
        }

        #endregion
    }
}
