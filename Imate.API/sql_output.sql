CREATE TABLE [Accounts] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(255) NOT NULL,
    [FullName] nvarchar(255) NOT NULL,
    [AvatarUrl] nvarchar(500) NULL,
    [Provider] nvarchar(50) NOT NULL,
    [ProviderId] nvarchar(255) NULL,
    [Balance] int NOT NULL DEFAULT 0,
    [Status] nvarchar(50) NOT NULL,
    [FreeUsedMock] int NOT NULL DEFAULT 0,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(255) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Companies] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(255) NOT NULL,
    [ImageUrl] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Positions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(255) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Positions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Skills] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(255) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Skills] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Slots] (
    [Id] int NOT NULL IDENTITY,
    [DayOfWeek] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    CONSTRAINT [PK_Slots] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SubscriptionPackages] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(255) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [DurationDays] int NULL,
    [Benefits] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsRecommended] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DailyInterviewLimit] int NULL,
    [TotalInterviewLimit] int NULL,
    [Rank] int NOT NULL,
    CONSTRAINT [PK_SubscriptionPackages] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SystemConfigs] (
    [Id] int NOT NULL IDENTITY,
    [Key] nvarchar(255) NOT NULL,
    [Value] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_SystemConfigs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AuditLogs] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [ActionTime] datetimeoffset NOT NULL,
    [Action] nvarchar(50) NOT NULL,
    [EntityType] nvarchar(100) NOT NULL,
    [EntityId] int NOT NULL,
    [OldValue] nvarchar(max) NULL,
    [NewValue] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuditLogs_Accounts_UserId] FOREIGN KEY ([UserId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Jobs] (
    [Id] int NOT NULL IDENTITY,
    [RecruiterId] int NOT NULL,
    [Title] nvarchar(255) NOT NULL,
    [JobDescription] nvarchar(max) NOT NULL,
    [EmploymentType] nvarchar(100) NOT NULL,
    [Location] nvarchar(255) NOT NULL,
    [MinSalary] bigint NOT NULL,
    [MaxSalary] bigint NOT NULL,
    [ApplicationDeadline] datetimeoffset NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Jobs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Jobs_Accounts_RecruiterId] FOREIGN KEY ([RecruiterId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Mentors] (
    [AccountId] int NOT NULL,
    [Bio] nvarchar(max) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [BirthDate] date NULL,
    [Yoe] int NOT NULL,
    [CvUrl] nvarchar(500) NULL,
    [CertificateUrl] nvarchar(500) NULL,
    [PricePerSession] int NOT NULL,
    [PriceLastUpdatedDate] datetimeoffset NULL,
    [AvgRatings] decimal(5,2) NULL,
    [TotalRatingCount] int NULL,
    [BankAccountHolderName] nvarchar(255) NOT NULL,
    [BankAccountNumber] nvarchar(50) NOT NULL,
    [BankCode] nvarchar(50) NOT NULL,
    [VerificationStatus] int NOT NULL,
    CONSTRAINT [PK_Mentors] PRIMARY KEY ([AccountId]),
    CONSTRAINT [FK_Mentors_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PracticeTestSessions] (
    [Id] int NOT NULL IDENTITY,
    [AccountId] int NOT NULL,
    [TestTitle] nvarchar(255) NOT NULL,
    [TestType] nvarchar(50) NOT NULL,
    [Field] nvarchar(255) NOT NULL,
    [Level] nvarchar(50) NOT NULL,
    [TotalQuestions] int NOT NULL,
    [CorrectAnswers] int NOT NULL,
    [Score] int NOT NULL,
    [TimeLimitMinutes] int NOT NULL,
    [DurationMinutes] int NULL,
    [TechnicalScore] int NULL,
    [LogicalScore] int NULL,
    [OptimizationScore] int NULL,
    [AiFeedback] nvarchar(max) NULL,
    [AiStrengths] nvarchar(max) NULL,
    [AiImprovements] nvarchar(max) NULL,
    [CompletedAt] datetimeoffset NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_PracticeTestSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PracticeTestSessions_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [RecruiterApplications] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [CompanyName] nvarchar(255) NOT NULL,
    [BusinessLicenseUrl] nvarchar(500) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [ReviewerId] int NULL,
    [Notes] nvarchar(max) NULL,
    CONSTRAINT [PK_RecruiterApplications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RecruiterApplications_Accounts_ReviewerId] FOREIGN KEY ([ReviewerId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RecruiterApplications_Accounts_UserId] FOREIGN KEY ([UserId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Recruiters] (
    [AccountId] int NOT NULL,
    [CompanyName] nvarchar(255) NOT NULL,
    [CompanyLogo] nvarchar(500) NULL,
    [Website] nvarchar(500) NULL,
    [Industry] nvarchar(100) NOT NULL,
    [CompanySize] nvarchar(100) NULL,
    [Address] nvarchar(500) NULL,
    [Phone] nvarchar(50) NULL,
    [VerificationStatus] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_Recruiters] PRIMARY KEY ([AccountId]),
    CONSTRAINT [FK_Recruiters_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RefreshTokens] (
    [Id] int NOT NULL IDENTITY,
    [Token] nvarchar(500) NOT NULL,
    [AccountId] int NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [IsRevoked] bit NOT NULL DEFAULT CAST(0 AS bit),
    [RevokedAt] datetimeoffset NULL,
    [IpAddress] nvarchar(50) NULL,
    [UserAgent] nvarchar(500) NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SystemNotifications] (
    [Id] int NOT NULL IDENTITY,
    [RecipientUserId] int NOT NULL,
    [TriggerByUserId] int NULL,
    [Type] nvarchar(100) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Link] nvarchar(500) NULL,
    [IsRead] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_SystemNotifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SystemNotifications_Accounts_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SystemNotifications_Accounts_TriggerByUserId] FOREIGN KEY ([TriggerByUserId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [UserCvs] (
    [Id] int NOT NULL IDENTITY,
    [AccountId] int NOT NULL,
    [FileUrl] nvarchar(500) NOT NULL,
    [FileName] nvarchar(255) NOT NULL,
    [UploadDate] datetimeoffset NOT NULL,
    [ScannedData] nvarchar(max) NOT NULL,
    [AnalysisData] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_UserCvs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserCvs_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ContributedDetails] (
    [Id] int NOT NULL IDENTITY,
    [InterviewDate] date NOT NULL,
    [Level] nvarchar(50) NOT NULL,
    [CompanyId] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_ContributedDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ContributedDetails_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AccountRoles] (
    [AccountId] int NOT NULL,
    [RoleId] int NOT NULL,
    CONSTRAINT [PK_AccountRoles] PRIMARY KEY ([AccountId], [RoleId]),
    CONSTRAINT [FK_AccountRoles_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AccountRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [UserSubscriptions] (
    [Id] int NOT NULL IDENTITY,
    [CandidateId] int NOT NULL,
    [PackageId] int NOT NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NULL,
    [InitialMockLimit] int NOT NULL,
    [MockInterviewUsed] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_UserSubscriptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserSubscriptions_Accounts_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserSubscriptions_SubscriptionPackages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [SubscriptionPackages] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [JobPositions] (
    [JobId] int NOT NULL,
    [PositionId] int NOT NULL,
    CONSTRAINT [PK_JobPositions] PRIMARY KEY ([JobId], [PositionId]),
    CONSTRAINT [FK_JobPositions_Jobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [Jobs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_JobPositions_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [Positions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [JobSkills] (
    [JobId] int NOT NULL,
    [SkillId] int NOT NULL,
    CONSTRAINT [PK_JobSkills] PRIMARY KEY ([JobId], [SkillId]),
    CONSTRAINT [FK_JobSkills_Jobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [Jobs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_JobSkills_Skills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Bookings] (
    [Id] int NOT NULL IDENTITY,
    [CandidateId] int NOT NULL,
    [MentorId] int NOT NULL,
    [StartTime] datetimeoffset NOT NULL,
    [BookDate] date NOT NULL,
    [PriceAtBooking] int NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [AgoraChannelName] nvarchar(255) NOT NULL,
    [AudioRecordKey] nvarchar(500) NULL,
    [RatingScore] int NULL,
    [ReviewText] nvarchar(max) NULL,
    [RatingCreatedAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Bookings_Accounts_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Bookings_Mentors_MentorId] FOREIGN KEY ([MentorId]) REFERENCES [Mentors] ([AccountId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [MentorCompanies] (
    [MentorId] int NOT NULL,
    [CompanyId] int NOT NULL,
    CONSTRAINT [PK_MentorCompanies] PRIMARY KEY ([MentorId], [CompanyId]),
    CONSTRAINT [FK_MentorCompanies_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MentorCompanies_Mentors_MentorId] FOREIGN KEY ([MentorId]) REFERENCES [Mentors] ([AccountId]) ON DELETE CASCADE
);
GO


CREATE TABLE [MentorPositions] (
    [MentorId] int NOT NULL,
    [PositionId] int NOT NULL,
    CONSTRAINT [PK_MentorPositions] PRIMARY KEY ([MentorId], [PositionId]),
    CONSTRAINT [FK_MentorPositions_Mentors_MentorId] FOREIGN KEY ([MentorId]) REFERENCES [Mentors] ([AccountId]) ON DELETE CASCADE,
    CONSTRAINT [FK_MentorPositions_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [Positions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [MentorRecurringSlots] (
    [Id] int NOT NULL IDENTITY,
    [MentorId] int NOT NULL,
    [SlotId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_MentorRecurringSlots] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MentorRecurringSlots_Mentors_MentorId] FOREIGN KEY ([MentorId]) REFERENCES [Mentors] ([AccountId]) ON DELETE CASCADE,
    CONSTRAINT [FK_MentorRecurringSlots_Slots_SlotId] FOREIGN KEY ([SlotId]) REFERENCES [Slots] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [MentorSkills] (
    [MentorId] int NOT NULL,
    [SkillId] int NOT NULL,
    CONSTRAINT [PK_MentorSkills] PRIMARY KEY ([MentorId], [SkillId]),
    CONSTRAINT [FK_MentorSkills_Mentors_MentorId] FOREIGN KEY ([MentorId]) REFERENCES [Mentors] ([AccountId]) ON DELETE CASCADE,
    CONSTRAINT [FK_MentorSkills_Skills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PracticeTestAnswers] (
    [Id] int NOT NULL IDENTITY,
    [PracticeTestSessionId] int NOT NULL,
    [QuestionNumber] int NOT NULL,
    [QuestionText] nvarchar(max) NOT NULL,
    [OptionsJson] nvarchar(max) NOT NULL,
    [CorrectAnswer] nvarchar(10) NOT NULL,
    [UserAnswer] nvarchar(10) NULL,
    [IsCorrect] bit NOT NULL,
    [Explanation] nvarchar(max) NULL,
    CONSTRAINT [PK_PracticeTestAnswers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PracticeTestAnswers_PracticeTestSessions_PracticeTestSessionId] FOREIGN KEY ([PracticeTestSessionId]) REFERENCES [PracticeTestSessions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [JobApplications] (
    [Id] int NOT NULL IDENTITY,
    [JobId] int NOT NULL,
    [CandidateId] int NOT NULL,
    [CvId] int NOT NULL,
    [AppliedDate] datetimeoffset NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [RecruiterFeedback] nvarchar(max) NULL,
    CONSTRAINT [PK_JobApplications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JobApplications_Accounts_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_JobApplications_Jobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [Jobs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_JobApplications_UserCvs_CvId] FOREIGN KEY ([CvId]) REFERENCES [UserCvs] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Questions] (
    [Id] int NOT NULL IDENTITY,
    [Content] nvarchar(max) NOT NULL,
    [Difficulty] nvarchar(50) NULL,
    [IsFromSystem] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatorId] int NOT NULL,
    [SampleAnswer] nvarchar(max) NULL,
    [ContributedDetailId] int NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    [ApprovalStatus] nvarchar(50) NULL,
    CONSTRAINT [PK_Questions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Questions_Accounts_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Questions_ContributedDetails_ContributedDetailId] FOREIGN KEY ([ContributedDetailId]) REFERENCES [ContributedDetails] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [Comments] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [QuestionId] int NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Comments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Comments_Accounts_UserId] FOREIGN KEY ([UserId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Comments_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [InterviewSessions] (
    [Id] int NOT NULL IDENTITY,
    [AccountId] int NOT NULL,
    [UserCvId] int NULL,
    [StartTime] datetimeoffset NOT NULL,
    [EndTime] datetimeoffset NULL,
    [Status] nvarchar(50) NOT NULL,
    [OverallFeedback] nvarchar(max) NULL,
    [InterviewType] nvarchar(50) NOT NULL,
    [QuestionId] int NULL,
    [PositionName] nvarchar(255) NULL,
    [SkillName] nvarchar(255) NULL,
    [LevelName] nvarchar(50) NULL,
    [CompanyName] nvarchar(255) NULL,
    [JobDescriptionText] nvarchar(max) NULL,
    [EstimatedAbility] float NULL,
    [TotalQuestionsAnswered] int NOT NULL DEFAULT 0,
    [CvContent] nvarchar(max) NULL,
    [ExtractedSkillsJson] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_InterviewSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InterviewSessions_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InterviewSessions_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_InterviewSessions_UserCvs_UserCvId] FOREIGN KEY ([UserCvId]) REFERENCES [UserCvs] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [QuestionCategories] (
    [QuestionId] int NOT NULL,
    [CategoryId] int NOT NULL,
    CONSTRAINT [PK_QuestionCategories] PRIMARY KEY ([QuestionId], [CategoryId]),
    CONSTRAINT [FK_QuestionCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_QuestionCategories_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [QuestionPositions] (
    [QuestionId] int NOT NULL,
    [PositionId] int NOT NULL,
    CONSTRAINT [PK_QuestionPositions] PRIMARY KEY ([QuestionId], [PositionId]),
    CONSTRAINT [FK_QuestionPositions_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [Positions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_QuestionPositions_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [QuestionSkills] (
    [QuestionId] int NOT NULL,
    [SkillId] int NOT NULL,
    CONSTRAINT [PK_QuestionSkills] PRIMARY KEY ([QuestionId], [SkillId]),
    CONSTRAINT [FK_QuestionSkills_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_QuestionSkills_Skills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SavedQuestions] (
    [AccountId] int NOT NULL,
    [QuestionId] int NOT NULL,
    CONSTRAINT [PK_SavedQuestions] PRIMARY KEY ([AccountId], [QuestionId]),
    CONSTRAINT [FK_SavedQuestions_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SavedQuestions_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Applications] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [ApplicationType] nvarchar(50) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [Title] nvarchar(255) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [EvidenceUrls] nvarchar(max) NULL,
    [Response] nvarchar(max) NULL,
    [ReviewerId] int NULL,
    [BookingId] int NULL,
    [CommentId] int NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Applications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Applications_Accounts_ReviewerId] FOREIGN KEY ([ReviewerId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Applications_Accounts_UserId] FOREIGN KEY ([UserId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Applications_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Applications_Comments_CommentId] FOREIGN KEY ([CommentId]) REFERENCES [Comments] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Votes] (
    [AccountId] int NOT NULL,
    [CommentId] int NOT NULL,
    [IsUpvote] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Votes] PRIMARY KEY ([AccountId], [CommentId]),
    CONSTRAINT [FK_Votes_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Votes_Comments_CommentId] FOREIGN KEY ([CommentId]) REFERENCES [Comments] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [InterviewResponses] (
    [Id] int NOT NULL IDENTITY,
    [InterviewSessionId] int NOT NULL,
    [TurnNumber] int NOT NULL,
    [QuestionContent] nvarchar(max) NOT NULL,
    [UserAnswer] nvarchar(max) NULL,
    [AnswerTimestamp] datetimeoffset NULL,
    [AIFeedback] nvarchar(max) NULL,
    [SuggestedAnswer] nvarchar(max) NULL,
    [ExpectedBloomLevel] int NULL,
    [DemonstratedBloomLevel] int NULL,
    [BloomScore] float NULL,
    [DifficultyScore] float NULL,
    [CognitiveLoadScore] float NULL,
    [IntrinsicLoad] float NULL,
    [ExtraneousLoad] float NULL,
    [TechnicalDepthScore] float NULL,
    [ProblemSolvingScore] float NULL,
    [CommunicationScore] float NULL,
    [PracticalExperienceScore] float NULL,
    [StarSituationScore] float NULL,
    [StarTaskScore] float NULL,
    [StarActionScore] float NULL,
    [StarResultScore] float NULL,
    [StructuredFeedbackJson] nvarchar(max) NULL,
    [ExpectedAnswerOutline] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_InterviewResponses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InterviewResponses_InterviewSessions_InterviewSessionId] FOREIGN KEY ([InterviewSessionId]) REFERENCES [InterviewSessions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Transactions] (
    [Id] int NOT NULL IDENTITY,
    [SourceAccountId] int NULL,
    [TargetAccountId] int NULL,
    [TransactionType] nvarchar(50) NOT NULL,
    [Amount] int NOT NULL,
    [CommissionRateApplied] decimal(5,2) NULL,
    [BookingId] int NULL,
    [UserSubscriptionId] int NULL,
    [ApplicationId] int NULL,
    [Status] nvarchar(50) NOT NULL,
    [EscrowDeadline] datetimeoffset NULL,
    [ExternalTransactionCode] nvarchar(255) NULL,
    [ReviewerId] int NULL,
    [Reason] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Transactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Transactions_Accounts_ReviewerId] FOREIGN KEY ([ReviewerId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Transactions_Accounts_SourceAccountId] FOREIGN KEY ([SourceAccountId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Transactions_Accounts_TargetAccountId] FOREIGN KEY ([TargetAccountId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Transactions_Applications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [Applications] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Transactions_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Transactions_UserSubscriptions_UserSubscriptionId] FOREIGN KEY ([UserSubscriptionId]) REFERENCES [UserSubscriptions] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [WithdrawalDetails] (
    [TransactionId] int NOT NULL,
    [BankCode] nvarchar(50) NOT NULL,
    [BankAccountHolder] nvarchar(255) NOT NULL,
    [BankAccountNumber] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_WithdrawalDetails] PRIMARY KEY ([TransactionId]),
    CONSTRAINT [FK_WithdrawalDetails_Transactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [Transactions] ([Id]) ON DELETE CASCADE
);
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'IsActive', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Categories]'))
    SET IDENTITY_INSERT [Categories] ON;
INSERT INTO [Categories] ([Id], [CreatedAt], [IsActive], [Name], [UpdatedAt])
VALUES (1, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Behavioral', NULL),
(2, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Technical', NULL),
(3, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'System Design', NULL),
(4, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Coding', NULL),
(5, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Case Study', NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'IsActive', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Categories]'))
    SET IDENTITY_INSERT [Categories] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'IsActive', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Positions]'))
    SET IDENTITY_INSERT [Positions] ON;
INSERT INTO [Positions] ([Id], [CreatedAt], [IsActive], [Name], [UpdatedAt])
VALUES (1, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Backend Developer', NULL),
(2, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Frontend Developer', NULL),
(3, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Fullstack Developer', NULL),
(4, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Mobile Developer', NULL),
(5, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'DevOps Engineer', NULL),
(6, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Data Engineer', NULL),
(7, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'QA Engineer', NULL),
(8, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Business Analyst', NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'IsActive', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Positions]'))
    SET IDENTITY_INSERT [Positions] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [CreatedAt], [Name], [UpdatedAt])
VALUES (1, '2024-01-01T00:00:00.0000000+00:00', N'Candidate', NULL),
(2, '2024-01-01T00:00:00.0000000+00:00', N'Mentor', NULL),
(3, '2024-01-01T00:00:00.0000000+00:00', N'Recruiter', NULL),
(4, '2024-01-01T00:00:00.0000000+00:00', N'Staff', NULL),
(5, '2024-01-01T00:00:00.0000000+00:00', N'Admin', NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'IsActive', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Skills]'))
    SET IDENTITY_INSERT [Skills] ON;
INSERT INTO [Skills] ([Id], [CreatedAt], [IsActive], [Name], [UpdatedAt])
VALUES (1, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'C#', NULL),
(2, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Java', NULL),
(3, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Python', NULL),
(4, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'JavaScript', NULL),
(5, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'TypeScript', NULL),
(6, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'React', NULL),
(7, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Angular', NULL),
(8, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'.NET', NULL),
(9, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'SQL', NULL),
(10, '2024-01-01T00:00:00.0000000+00:00', CAST(1 AS bit), N'Docker', NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'IsActive', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Skills]'))
    SET IDENTITY_INSERT [Skills] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DayOfWeek', N'EndTime', N'StartTime') AND [object_id] = OBJECT_ID(N'[Slots]'))
    SET IDENTITY_INSERT [Slots] ON;
INSERT INTO [Slots] ([Id], [DayOfWeek], [EndTime], [StartTime])
VALUES (1, 0, '09:00:00', '08:00:00'),
(2, 0, '10:00:00', '09:00:00'),
(3, 0, '11:00:00', '10:00:00'),
(4, 0, '12:00:00', '11:00:00'),
(5, 0, '13:00:00', '12:00:00'),
(6, 0, '14:00:00', '13:00:00'),
(7, 0, '15:00:00', '14:00:00'),
(8, 0, '16:00:00', '15:00:00'),
(9, 0, '17:00:00', '16:00:00'),
(10, 0, '18:00:00', '17:00:00'),
(11, 0, '19:00:00', '18:00:00'),
(12, 0, '20:00:00', '19:00:00'),
(13, 0, '21:00:00', '20:00:00'),
(14, 0, '22:00:00', '21:00:00'),
(15, 1, '09:00:00', '08:00:00'),
(16, 1, '10:00:00', '09:00:00'),
(17, 1, '11:00:00', '10:00:00'),
(18, 1, '12:00:00', '11:00:00'),
(19, 1, '13:00:00', '12:00:00'),
(20, 1, '14:00:00', '13:00:00'),
(21, 1, '15:00:00', '14:00:00'),
(22, 1, '16:00:00', '15:00:00'),
(23, 1, '17:00:00', '16:00:00'),
(24, 1, '18:00:00', '17:00:00'),
(25, 1, '19:00:00', '18:00:00'),
(26, 1, '20:00:00', '19:00:00'),
(27, 1, '21:00:00', '20:00:00'),
(28, 1, '22:00:00', '21:00:00'),
(29, 2, '09:00:00', '08:00:00'),
(30, 2, '10:00:00', '09:00:00'),
(31, 2, '11:00:00', '10:00:00'),
(32, 2, '12:00:00', '11:00:00'),
(33, 2, '13:00:00', '12:00:00'),
(34, 2, '14:00:00', '13:00:00'),
(35, 2, '15:00:00', '14:00:00'),
(36, 2, '16:00:00', '15:00:00'),
(37, 2, '17:00:00', '16:00:00'),
(38, 2, '18:00:00', '17:00:00'),
(39, 2, '19:00:00', '18:00:00'),
(40, 2, '20:00:00', '19:00:00'),
(41, 2, '21:00:00', '20:00:00'),
(42, 2, '22:00:00', '21:00:00');
INSERT INTO [Slots] ([Id], [DayOfWeek], [EndTime], [StartTime])
VALUES (43, 3, '09:00:00', '08:00:00'),
(44, 3, '10:00:00', '09:00:00'),
(45, 3, '11:00:00', '10:00:00'),
(46, 3, '12:00:00', '11:00:00'),
(47, 3, '13:00:00', '12:00:00'),
(48, 3, '14:00:00', '13:00:00'),
(49, 3, '15:00:00', '14:00:00'),
(50, 3, '16:00:00', '15:00:00'),
(51, 3, '17:00:00', '16:00:00'),
(52, 3, '18:00:00', '17:00:00'),
(53, 3, '19:00:00', '18:00:00'),
(54, 3, '20:00:00', '19:00:00'),
(55, 3, '21:00:00', '20:00:00'),
(56, 3, '22:00:00', '21:00:00'),
(57, 4, '09:00:00', '08:00:00'),
(58, 4, '10:00:00', '09:00:00'),
(59, 4, '11:00:00', '10:00:00'),
(60, 4, '12:00:00', '11:00:00'),
(61, 4, '13:00:00', '12:00:00'),
(62, 4, '14:00:00', '13:00:00'),
(63, 4, '15:00:00', '14:00:00'),
(64, 4, '16:00:00', '15:00:00'),
(65, 4, '17:00:00', '16:00:00'),
(66, 4, '18:00:00', '17:00:00'),
(67, 4, '19:00:00', '18:00:00'),
(68, 4, '20:00:00', '19:00:00'),
(69, 4, '21:00:00', '20:00:00'),
(70, 4, '22:00:00', '21:00:00'),
(71, 5, '09:00:00', '08:00:00'),
(72, 5, '10:00:00', '09:00:00'),
(73, 5, '11:00:00', '10:00:00'),
(74, 5, '12:00:00', '11:00:00'),
(75, 5, '13:00:00', '12:00:00'),
(76, 5, '14:00:00', '13:00:00'),
(77, 5, '15:00:00', '14:00:00'),
(78, 5, '16:00:00', '15:00:00'),
(79, 5, '17:00:00', '16:00:00'),
(80, 5, '18:00:00', '17:00:00'),
(81, 5, '19:00:00', '18:00:00'),
(82, 5, '20:00:00', '19:00:00'),
(83, 5, '21:00:00', '20:00:00'),
(84, 5, '22:00:00', '21:00:00');
INSERT INTO [Slots] ([Id], [DayOfWeek], [EndTime], [StartTime])
VALUES (85, 6, '09:00:00', '08:00:00'),
(86, 6, '10:00:00', '09:00:00'),
(87, 6, '11:00:00', '10:00:00'),
(88, 6, '12:00:00', '11:00:00'),
(89, 6, '13:00:00', '12:00:00'),
(90, 6, '14:00:00', '13:00:00'),
(91, 6, '15:00:00', '14:00:00'),
(92, 6, '16:00:00', '15:00:00'),
(93, 6, '17:00:00', '16:00:00'),
(94, 6, '18:00:00', '17:00:00'),
(95, 6, '19:00:00', '18:00:00'),
(96, 6, '20:00:00', '19:00:00'),
(97, 6, '21:00:00', '20:00:00'),
(98, 6, '22:00:00', '21:00:00');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DayOfWeek', N'EndTime', N'StartTime') AND [object_id] = OBJECT_ID(N'[Slots]'))
    SET IDENTITY_INSERT [Slots] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Benefits', N'DailyInterviewLimit', N'DurationDays', N'IsActive', N'Name', N'Price', N'Rank', N'TotalInterviewLimit') AND [object_id] = OBJECT_ID(N'[SubscriptionPackages]'))
    SET IDENTITY_INSERT [SubscriptionPackages] ON;
INSERT INTO [SubscriptionPackages] ([Id], [Benefits], [DailyInterviewLimit], [DurationDays], [IsActive], [Name], [Price], [Rank], [TotalInterviewLimit])
VALUES (1, N'{"features":["1 mock interview per month","Basic resume feedback"]}', NULL, NULL, CAST(1 AS bit), N'Free', 0.0, 0, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Benefits', N'DailyInterviewLimit', N'DurationDays', N'IsActive', N'Name', N'Price', N'Rank', N'TotalInterviewLimit') AND [object_id] = OBJECT_ID(N'[SubscriptionPackages]'))
    SET IDENTITY_INSERT [SubscriptionPackages] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Benefits', N'DailyInterviewLimit', N'DurationDays', N'IsActive', N'IsRecommended', N'Name', N'Price', N'Rank', N'TotalInterviewLimit') AND [object_id] = OBJECT_ID(N'[SubscriptionPackages]'))
    SET IDENTITY_INSERT [SubscriptionPackages] ON;
INSERT INTO [SubscriptionPackages] ([Id], [Benefits], [DailyInterviewLimit], [DurationDays], [IsActive], [IsRecommended], [Name], [Price], [Rank], [TotalInterviewLimit])
VALUES (2, N'{"features":["Unlimited mock interviews","AI career assistant","Detailed feedback reports"]}', NULL, 30, CAST(1 AS bit), CAST(1 AS bit), N'Premium', 199000.0, 0, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Benefits', N'DailyInterviewLimit', N'DurationDays', N'IsActive', N'IsRecommended', N'Name', N'Price', N'Rank', N'TotalInterviewLimit') AND [object_id] = OBJECT_ID(N'[SubscriptionPackages]'))
    SET IDENTITY_INSERT [SubscriptionPackages] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Benefits', N'DailyInterviewLimit', N'DurationDays', N'IsActive', N'Name', N'Price', N'Rank', N'TotalInterviewLimit') AND [object_id] = OBJECT_ID(N'[SubscriptionPackages]'))
    SET IDENTITY_INSERT [SubscriptionPackages] ON;
INSERT INTO [SubscriptionPackages] ([Id], [Benefits], [DailyInterviewLimit], [DurationDays], [IsActive], [Name], [Price], [Rank], [TotalInterviewLimit])
VALUES (3, N'{"features":["All Premium features","1-on-1 expert coaching session","Priority support"]}', NULL, 90, CAST(1 AS bit), N'Enterprise', 499000.0, 0, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Benefits', N'DailyInterviewLimit', N'DurationDays', N'IsActive', N'Name', N'Price', N'Rank', N'TotalInterviewLimit') AND [object_id] = OBJECT_ID(N'[SubscriptionPackages]'))
    SET IDENTITY_INSERT [SubscriptionPackages] OFF;
GO


CREATE INDEX [IX_AccountRoles_RoleId] ON [AccountRoles] ([RoleId]);
GO


CREATE UNIQUE INDEX [IX_Accounts_Email] ON [Accounts] ([Email]);
GO


CREATE INDEX [IX_Applications_BookingId] ON [Applications] ([BookingId]);
GO


CREATE INDEX [IX_Applications_CommentId] ON [Applications] ([CommentId]);
GO


CREATE INDEX [IX_Applications_ReviewerId] ON [Applications] ([ReviewerId]);
GO


CREATE INDEX [IX_Applications_UserId] ON [Applications] ([UserId]);
GO


CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
GO


CREATE INDEX [IX_Bookings_CandidateId] ON [Bookings] ([CandidateId]);
GO


CREATE INDEX [IX_Bookings_MentorId] ON [Bookings] ([MentorId]);
GO


CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
GO


CREATE INDEX [IX_Comments_QuestionId] ON [Comments] ([QuestionId]);
GO


CREATE INDEX [IX_Comments_UserId] ON [Comments] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_Companies_Name] ON [Companies] ([Name]);
GO


CREATE INDEX [IX_ContributedDetails_CompanyId] ON [ContributedDetails] ([CompanyId]);
GO


CREATE INDEX [IX_InterviewResponses_InterviewSessionId] ON [InterviewResponses] ([InterviewSessionId]);
GO


CREATE INDEX [IX_InterviewSessions_AccountId] ON [InterviewSessions] ([AccountId]);
GO


CREATE INDEX [IX_InterviewSessions_QuestionId] ON [InterviewSessions] ([QuestionId]);
GO


CREATE INDEX [IX_InterviewSessions_UserCvId] ON [InterviewSessions] ([UserCvId]);
GO


CREATE INDEX [IX_JobApplications_CandidateId] ON [JobApplications] ([CandidateId]);
GO


CREATE INDEX [IX_JobApplications_CvId] ON [JobApplications] ([CvId]);
GO


CREATE INDEX [IX_JobApplications_JobId] ON [JobApplications] ([JobId]);
GO


CREATE INDEX [IX_JobPositions_PositionId] ON [JobPositions] ([PositionId]);
GO


CREATE INDEX [IX_Jobs_RecruiterId] ON [Jobs] ([RecruiterId]);
GO


CREATE INDEX [IX_JobSkills_SkillId] ON [JobSkills] ([SkillId]);
GO


CREATE INDEX [IX_MentorCompanies_CompanyId] ON [MentorCompanies] ([CompanyId]);
GO


CREATE INDEX [IX_MentorPositions_PositionId] ON [MentorPositions] ([PositionId]);
GO


CREATE INDEX [IX_MentorRecurringSlots_MentorId] ON [MentorRecurringSlots] ([MentorId]);
GO


CREATE INDEX [IX_MentorRecurringSlots_SlotId] ON [MentorRecurringSlots] ([SlotId]);
GO


CREATE INDEX [IX_MentorSkills_SkillId] ON [MentorSkills] ([SkillId]);
GO


CREATE UNIQUE INDEX [IX_Positions_Name] ON [Positions] ([Name]);
GO


CREATE INDEX [IX_PracticeTestAnswers_PracticeTestSessionId] ON [PracticeTestAnswers] ([PracticeTestSessionId]);
GO


CREATE INDEX [IX_PracticeTestSessions_AccountId] ON [PracticeTestSessions] ([AccountId]);
GO


CREATE INDEX [IX_QuestionCategories_CategoryId] ON [QuestionCategories] ([CategoryId]);
GO


CREATE INDEX [IX_QuestionPositions_PositionId] ON [QuestionPositions] ([PositionId]);
GO


CREATE INDEX [IX_Questions_ContributedDetailId] ON [Questions] ([ContributedDetailId]);
GO


CREATE INDEX [IX_Questions_CreatorId] ON [Questions] ([CreatorId]);
GO


CREATE INDEX [IX_QuestionSkills_SkillId] ON [QuestionSkills] ([SkillId]);
GO


CREATE INDEX [IX_RecruiterApplications_ReviewerId] ON [RecruiterApplications] ([ReviewerId]);
GO


CREATE INDEX [IX_RecruiterApplications_UserId] ON [RecruiterApplications] ([UserId]);
GO


CREATE INDEX [IX_RefreshTokens_AccountId] ON [RefreshTokens] ([AccountId]);
GO


CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
GO


CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
GO


CREATE INDEX [IX_SavedQuestions_QuestionId] ON [SavedQuestions] ([QuestionId]);
GO


CREATE UNIQUE INDEX [IX_Skills_Name] ON [Skills] ([Name]);
GO


CREATE UNIQUE INDEX [IX_SystemConfigs_Key] ON [SystemConfigs] ([Key]);
GO


CREATE INDEX [IX_SystemNotifications_RecipientUserId] ON [SystemNotifications] ([RecipientUserId]);
GO


CREATE INDEX [IX_SystemNotifications_TriggerByUserId] ON [SystemNotifications] ([TriggerByUserId]);
GO


CREATE INDEX [IX_Transactions_ApplicationId] ON [Transactions] ([ApplicationId]);
GO


CREATE INDEX [IX_Transactions_BookingId] ON [Transactions] ([BookingId]);
GO


CREATE INDEX [IX_Transactions_ReviewerId] ON [Transactions] ([ReviewerId]);
GO


CREATE INDEX [IX_Transactions_SourceAccountId] ON [Transactions] ([SourceAccountId]);
GO


CREATE INDEX [IX_Transactions_TargetAccountId] ON [Transactions] ([TargetAccountId]);
GO


CREATE UNIQUE INDEX [IX_Transactions_UserSubscriptionId] ON [Transactions] ([UserSubscriptionId]) WHERE [UserSubscriptionId] IS NOT NULL;
GO


CREATE INDEX [IX_UserCvs_AccountId] ON [UserCvs] ([AccountId]);
GO


CREATE INDEX [IX_UserSubscriptions_CandidateId] ON [UserSubscriptions] ([CandidateId]);
GO


CREATE INDEX [IX_UserSubscriptions_PackageId] ON [UserSubscriptions] ([PackageId]);
GO


CREATE INDEX [IX_Votes_CommentId] ON [Votes] ([CommentId]);
GO


