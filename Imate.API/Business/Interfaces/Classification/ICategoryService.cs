using Imate.API.Business.Helper;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.Classification;
using Imate.API.Presentation.ResponseModels.Classification;

namespace Imate.API.Business.Interfaces.Classification
{
    public interface ICategoryService
    {
        Task<List<int>> GetNonExistingCategoryIdsAsync(IEnumerable<int> categoryIds);
        Task<PagedList<CategoryResponse>> GetAllCategoryAsync(CommonParams categoryParams);
        Task<Category> UpdateCategoriesAsync(int id, CategoryUpdateRequest category);
        Task<Category> AddCategoriesAsync(CategoryCreateRequest category);
        Task<List<AffectedQuestionResponseModel>> GetAffectedQuestionsAsync(int categoryId, bool willBeActive);
        Task<Category?> SetCategoryStatusAsync(int id, bool isActive);
    }
}
