using Imate.API.Business.Exceptions;
using Imate.API.Business.Interfaces.Staff;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Enums;
using Imate.API.Presentation.ResponseModels.Staff;
using Imate.API.Business.Interfaces;

namespace Imate.API.Business.Services.Staff
{
    public class StaffReviewService : IStaffReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;

        public StaffReviewService(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _auditLogService = auditLogService;
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
            var account = await _unitOfWork.Accounts.GetByIdMentor(accountId)
                ?? throw new NotFoundException("Không tìm thấy tài khoản Mentor.");

            if (account.Status != AccountStatus.PendingVerification)
                throw new BadRequestException("Tài khoản không ở trạng thái chờ duyệt.");

            account.Status = isApproved ? AccountStatus.Active : AccountStatus.Suspended;
            
            await _unitOfWork.Accounts.UpdateAsync(account);
            
            // Log action
            await _auditLogService.CreateAuditLogAsync(staffId, AuditAction.Update, "Mentor", account.Id, 
                new { status = "PendingVerification" }, 
                new { status = account.Status.ToString(), note = note });
            
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ReviewRecruiterApplicationAsync(int accountId, bool isApproved, string? note, int staffId)
        {
            var account = await _unitOfWork.Accounts.GetByIdRecruiter(accountId)
                ?? throw new NotFoundException("Không tìm thấy tài khoản Recruiter.");

            if (account.Status != AccountStatus.PendingVerification)
                throw new BadRequestException("Tài khoản không ở trạng thái chờ duyệt.");

            account.Status = isApproved ? AccountStatus.Active : AccountStatus.Suspended;
            
            if (account.Recruiter != null)
            {
                account.Recruiter.VerificationStatus = isApproved ? VerificationStatus.Verified : VerificationStatus.Rejected;
                _unitOfWork.Recruiters.Update(account.Recruiter);
            }

            await _unitOfWork.Accounts.UpdateAsync(account);

            // Log action
            await _auditLogService.CreateAuditLogAsync(staffId, AuditAction.Update, "Recruiter", account.Id, 
                new { status = "PendingVerification" }, 
                new { status = account.Status.ToString(), note = note });

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
