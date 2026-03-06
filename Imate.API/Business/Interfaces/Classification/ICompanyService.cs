using Imate.API.Presentation.RequestModels.Classification;
using Imate.API.Presentation.ResponseModels.Classification;

namespace Imate.API.Business.Interfaces.Classification
{
    public interface ICompanyService
    {
        Task<CompanyResponseModel> CreateCompanyAsync(CreateCompanyRequestModel model);
        Task<CompanyResponseModel?> GetCompanyDetailsAsync(int id);
        Task<CompanyResponseModel?> UpdateCompanyAsync(int id, UpdateCompanyRequestModel model);
        Task<CompanyResponseModel?> SetCompanyStatusAsync(int id, bool isActive);
        Task<PaginatedCompanyResponseModel> GetCompanyListAsync(CompanyListRequestModel request);
    }
}
