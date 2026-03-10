-- ============================================================================
-- IMATE: Fake Data SQL Script cho test User Detail Modal
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio
-- ============================================================================
-- LƯU Ý: 
--   AccountStatus: Active=0, Suspended=1, PendingVerification=2
--   RoleName (Roles.Id): 1=Candidate, 2=Mentor, 3=Recruiter, 4=Staff, 5=Admin
--   BookingStatus: Pending=0, Confirmed=1, Completed=2, Cancelled=3, Refunded=4
--   AuditAction: Create=0, Update=1, Delete=2, Read=3, SuspendUser=4
--   LoginProvider: Google=0, Email=1 (giả sử)
-- ============================================================================

-- Trước tiên, kiểm tra RoleId trong bảng Roles của bạn:
-- SELECT * FROM Roles;
-- Nếu Roles.Id khác, hãy thay đổi RoleId bên dưới cho phù hợp.

-- ============================================================================
-- 1. TẠO ACCOUNTS (3 test users)
-- ============================================================================
SET IDENTITY_INSERT Accounts ON;

-- Test Mentor Account (ID = 900)
INSERT INTO Accounts (Id, Email, FullName, AvatarUrl, Provider, ProviderId, Balance, Status, FreeUsedMock, CreatedAt, UpdatedAt)
VALUES (900, 
        N'test.mentor@imate.com', 
        N'Nguyễn Văn Mentor', 
        N'https://api.dicebear.com/7.x/avataaars/svg?seed=mentor',
        0, -- Google
        'google_mentor_900',
        500000,
        0,  -- Active
        0,
        DATEADD(MONTH, -3, GETUTCDATE()),
        NULL);

-- Test Candidate Account (ID = 901)
INSERT INTO Accounts (Id, Email, FullName, AvatarUrl, Provider, ProviderId, Balance, Status, FreeUsedMock, CreatedAt, UpdatedAt)
VALUES (901,
        N'test.candidate@imate.com',
        N'Trần Thị Candidate',
        N'https://api.dicebear.com/7.x/avataaars/svg?seed=candidate',
        0,
        'google_candidate_901',
        100000,
        0,  -- Active
        2,
        DATEADD(MONTH, -2, GETUTCDATE()),
        NULL);

-- Test Staff Account (ID = 902)
INSERT INTO Accounts (Id, Email, FullName, AvatarUrl, Provider, ProviderId, Balance, Status, FreeUsedMock, CreatedAt, UpdatedAt)
VALUES (902,
        N'test.staff@imate.com',
        N'Lê Minh Staff',
        N'https://api.dicebear.com/7.x/avataaars/svg?seed=staff',
        0,
        'google_staff_902',
        0,
        0,  -- Active
        0,
        DATEADD(MONTH, -6, GETUTCDATE()),
        NULL);

SET IDENTITY_INSERT Accounts OFF;

-- ============================================================================
-- 2. GÁN ROLES
-- ============================================================================
-- Kiểm tra RoleId đúng: SELECT * FROM Roles;
-- Thay đổi RoleId nếu cần

INSERT INTO AccountRoles (AccountId, RoleId) VALUES (900, 2); -- Mentor
INSERT INTO AccountRoles (AccountId, RoleId) VALUES (901, 1); -- Candidate
INSERT INTO AccountRoles (AccountId, RoleId) VALUES (902, 4); -- Staff

-- ============================================================================
-- 3. TẠO MENTOR DATA (cho Account 900)
-- ============================================================================
INSERT INTO Mentors (AccountId, Bio, Phone, BirthDate, Yoe, CvUrl, CertificateUrl, PricePerSession, PriceLastUpdatedDate, AvgRatings, TotalRatingCount, BankAccountHolderName, BankAccountNumber, BankCode)
VALUES (900,
        N'Senior Software Engineer với 8 năm kinh nghiệm trong lĩnh vực Backend Development. Chuyên môn sâu về C#, .NET, và kiến trúc Microservices. Đã mentor cho hơn 50 developer.',
        N'+84 912 345 678',
        '1992-05-15',    -- BirthDate
        8,               -- Years of Experience
        NULL,
        NULL,
        200000,          -- PricePerSession (200k VND)
        NULL,
        4.7,             -- AvgRatings
        15,              -- TotalRatingCount
        N'NGUYEN VAN MENTOR',
        N'1234567890',
        N'VCB');

