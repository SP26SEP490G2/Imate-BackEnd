using Imate.API.Business.Interfaces;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.PracticeTest;
using Imate.API.Presentation.ResponseModels.PracticeTest;

namespace Imate.API.Business.Services
{
    public class PracticeTestHistoryService : IPracticeTestHistoryService
    {
        private readonly IPracticeTestSessionRepository _sessionRepository;

        public PracticeTestHistoryService(IPracticeTestSessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
        }

        public async Task<int> SubmitTestAsync(int accountId, SubmitPracticeTestRequest request)
        {
            if (request.Answers == null || request.Answers.Count == 0)
                throw new ArgumentException("Danh sách câu trả lời không được để trống.");

            var correctCount = request.Answers.Count(a =>
                !string.IsNullOrEmpty(a.UserAnswer) &&
                a.UserAnswer.Equals(a.CorrectAnswer, StringComparison.OrdinalIgnoreCase));

            var score = (int)Math.Round((double)correctCount / request.TotalQuestions * 100);

            var session = new PracticeTestSession
            {
                AccountId = accountId,
                TestTitle = request.TestTitle,
                TestType = request.TestType,
                Field = request.Field,
                Level = request.Level,
                TotalQuestions = request.TotalQuestions,
                CorrectAnswers = correctCount,
                Score = score,
                TimeLimitMinutes = request.TimeLimitMinutes,
                DurationMinutes = request.DurationMinutes,
                CompletedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                Answers = request.Answers.Select(a => new PracticeTestAnswer
                {
                    QuestionNumber = a.QuestionNumber,
                    QuestionText = a.QuestionText,
                    OptionsJson = a.OptionsJson,
                    CorrectAnswer = a.CorrectAnswer,
                    UserAnswer = a.UserAnswer,
                    IsCorrect = !string.IsNullOrEmpty(a.UserAnswer) &&
                                a.UserAnswer.Equals(a.CorrectAnswer, StringComparison.OrdinalIgnoreCase),
                    Explanation = a.Explanation,
                }).ToList()
            };

            await _sessionRepository.AddAsync(session);
            return session.Id;
        }

        public async Task<List<TestHistoryItemResponse>> GetHistoryAsync(int accountId)
        {
            var sessions = await _sessionRepository.GetByAccountIdAsync(accountId);

            return sessions.Select(s => new TestHistoryItemResponse
            {
                Id = s.Id,
                TestTitle = s.TestTitle,
                TestType = s.TestType,
                Field = s.Field,
                Level = s.Level,
                Score = s.Score,
                TotalQuestions = s.TotalQuestions,
                CorrectAnswers = s.CorrectAnswers,
                CompletedAt = s.CompletedAt,
            }).ToList();
        }

        public async Task<TestHistoryDetailResponse> GetDetailAsync(int accountId, int sessionId)
        {
            var session = await _sessionRepository.GetByIdWithAnswersAsync(sessionId);

            if (session == null)
                throw new Exception("Không tìm thấy bài test.");

            if (session.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền xem bài test này.");

            return new TestHistoryDetailResponse
            {
                Id = session.Id,
                TestTitle = session.TestTitle,
                TestType = session.TestType,
                Field = session.Field,
                Level = session.Level,
                TotalQuestions = session.TotalQuestions,
                CorrectAnswers = session.CorrectAnswers,
                Score = session.Score,
                TimeLimitMinutes = session.TimeLimitMinutes,
                DurationMinutes = session.DurationMinutes,
                TechnicalScore = session.TechnicalScore,
                LogicalScore = session.LogicalScore,
                OptimizationScore = session.OptimizationScore,
                AiFeedback = session.AiFeedback,
                AiStrengths = session.AiStrengths,
                AiImprovements = session.AiImprovements,
                CompletedAt = session.CompletedAt,
                Answers = session.Answers.Select(a => new TestAnswerDetailResponse
                {
                    QuestionNumber = a.QuestionNumber,
                    QuestionText = a.QuestionText,
                    OptionsJson = a.OptionsJson,
                    CorrectAnswer = a.CorrectAnswer,
                    UserAnswer = a.UserAnswer,
                    IsCorrect = a.IsCorrect,
                    Explanation = a.Explanation,
                }).ToList()
            };
        }
    }
}
