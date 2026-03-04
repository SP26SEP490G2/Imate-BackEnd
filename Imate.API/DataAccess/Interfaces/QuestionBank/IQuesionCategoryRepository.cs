using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels;
using Imate.API.Presentation.ResponseModels;

namespace Imate.API.DataAccess.Interfaces.QuestionBank
{
    public interface IQuestionCategoryRepository : IRepositoryBase<QuestionCategory>
    {
        Task<IEnumerable<QuestionResponse.QuestionCategoryItem>> GetListQuestionCategoriesAsync();
    }
}
