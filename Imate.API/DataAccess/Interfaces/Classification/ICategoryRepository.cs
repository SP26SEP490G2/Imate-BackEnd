using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Classification
{
    public interface ICategoryRepository
    {
        Task<List<int>> GetNonExistingCategoryIdsAsync(IEnumerable<int> categoryIds);
        Task<List<string>> GetNonExistingCategoryNames(IEnumerable<string> categoryNames);
        Task<List<Category>> FindCategoriesByNamesAsync(IEnumerable<string> names);
        IQueryable<Category> GetAllCategories();
        Task<Category> UpdateCategoryAsync(Category category);
        Task<Category> GetCategoryByIdAsync(int id);
        Task<Category> AddCategory(Category category);
        void GetAllCategoriesAsync();
    }
}
