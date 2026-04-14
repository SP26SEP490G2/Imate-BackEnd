namespace Imate.API.Common.Router
{
    public class APIConfig
    {
        public class Mentor
        {
            public const string GetListPreviewMentors = "get-list-preview-mentors";
            public const string GetMyCandidateRatings = "mentor/my-candidate-ratings";
            public const string UpdatePrice = "mentor/price";
        }

        public class Recruiter
        {
            public const string SubmitRecruiterProfile = "recruiters/submit-profile";
            public const string UploadLogo = "recruiters/upload-logo";
            public const string GetJobApplicationList = "job-applications";
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
            public const string GetSystemQuestionById = "get-system-question-by-id/{questionId}";
            public const string GetContributedQuestionById = "get-contributed-question-by-id/{questionId}";
            public const string GetPublicSystemQuestionBanks = "get-public-system-question-banks";
            public const string GetPublicContributedQuestionBanks = "get-public-contributed-question-banks";
            public const string ContributeQuestion = "contribute-question";
            public const string ExportSystemQuestions = "export-system-questions";
            public const string GetAllPendingContributedQuestionsForStaff = "get-all-pending-contributed-questions-for-staff";
            public const string GetMyContributedQuestions = "get-my-contributed-questions";
            public const string ChangeContributedQuestionStatusForStaff = "change-contributed-question-status-staff/{questionId}";
            public const string ValidateQuestionsFromExcel = "validate-questions-from-excel";
            public const string ImportValidatedQuestions = "import-validated-questions";
            public const string RevalidateSingleQuestion = "revalidate-single-question";
            public const string DownloadQuestionTemplate = "download-question-template";
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

        public class Account
        {
            public const string Profile = "profile";
            public const string MentorProfile = "mentor-profile";
            public const string RecruiterProfile = "recruiter-profile";
        }
        
        public class Subscription
        {
            public const string GetSubscriptionPackages = "subscription-packages";
            public const string GetSubscriptionOverview = "subscription-packages/overview";
            public const string UpdateSubscriptionPackagePrice = "subscription-packages/{id}/price";
        }
        public class CV
        {
            public const string Upload = "cv/upload";
            public const string GetList = "cv/list";
            public const string Delete = "cv/{cvId}";
        }
    }
}