-- ============================================================================
-- 4. TẠO BOOKINGS + REVIEWS (cho test Mentor panel & Candidate panel)
-- ============================================================================
SET IDENTITY_INSERT Bookings ON;

-- Booking 1: Completed + có review (Candidate 901 -> Mentor 900)
INSERT INTO Bookings (Id, CandidateId, MentorId, StartTime, BookDate, PriceAtBooking, Status, AgoraChannelName, AudioRecordKey, RatingScore, ReviewText, RatingCreatedAt, CreatedAt, UpdatedAt)
VALUES (900,
        901,       -- CandidateId
        900,       -- MentorId
        DATEADD(DAY, -10, GETUTCDATE()),   -- StartTime
        CAST(DATEADD(DAY, -10, GETUTCDATE()) AS DATE), -- BookDate
        200000,    -- PriceAtBooking
        2,         -- Completed
        'channel_test_900',
        NULL,
        5,         -- RatingScore (5 sao)
        N'Mentor rất tận tâm, giải thích rõ ràng các khái niệm về SOLID principles và design patterns. Rất recommend!',
        DATEADD(DAY, -9, GETUTCDATE()),
        DATEADD(DAY, -11, GETUTCDATE()),
        DATEADD(DAY, -9, GETUTCDATE()));

-- Booking 2: Completed + có review
INSERT INTO Bookings (Id, CandidateId, MentorId, StartTime, BookDate, PriceAtBooking, Status, AgoraChannelName, AudioRecordKey, RatingScore, ReviewText, RatingCreatedAt, CreatedAt, UpdatedAt)
VALUES (901,
        901,
        900,
        DATEADD(DAY, -20, GETUTCDATE()),
        CAST(DATEADD(DAY, -20, GETUTCDATE()) AS DATE),
        200000,
        2,         -- Completed
        'channel_test_901',
        NULL,
        4,         -- RatingScore (4 sao)
        N'Buổi phỏng vấn thử rất bổ ích. Mentor cho nhiều feedback thiết thực về cách trả lời behavioral questions.',
        DATEADD(DAY, -19, GETUTCDATE()),
        DATEADD(DAY, -21, GETUTCDATE()),
        DATEADD(DAY, -19, GETUTCDATE()));

-- Booking 3: Completed + có review (từ 1 candidate khác - dùng account 901 luôn vì đơn giản)
INSERT INTO Bookings (Id, CandidateId, MentorId, StartTime, BookDate, PriceAtBooking, Status, AgoraChannelName, AudioRecordKey, RatingScore, ReviewText, RatingCreatedAt, CreatedAt, UpdatedAt)
VALUES (902,
        901,
        900,
        DATEADD(DAY, -5, GETUTCDATE()),
        CAST(DATEADD(DAY, -5, GETUTCDATE()) AS DATE),
        200000,
        2,         -- Completed
        'channel_test_902',
        NULL,
        5,
        N'Xuất sắc! Mentor đã giúp tôi chuẩn bị rất tốt cho buổi phỏng vấn ở FPT Software. Đã nhận offer sau đó.',
        DATEADD(DAY, -4, GETUTCDATE()),
        DATEADD(DAY, -6, GETUTCDATE()),
        DATEADD(DAY, -4, GETUTCDATE()));

-- Booking 4: Confirmed (upcoming, chưa hoàn thành)
INSERT INTO Bookings (Id, CandidateId, MentorId, StartTime, BookDate, PriceAtBooking, Status, AgoraChannelName, AudioRecordKey, RatingScore, ReviewText, RatingCreatedAt, CreatedAt, UpdatedAt)
VALUES (903,
        901,
        900,
        DATEADD(DAY, 3, GETUTCDATE()),
        CAST(DATEADD(DAY, 3, GETUTCDATE()) AS DATE),
        200000,
        1,         -- Confirmed
        'channel_test_903',
        NULL,
        NULL, NULL, NULL,
        DATEADD(DAY, -1, GETUTCDATE()),
        NULL);

