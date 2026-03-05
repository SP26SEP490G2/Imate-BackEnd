using Imate.API.Business.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Presentation.ResponseModels;

namespace Imate.API.Business.Services.Mentors
{
    public class MentorService : IMentorService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MentorService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<MentorResponse.ListPreviewMentor>> GetListPreviewMentorsAsync()
        {
            try
            {
                return await _unitOfWork.Mentors.GetListPreviewMentorsAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while retrieving mentors.", ex);
            }
        }
    }
}
