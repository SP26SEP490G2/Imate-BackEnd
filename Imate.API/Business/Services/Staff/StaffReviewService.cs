using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces.Staff;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Enums;
using Imate.API.Presentation.ResponseModels.Staff;
using Imate.API.Business.Interfaces;
using Imate.API.Models.Entities;
using MediatR;
using Imate.API.Presentation.SignalR.Events.Staff;

namespace Imate.API.Business.Services.Staff
{
    public class StaffReviewService : IStaffReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly IMediator _mediator;

        public StaffReviewService(IUnitOfWork unitOfWork, IAuditLogService auditLogService, IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _auditLogService = auditLogService;
            _mediator = mediator;
        }

        public async Task<IEnumerable<StaffMentorApplicationResponse>> GetPendingMentorApplicationsAsync()
        {
            var accounts = await _unitOfWork.Accounts.GetPendingMentorAccountsAsync();
            
            return accounts.Select(a => new StaffMentorApplicationResponse
            {
                AccountId = a.Id,
                FullName = a.FullName,
                Email = a.Email,
                AvatarUrl = a.AvatarUrl,
                Bio = a.Mentor?.Bio ?? string.Empty,
                Phone = a.Mentor?.Phone ?? string.Empty,
                BirthDate = a.Mentor?.BirthDate,
                Yoe = a.Mentor?.Yoe ?? 0,
                CvUrl = a.Mentor?.CvUrl,
                CertificateUrl = a.Mentor?.CertificateUrl,
                PricePerSession = a.Mentor?.PricePerSession ?? 0,
                BankAccountHolderName = a.Mentor?.BankAccountHolderName ?? string.Empty,
                BankAccountNumber = a.Mentor?.BankAccountNumber ?? string.Empty,
                BankCode = a.Mentor?.BankCode ?? string.Empty,
                Skills = a.Mentor?.MentorSkills.Select(ms => ms.Skill.Name).ToList() ?? new List<string>(),
                Positions = a.Mentor?.MentorPositions.Select(mp => mp.Position.Name).ToList() ?? new List<string>(),
                Companies = a.Mentor?.MentorCompanies.Select(mc => mc.Company.Name).ToList() ?? new List<string>(),
                CreatedAt = a.CreatedAt
            });
        }

        public async Task<PagedList<StaffMentorApplicationResponse>> GetPendingMentorApplicationsPagedAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            var (accounts, totalCount) = await _unitOfWork.Accounts.GetPendingMentorAccountsPagedAsync(pageNumber, pageSize, searchTerm);
            var items = accounts.Select(a => new StaffMentorApplicationResponse
            {
                AccountId = a.Id,
                FullName = a.FullName,
                Email = a.Email,
                AvatarUrl = a.AvatarUrl,
                Bio = a.Mentor?.Bio ?? string.Empty,
                Phone = a.Mentor?.Phone ?? string.Empty,
                BirthDate = a.Mentor?.BirthDate,
                Yoe = a.Mentor?.Yoe ?? 0,
                CvUrl = a.Mentor?.CvUrl,
                CertificateUrl = a.Mentor?.CertificateUrl,
                PricePerSession = a.Mentor?.PricePerSession ?? 0,
                BankAccountHolderName = a.Mentor?.BankAccountHolderName ?? string.Empty,
                BankAccountNumber = a.Mentor?.BankAccountNumber ?? string.Empty,
                BankCode = a.Mentor?.BankCode ?? string.Empty,
                Skills = a.Mentor?.MentorSkills.Select(ms => ms.Skill.Name).ToList() ?? new List<string>(),
                Positions = a.Mentor?.MentorPositions.Select(mp => mp.Position.Name).ToList() ?? new List<string>(),
                Companies = a.Mentor?.MentorCompanies.Select(mc => mc.Company.Name).ToList() ?? new List<string>(),
                CreatedAt = a.CreatedAt
            }).ToList();
            return new PagedList<StaffMentorApplicationResponse>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<StaffMentorApplicationResponse?> GetMentorApplicationByIdAsync(int id)
        {
            var account = await _unitOfWork.Accounts.GetByIdMentorWithDetailsAsync(id);
            if (account?.Mentor == null)
                return null;
            return new StaffMentorApplicationResponse
            {
                AccountId = account.Id,
                FullName = account.FullName,
                Email = account.Email,
                AvatarUrl = account.AvatarUrl,
                Bio = account.Mentor.Bio ?? string.Empty,
                Phone = account.Mentor.Phone ?? string.Empty,
                BirthDate = account.Mentor.BirthDate,
                Yoe = account.Mentor.Yoe,
                CvUrl = account.Mentor.CvUrl,
                CertificateUrl = account.Mentor.CertificateUrl,
                PricePerSession = account.Mentor.PricePerSession,
                BankAccountHolderName = account.Mentor.BankAccountHolderName ?? string.Empty,
                BankAccountNumber = account.Mentor.BankAccountNumber ?? string.Empty,
                BankCode = account.Mentor.BankCode ?? string.Empty,
                Skills = account.Mentor.MentorSkills?.Select(ms => ms.Skill.Name).ToList() ?? new List<string>(),
                Positions = account.Mentor.MentorPositions?.Select(mp => mp.Position.Name).ToList() ?? new List<string>(),
                Companies = account.Mentor.MentorCompanies?.Select(mc => mc.Company.Name).ToList() ?? new List<string>(),
                CreatedAt = account.CreatedAt
            };
        }

