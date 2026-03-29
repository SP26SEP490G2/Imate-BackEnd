using Imate.API.DataAccess.Interfaces.Applications;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.DataAccess.Interfaces.Comunity;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Notification;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Repositories;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels;
using Imate.API.Presentation.ResponseModels;

namespace Imate.API.DataAccess.Interfaces
{
    public interface IUnitOfWork
    {
        IAccountRepository Accounts { get; }
        IBookingRepository Bookings { get; }
        IUserSubscriptionRepository UserSubscriptions { get; }
        IQuestionRepository Questions { get; }
        ISavedQuestionRepository SavedQuestions { get; }
        ISubscriptionPackageRepository SubscriptionPackages { get; }
        IMentorRepository Mentors { get; }
        ICategoryRepository Categories { get; }
        IPositionRepository Positions { get; }
        ISkillRepository Skills { get; }
        ICompanyRepository Companies { get; }
        ISlotRepository Slots { get; }
        IMentorRecurringSlotRepository MentorRecurringSlots { get; }
        ITransactionRepository Transactions { get; }
        IRecruiterRepository Recruiters { get; }
        IApplicationRepository Applications { get; }
        ISystemConfigRepository SystemConfigs { get; }
        ISystemNotificationRepository SystemNotifications { get; }
        ICommentRepository Comments { get; }


        Task SaveChangesAsync();
        Task SaveAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
