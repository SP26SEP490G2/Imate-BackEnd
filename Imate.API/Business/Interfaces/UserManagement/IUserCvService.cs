using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels.UserManagement;

namespace Imate.API.Business.Interfaces.UserManagement
{
    public interface IUserCvService
    {
        Task<UserCvResponseModel> UploadCvAsync(int accountId, UploadCvRequestModel model);
        Task<IEnumerable<UserCvResponseModel>> GetUserCvsAsync(int accountId);
        Task<UserCvResponseModel?> GetCvByIdAsync(int id, int accountId);
        Task DeleteCvAsync(int id, int accountId);
    }
}

