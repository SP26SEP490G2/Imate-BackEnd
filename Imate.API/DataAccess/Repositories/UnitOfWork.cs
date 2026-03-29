using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Applications;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.DataAccess.Interfaces.Comunity;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Notification;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Repositories.Mentors;
using Imate.API.DataAccess.Repositories.QuestionBank;
using Imate.API.DataAccess.Repositories.Recruiters;
using Imate.API.DataAccess.Repositories.UserManagement;
using Microsoft.EntityFrameworkCore.Storage;

namespace Imate.API.DataAccess.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ImateDbContext _repositoryContext;
        private IDbContextTransaction _transaction;
        public UnitOfWork(
            ImateDbContext repositoryContext,
            IAccountRepository accounts,
            IMentorRepository mentors,
            IRecruiterRepository recruiters,
            IUserSubscriptionRepository userSubscriptions,
            IBookingRepository bookings,
            IQuestionRepository questions,
            ISavedQuestionRepository savedQuestions,
            ICategoryRepository categories,
            IPositionRepository positions,
            ISkillRepository skills,
            ICompanyRepository companies,
            ISlotRepository slots,
            ITransactionRepository transactions,
            ISystemConfigRepository systemConfigs,
            IApplicationRepository applications,
            IMentorRecurringSlotRepository mentorRecurringSlots,
            ISystemNotificationRepository systemNotifications,
            ISubscriptionPackageRepository subscriptionPackages,
            ICommentRepository comments)
        {
            _repositoryContext = repositoryContext;
            Accounts = accounts;
            Mentors = mentors;
            Recruiters = recruiters;
            UserSubscriptions = userSubscriptions;
            Bookings = bookings;
            Questions = questions;
            SavedQuestions = savedQuestions;
            Categories = categories;
            Positions = positions;
            Companies = companies;
            Skills = skills;
            Slots = slots;
            MentorRecurringSlots = mentorRecurringSlots;
            Transactions = transactions;
            Applications = applications;
            SystemConfigs = systemConfigs;
            SystemNotifications = systemNotifications;
            SubscriptionPackages = subscriptionPackages;
            Comments = comments;
        }
        public IAccountRepository Accounts { get; private set; }
        public IMentorRepository Mentors { get; private set; }
        public IRecruiterRepository Recruiters { get; private set; }
        public IUserSubscriptionRepository UserSubscriptions { get; private set; }
        public IBookingRepository Bookings { get; private set; }
        public IQuestionRepository Questions { get; private set; }
        public ISavedQuestionRepository SavedQuestions { get; private set; }
        public ICategoryRepository Categories { get; private set; }
        public IPositionRepository Positions { get; private set; }
        public ISkillRepository Skills { get; private set; }
        public ICompanyRepository Companies { get; private set; }
        public ISlotRepository Slots { get; private set; }
        public IMentorRecurringSlotRepository MentorRecurringSlots { get; private set; }
        public IApplicationRepository Applications { get; private set; }
        public ITransactionRepository Transactions { get; private set; }
        public ISubscriptionPackageRepository SubscriptionPackages { get; private set; }
        public ISystemConfigRepository SystemConfigs { get; }
        public ISystemNotificationRepository SystemNotifications { get; private set; }
        public ICommentRepository Comments { get; private set; }
        public Task SaveChangesAsync() => _repositoryContext.SaveChangesAsync();
        public Task SaveAsync() => _repositoryContext.SaveChangesAsync();
        public void Dispose()
        {
            _transaction?.Dispose();
            _repositoryContext.Dispose();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _repositoryContext.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
}
