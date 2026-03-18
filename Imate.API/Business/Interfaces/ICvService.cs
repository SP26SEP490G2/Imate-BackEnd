using Imate.API.Models.Entities;

namespace Imate.API.Business.Interfaces
{
    public interface ICvService
    {
        Task<UserCv> UploadCvAsync(int accountId, IFormFile file, string fileName);
        Task<List<UserCv>> GetListCvAsync(int accountId);
        Task DeleteCvAsync(int accountId, int cvId);
    }
}
