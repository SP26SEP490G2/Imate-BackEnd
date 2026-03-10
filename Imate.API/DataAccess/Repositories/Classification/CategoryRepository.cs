using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.DataAccess.Repositories.Classification
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ImateDbContext _context;

        public CategoryRepository(ImateDbContext context)
        {
            _context = context;
        }
        public async Task<List<int>> GetNonExistingCategoryIdsAsync(IEnumerable<int> categoryIds)
        {
            if (categoryIds == null || !categoryIds.Any())
            {
                return new List<int>();
            }

            var distinctIds = categoryIds.Distinct().ToList();
            var existingIds = await _context.Categories
                .Where(c => distinctIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            var nonExistingIds = distinctIds.Except(existingIds).ToList();

            return nonExistingIds;
        }
        public async Task<List<string>> GetNonExistingCategoryNames(IEnumerable<string> categoryNames)
        {
            if (categoryNames == null || !categoryNames.Any())
            {
                return new List<string>();
            }
            var distinctNames = categoryNames.Select(name => name.Trim().ToLower()).Distinct().ToList();
            var existingNames = await _context.Categories
                .Where(c => distinctNames.Contains(c.Name.ToLower()))
                .Select(c => c.Name.ToLower())
                .ToListAsync();
            var nonExistingNames = distinctNames.Except(existingNames).ToList();
            return nonExistingNames;
        }
        public async Task<List<Category>> FindCategoriesByNamesAsync(IEnumerable<string> names)
        {
            if (names == null || !names.Any())
            {
                return new List<Category>();
            }
            var lowerCaseNames = names.Select(n => n.ToLower()).ToList();

            return await _context.Categories
                .Where(s => lowerCaseNames.Contains(s.Name.ToLower()))
                .ToListAsync();
        }
        public IQueryable<Category> GetAllCategories()
        {
            return _context.Categories.AsNoTracking();
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            await _context.SaveChangesAsync();
            return category;

        }
        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }
        public async Task<Category> AddCategory(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public void GetAllCategoriesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
