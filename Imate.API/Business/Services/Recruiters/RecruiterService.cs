using Imate.API.Business.Exceptions;
using Imate.API.Business.Interfaces.Recruiters;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.Recruiters;
using Imate.API.Presentation.RequestModels.UserManagement;

namespace Imate.API.Business.Services.Recruiters
{
    public class RecruiterService : IRecruiterService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RecruiterService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task SubmitRecruiterProfileAsync(int accountId, SubmitRecruiterProfileRequest request)
        {
            if (request == null)
                throw new BadRequestException("Dữ liệu hồ sơ Recruiter không hợp lệ.");

            // Lấy account kèm theo navigation Recruiter
            var account = await _unitOfWork.Accounts.GetByIdRecruiter(accountId)
                ?? throw new NotFoundException("Không tìm thấy tài khoản.");

            // Chỉ cho phép tài khoản role Recruiter nộp hồ sơ
            var primaryRole = account.AccountRoles.FirstOrDefault()?.Role.Name;
            if (primaryRole != RoleName.Recruiter)
            {
                throw new BadRequestException("Chỉ tài khoản Recruiter mới có thể nộp hồ sơ Recruiter.");
            }

            // Validate bắt buộc
            if (string.IsNullOrWhiteSpace(request.CompanyName))
                throw new BadRequestException("Tên công ty không được để trống.");
            if (string.IsNullOrWhiteSpace(request.Phone))
                throw new BadRequestException("Số điện thoại không được để trống.");

            if (account.Recruiter == null)
            {
                // Tạo mới hồ sơ Recruiter
                var recruiter = new Recruiter
                {
                    AccountId = account.Id,
                    CompanyName = request.CompanyName.Trim(),
                    Industry = request.Industry?.Trim() ?? "General",
                    CompanySize = request.CompanySize?.Trim(),
                    Website = request.CompanyWebsite?.Trim(),
                    Address = request.CompanyAddress?.Trim(),
                    Phone = request.Phone.Trim(),
                    VerificationStatus = VerificationStatus.Pending
                };

                _unitOfWork.Recruiters.Create(recruiter);
            }
            else
            {
                // Cập nhật hồ sơ đã có
                account.Recruiter.CompanyName = request.CompanyName.Trim();
                account.Recruiter.Industry = request.Industry?.Trim() ?? account.Recruiter.Industry;
                account.Recruiter.CompanySize = request.CompanySize?.Trim() ?? account.Recruiter.CompanySize;
                account.Recruiter.Website = request.CompanyWebsite?.Trim();
                account.Recruiter.Address = request.CompanyAddress?.Trim();
                account.Recruiter.Phone = request.Phone.Trim();

                _unitOfWork.Recruiters.Update(account.Recruiter);
            }

            // Đảm bảo trạng thái account là PendingVerification sau khi nộp hồ sơ
            if (account.Status != AccountStatus.PendingVerification)
            {
                account.Status = AccountStatus.PendingVerification;
                await _unitOfWork.Accounts.UpdateAsync(account);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdataRecruiterrProfileAsync(int accountId, UpdateRecruiterProfileRequest request)
        {
            var recruiter = await _unitOfWork.Recruiters.GetRecruiterByIdAsync(accountId)
                ?? throw new NotFoundException("Không tìm thấy hồ sơ Recruiter.");

            recruiter.CompanyName = request.CompanyName;
            recruiter.CompanyLogo = request.CompanyLogo;
            recruiter.Website = request.Website;
            recruiter.Industry = request.Industry;
            recruiter.CompanySize = request.CompanySize;
            recruiter.Address = request.Address;
            recruiter.Phone = request.Phone;

            await _unitOfWork.Recruiters.UpdateRecruiterAsync(recruiter);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