SET IDENTITY_INSERT Bookings OFF;

-- ============================================================================
-- 5. TẠO SUBSCRIPTION DATA (cho test Candidate panel)
-- ============================================================================
-- Đầu tiên kiểm tra xem đã có SubscriptionPackage chưa:
-- SELECT * FROM SubscriptionPackages;
-- Nếu chưa có, tạo gói:

-- Tạo gói nếu chưa tồn tại
IF NOT EXISTS (SELECT 1 FROM SubscriptionPackages WHERE Id = 900)
BEGIN
    SET IDENTITY_INSERT SubscriptionPackages ON;
    
    INSERT INTO SubscriptionPackages (Id, Name, Price, DurationDays, Benefits, IsActive, IsRecommended)
    VALUES (900, N'Gói Cơ Bản', 99000, 30,
            N'{"mockInterviews": 5, "aiReview": true}', 1, 0);

    INSERT INTO SubscriptionPackages (Id, Name, Price, DurationDays, Benefits, IsActive, IsRecommended)
    VALUES (901, N'Gói Premium', 299000, 90,
            N'{"mockInterviews": 20, "aiReview": true, "mentorSession": 3}', 1, 1);

    SET IDENTITY_INSERT SubscriptionPackages OFF;
END;

-- Tạo subscriptions cho Candidate 901
SET IDENTITY_INSERT UserSubscriptions ON;

-- Gói cũ (đã hết hạn)
INSERT INTO UserSubscriptions (Id, CandidateId, PackageId, StartDate, EndDate, InitialMockLimit, MockInterviewUsed, IsActive, CreatedAt, UpdatedAt)
VALUES (900,
        901,            -- Candidate
        900,            -- Gói Cơ Bản
        DATEADD(MONTH, -4, CAST(GETUTCDATE() AS DATE)),  -- StartDate: 4 tháng trước
        DATEADD(MONTH, -3, CAST(GETUTCDATE() AS DATE)),  -- EndDate
        5,
        5,              -- Đã dùng hết
        0,              -- Không còn active
        DATEADD(MONTH, -4, GETUTCDATE()),
        NULL);

-- Gói hiện tại (đang active)
INSERT INTO UserSubscriptions (Id, CandidateId, PackageId, StartDate, EndDate, InitialMockLimit, MockInterviewUsed, IsActive, CreatedAt, UpdatedAt)
VALUES (901,
        901,            -- Candidate
        901,            -- Gói Premium
        DATEADD(DAY, -15, CAST(GETUTCDATE() AS DATE)),   -- StartDate: 15 ngày trước
        DATEADD(DAY, 75, CAST(GETUTCDATE() AS DATE)),    -- EndDate: còn 75 ngày
        20,
        3,              -- Đã dùng 3
        1,              -- Đang active
        DATEADD(DAY, -15, GETUTCDATE()),
        NULL);

SET IDENTITY_INSERT UserSubscriptions OFF;

-- ============================================================================
-- 6. TẠO AUDIT LOG DATA (cho test Staff panel)
-- ============================================================================
SET IDENTITY_INSERT AuditLogs ON;

