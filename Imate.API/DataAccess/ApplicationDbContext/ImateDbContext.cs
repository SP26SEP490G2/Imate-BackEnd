using Imate.API.Entities;
using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.DataAccess.ApplicationDbContext
{
    public class ImateDbContext : DbContext
    {
        public ImateDbContext(DbContextOptions<ImateDbContext> options)
            : base(options)
        {
        }

        // ─── User Management ──────────────────────────────────────────────────────
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<AccountRole> AccountRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        // ─── Mentor ───────────────────────────────────────────────────────────────
        public DbSet<Mentor> Mentors { get; set; }
        public DbSet<MentorSkill> MentorSkills { get; set; }
        public DbSet<MentorPosition> MentorPositions { get; set; }
        public DbSet<MentorCompany> MentorCompanies { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Slot> Slots { get; set; }
        public DbSet<MentorRecurringSlot> MentorRecurringSlots { get; set; }

        // ─── Recruitment ──────────────────────────────────────────────────────────
        public DbSet<Recruiter> Recruiters { get; set; }
        public DbSet<RecruiterApplication> RecruiterApplications { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<JobSkill> JobSkills { get; set; }
        public DbSet<JobPosition> JobPositions { get; set; }

        // ─── Question Bank ────────────────────────────────────────────────────────
        public DbSet<Question> Questions { get; set; }
        public DbSet<SavedQuestion> SavedQuestions { get; set; }
        public DbSet<ContributedDetail> ContributedDetails { get; set; }

        // ─── Classification ───────────────────────────────────────────────────────
        public DbSet<Category> Categories { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<QuestionCategory> QuestionCategories { get; set; }
        public DbSet<QuestionSkill> QuestionSkills { get; set; }
        public DbSet<QuestionPosition> QuestionPositions { get; set; }

        // ─── Community ────────────────────────────────────────────────────────────
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Vote> Votes { get; set; }

        // ─── Payment ──────────────────────────────────────────────────────────────
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<SubscriptionPackage> SubscriptionPackages { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<WithdrawalDetail> WithdrawalDetails { get; set; }

        // ─── Application & CV ─────────────────────────────────────────────────────
        public DbSet<Application> Applications { get; set; }
        public DbSet<UserCv> UserCvs { get; set; }

        // ─── Interview AI ─────────────────────────────────────────────────────────
        public DbSet<InterviewSession> InterviewSessions { get; set; }
        public DbSet<InterviewResponse> InterviewResponses { get; set; }

        // ─── Practice Test ────────────────────────────────────────────────────────
        public DbSet<PracticeTestSession> PracticeTestSessions { get; set; }
        public DbSet<PracticeTestAnswer> PracticeTestAnswers { get; set; }

        // ─── Notifications & Config ───────────────────────────────────────────────
        public DbSet<SystemNotification> SystemNotifications { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }

        public DbSet<TrainingJourney> TrainingJourneys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Auto-discover and apply all IEntityTypeConfiguration<T> in this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImateDbContext).Assembly);
        }
    }
}