        public async Task<IEnumerable<StaffRecruiterApplicationResponse>> GetPendingRecruiterApplicationsAsync()
        {
            var accounts = await _unitOfWork.Accounts.GetPendingRecruiterAccountsAsync();

            return accounts.Select(a => new StaffRecruiterApplicationResponse
            {
                AccountId = a.Id,
                FullName = a.FullName,
                Email = a.Email,
                AvatarUrl = a.AvatarUrl,
                CompanyName = a.Recruiter?.CompanyName ?? string.Empty,
                CompanyLogo = a.Recruiter?.CompanyLogo,
                Website = a.Recruiter?.Website,
                Industry = a.Recruiter?.Industry ?? string.Empty,
                CompanySize = a.Recruiter?.CompanySize,
                Address = a.Recruiter?.Address,
                Phone = a.Recruiter?.Phone,
                VerificationStatus = a.Recruiter?.VerificationStatus.ToString() ?? string.Empty,
                CreatedAt = a.CreatedAt
            });
        }

        public async Task ReviewMentorApplicationAsync(int accountId, bool isApproved, string? note, int staffId)
        {
            var account = await _unitOfWork.Accounts.GetByIdForStatusUpdateAsync(accountId)
                ?? throw new NotFoundException("Không tìm thấy tài khoản Mentor.");

            var hasMentorRole = account.AccountRoles?.Any(ar => ar.Role?.Name == RoleName.Mentor) == true;
            if (!hasMentorRole)
                throw new BadRequestException("Tài khoản không có vai trò Mentor.");

            if (account.Status != AccountStatus.PendingVerification)
                throw new BadRequestException("Tài khoản không ở trạng thái chờ duyệt.");

            if (isApproved)
            {
                account.Status = AccountStatus.Active;
            }

            var mentor = await _unitOfWork.Mentors.GetMentorByIdAsync(accountId);
            if (mentor != null)
            {
                mentor.VerificationStatus = isApproved ? VerificationStatus.Verified : VerificationStatus.Rejected;
            }

            if (staffId > 0)
            {
                await _auditLogService.CreateAuditLogAsync(staffId, AuditAction.Update, "Mentor", account.Id,
                    new { status = "PendingVerification" },
                    new { status = account.Status.ToString(), note = note });
            }

            await _unitOfWork.SaveChangesAsync();

            // Publish event for notification
            await _mediator.Publish(new MentorReviewCompletedEvent(account, isApproved, note));
        }

        public async Task ReviewRecruiterApplicationAsync(int accountId, bool isApproved, string? note, int staffId, bool createCompany)
        {
            var account = await _unitOfWork.Accounts.GetByIdRecruiter(accountId)
                ?? throw new NotFoundException("Không tìm thấy tài khoản Recruiter.");

            if (account.Status != AccountStatus.PendingVerification)
                throw new BadRequestException("Tài khoản không ở trạng thái chờ duyệt.");

            if (isApproved)
            {
                account.Status = AccountStatus.Active;

                // Tự động tạo công ty nếu được yêu cầu
                if (createCompany && account.Recruiter != null && !string.IsNullOrWhiteSpace(account.Recruiter.CompanyName))
                {
                    var existingCompany = await _unitOfWork.Companies.GetByNameAsync(account.Recruiter.CompanyName);
                    if (existingCompany == null)
                    {
                        var newCompany = new Company
                        {
                            Name = account.Recruiter.CompanyName,
                            ImageUrl = account.Recruiter.CompanyLogo,
                            IsActive = true // Mặc định là active khi staff phê duyệt
                        };
                        await _unitOfWork.Companies.AddAsync(newCompany);
                    }
                }
            }
            
            if (account.Recruiter != null)
            {
                account.Recruiter.VerificationStatus = isApproved ? VerificationStatus.Verified : VerificationStatus.Rejected;
            }

            if (staffId > 0)
            {
                await _auditLogService.CreateAuditLogAsync(staffId, AuditAction.Update, "Recruiter", account.Id, 
                    new { status = "PendingVerification" }, 
                    new { status = account.Status.ToString(), note = note });
            }

            await _unitOfWork.SaveChangesAsync();

            // Publish event for notification
            await _mediator.Publish(new RecruiterReviewCompletedEvent(account, isApproved, note));
        }
    }
}