INSERT INTO AuditLogs (Id, UserId, ActionTime, Action, EntityType, EntityId, OldValue, NewValue, CreatedAt, UpdatedAt)
VALUES 
(900, 902, DATEADD(HOUR, -2, GETUTCDATE()),  0, 'Question', 1,  NULL, N'{"Title":"Câu hỏi về React Hooks"}', DATEADD(HOUR, -2, GETUTCDATE()), NULL),
(901, 902, DATEADD(HOUR, -5, GETUTCDATE()),  1, 'Question', 2,  N'{"Status":"Draft"}', N'{"Status":"Published"}', DATEADD(HOUR, -5, GETUTCDATE()), NULL),
(902, 902, DATEADD(DAY, -1, GETUTCDATE()),   0, 'Question', 3,  NULL, N'{"Title":"System Design: URL Shortener"}', DATEADD(DAY, -1, GETUTCDATE()), NULL),
(903, 902, DATEADD(DAY, -2, GETUTCDATE()),   4, 'Account',  901, N'{"Status":"Active"}', N'{"Status":"Suspended"}', DATEADD(DAY, -2, GETUTCDATE()), NULL),
(904, 902, DATEADD(DAY, -3, GETUTCDATE()),   1, 'Question', 5,  N'{"Difficulty":"Easy"}', N'{"Difficulty":"Medium"}', DATEADD(DAY, -3, GETUTCDATE()), NULL);

SET IDENTITY_INSERT AuditLogs OFF;

-- ============================================================================
-- 7. TẠO QUESTIONS (cho test Staff panel - QuestionCount)
-- ============================================================================
SET IDENTITY_INSERT Questions ON;

-- Tạo vài câu hỏi do Staff 902 tạo (CreatorId = 902)
-- Kiểm tra schema Questions trước: SELECT TOP 1 * FROM Questions;
-- Nếu bảng Questions có các cột khác, cần adjust

-- Nếu bảng Questions yêu cầu các cột bắt buộc khác, hãy comment block này
-- và chỉ test với Staff panel mà không có QuestionCount
/*
INSERT INTO Questions (Id, CreatorId, ...) 
VALUES ...
*/

SET IDENTITY_INSERT Questions OFF;

-- ============================================================================
-- KIỂM TRA DỮ LIỆU ĐÃ TẠO
-- ============================================================================
PRINT N'=== Accounts đã tạo ===';
SELECT Id, Email, FullName, Status, CreatedAt FROM Accounts WHERE Id IN (900, 901, 902);

PRINT N'=== AccountRoles ===';
SELECT ar.AccountId, r.Name AS RoleName FROM AccountRoles ar JOIN Roles r ON ar.RoleId = r.Id WHERE ar.AccountId IN (900, 901, 902);

PRINT N'=== Mentor data ===';
SELECT AccountId, Bio, Phone, AvgRatings, TotalRatingCount, PricePerSession FROM Mentors WHERE AccountId = 900;

PRINT N'=== Bookings ===';
SELECT Id, CandidateId, MentorId, Status, RatingScore, ReviewText FROM Bookings WHERE Id BETWEEN 900 AND 903;

PRINT N'=== UserSubscriptions ===';
SELECT us.Id, us.CandidateId, sp.Name AS PackageName, us.StartDate, us.EndDate, us.IsActive 
FROM UserSubscriptions us JOIN SubscriptionPackages sp ON us.PackageId = sp.Id 
WHERE us.CandidateId = 901;

PRINT N'=== AuditLogs ===';
SELECT Id, UserId, Action, EntityType, EntityId FROM AuditLogs WHERE UserId = 902;

PRINT N'✅ Fake data đã được tạo thành công!';
PRINT N'Test accounts:';
PRINT N'  - Mentor:    ID=900, test.mentor@imate.com';
PRINT N'  - Candidate: ID=901, test.candidate@imate.com';
PRINT N'  - Staff:     ID=902, test.staff@imate.com';

-- ============================================================================
-- CLEANUP (chạy khi muốn xoá test data)
-- ============================================================================
/*
DELETE FROM AuditLogs WHERE Id BETWEEN 900 AND 904;
DELETE FROM UserSubscriptions WHERE Id IN (900, 901);
DELETE FROM Bookings WHERE Id BETWEEN 900 AND 903;
DELETE FROM Mentors WHERE AccountId = 900;
DELETE FROM AccountRoles WHERE AccountId IN (900, 901, 902);
DELETE FROM Accounts WHERE Id IN (900, 901, 902);
-- Nếu đã tạo SubscriptionPackages test:
DELETE FROM SubscriptionPackages WHERE Id IN (900, 901);
PRINT N'✅ Test data đã được xoá!';
*/
