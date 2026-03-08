namespace Imate.API.Common.Router
{
    public class APIConfig
    {
        public class Mentor
        {
            public const string GetListPreviewMentors = "get-list-preview-mentors";
        }

        public class Recruiter
        {
            public const string SubmitRecruiterProfile = "recruiters/submit-profile";
        }

        public class Question
        {
            public const string GetListHotQuestions = "get-list-hot-questions";
            public const string GetQuestionBankList = "get-question-bank-list";
            public const string GetListQuestionCategories = "get-list-question-categories";
            public const string GetAllSystemQuestionsForStaff = "get-all-system-question-banks-for-staff";
            public const string GetAllContributedQuestionsForStaff = "get-all-contributed-question-banks-for-staff";
            public const string CreateSystemQuestionForStaff = "create-system-question-for-staff";
            public const string UpdateSystemQuestionForStaff = "update-system-question-for-staff/{questionId}";
        }
        public class Position
        {
            public const string GetAllPositions = "get-positions";
        }
        public class Category
        {
            public const string GetAllCategories = "get-categories";
        }
        public class Companies
        {
            public const string GetAllCompanies = "get-companies";
        }
        public class Skills
        {
            public const string GetAllSkills = "get-skills";
        }
        public class Authentication
        {
            public const string RegisterEmail = "register-email";
            public const string LoginEmail = "login-email";
            public const string RegisterLoginWithGoogle = "google";
            public const string RefreshAuthenToken = "refresh-token";
            public const string ChangePassword = "change-password";
            public const string GenerateActionCode = "generate-action-code";
            public const string SendActionEmail = "send-action-email";
            public const string ActionAuthenHandler = "action-handler";
        }

        public class Subscription
        {
            public const string GetSubscriptionPackages = "subscription-packages";
        }
    }
}
