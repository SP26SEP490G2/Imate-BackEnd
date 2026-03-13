using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Repositories.Mentors;
using Imate.API.DataAccess.Repositories.QuestionBank;
using Imate.API.DataAccess.Repositories.UserManagement;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Imate.API.DataAccess.Repositories.Recruiters;

namespace Imate.API.DataAccess.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ImateDbContext _repositoryContext;
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
            IMentorRecurringSlotRepository mentorRecurringSlots,
            ITransactionRepository transactions)
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
        public ITransactionRepository Transactions { get; private set; }
        public Task SaveChangesAsync() => _repositoryContext.SaveChangesAsync();
        public Task SaveAsync() => _repositoryContext.SaveChangesAsync();
    }
}
