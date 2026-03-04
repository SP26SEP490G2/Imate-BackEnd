using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Models.Entities;
using Imate.API.DataAccess.Repositories;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Presentation.RequestModels;

namespace Imate.API.DataAccess.Interfaces
{
    public interface IUnitOfWork
    {

        IAccountRepository Accounts { get; }
        IBookingRepository Bookings { get; }
        IUserSubscriptionRepository UserSubscriptions { get; }
        IQuestionRepository Questions { get; }       
        IMentorRepository Mentors { get; }
        IQuestionCategoryRepository QuestionCategories { get; }
        Task SaveChangesAsync();
        Task SaveAsync();
    }
}
