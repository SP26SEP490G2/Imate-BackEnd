using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels;

namespace Imate.API.DataAccess.Repositories.QuestionBank
{
    public class QuestionCategoryRepository : RepositoryBase<QuestionCategory>, IQuestionCategoryRepository
    {
        public QuestionCategoryRepository(ImateDbContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<IEnumerable<QuestionResponse.QuestionCategoryItem>> GetListQuestionCategoriesAsync()
        {
            // TODO: Uncomment this when you have real data
            /*
            return await FindAll(trackChanges: false)
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new QuestionResponse.CategoryItem
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();
            */

            // Fake data for testing
            await Task.Delay(50);

            return new List<QuestionResponse.QuestionCategoryItem>
            {
                new QuestionResponse.QuestionCategoryItem { Id = 1, Name = "Frontend" },
                new QuestionResponse.QuestionCategoryItem { Id = 2, Name = "Backend" },
                new QuestionResponse.QuestionCategoryItem { Id = 3, Name = "System Design" },
                new QuestionResponse.QuestionCategoryItem { Id = 4, Name = "Database" },
                new QuestionResponse.QuestionCategoryItem { Id = 5, Name = "DevOps" },
                new QuestionResponse.QuestionCategoryItem { Id = 6, Name = "Mobile" },
                new QuestionResponse.QuestionCategoryItem { Id = 7, Name = "Security" },
                new QuestionResponse.QuestionCategoryItem { Id = 8, Name = "Algorithm" }
            };
        }
    }
}
