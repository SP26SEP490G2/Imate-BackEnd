using Imate.API.Business.Exceptions;
using Imate.API.Business.Interfaces.Recruiter;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Presentation.RequestModels.UserManagement;

namespace Imate.API.Business.Services.Recruiter
{
    public class RecruiterService : IRecruiterService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RecruiterService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            await _unitOfWork.Recruiters.UpdateRecruiterAsync(recruiter);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
