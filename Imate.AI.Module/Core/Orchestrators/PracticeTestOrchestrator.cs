using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.Logging;


namespace Imate.AI.Module.Core.Orchestrators
{
    /// <summary>
    /// Orchestrator bài test luyện tập (Tầng 2 - Orchestrators)
    /// Điều phối workflow: validate → credit check → RAG query → Agent → deduct credit
    /// </summary>
    public class PracticeTestOrchestrator : IPracticeTestOrchestrator
    {
        private readonly IPracticeTestAgent _practiceTestAgent;
        private readonly ICvDataProvider? _cvDataProvider;
        private readonly IQuestionDataProvider? _questionDataProvider;
        private readonly IInterviewSessionDataProvider? _sessionDataProvider;
        private readonly ILogger<PracticeTestOrchestrator> _logger;

        public PracticeTestOrchestrator(
            IPracticeTestAgent practiceTestAgent,
            ILogger<PracticeTestOrchestrator> logger,
            ICvDataProvider? cvDataProvider = null,
            IQuestionDataProvider? questionDataProvider = null,
            IInterviewSessionDataProvider? sessionDataProvider = null)
        {
            _practiceTestAgent = practiceTestAgent;
            _logger = logger;
            _cvDataProvider = cvDataProvider;
            _questionDataProvider = questionDataProvider;
            _sessionDataProvider = sessionDataProvider;
        }

        public async Task<PracticeTestResponse> GenerateTestAsync(int accountId, GeneratePracticeTestRequest request)
        {
            // 1. Validate request
            ValidateRequest(request);

            // 2. Kiểm tra AI Credits trước khi tạo bài test (dùng PRACTICE_QUESTION_COST_POINTS)
            if (_sessionDataProvider != null)
            {
                var limitStatus = await _sessionDataProvider.GetPracticeTestLimitStatusAsync(accountId);
                if (!limitStatus.CanStart)
                {
                    _logger.LogWarning("[PracticeTest] Account {AccountId} không đủ AI Credits để tạo bài test. Remaining={Remaining}",
                        accountId, limitStatus.RemainingCount);
                    throw new InvalidOperationException(limitStatus.Message);
                }
                _logger.LogInformation("[PracticeTest] Account {AccountId} còn {Remaining} AI Credits, tiến hành tạo bài test.",
                    accountId, limitStatus.RemainingCount);
            }

            // 3. Lấy CV context nếu cần
            string? cvContext = null;
            if (request.UseCV && !string.IsNullOrWhiteSpace(request.CvText))
            {
                cvContext = request.CvText;
            }

            // 4. RAG: Lấy câu hỏi mẫu từ Question Bank trong DB
            List<QuestionBankItem> ragQuestions = new();
            if (_questionDataProvider != null)
            {
                try
                {
                    ragQuestions = await _questionDataProvider.GetQuestionsAsync(
                        request.Skill, request.Field, request.Level, request.NumberOfQuestions);
                    _logger.LogInformation(
                        "RAG: Retrieved {Count} reference questions from Question Bank for skill={Skill}, field={Field}, level={Level}",
                        ragQuestions.Count, request.Skill, request.Field, request.Level);
                    _logger.LogInformation(ragQuestions.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "RAG: Failed to retrieve questions from DB, falling back to AI-only mode");
                }
            }
            else
            {
                _logger.LogInformation("RAG: IQuestionDataProvider not registered, using AI-only mode");
            }

            // 5. Gọi Agent tạo test
            var result = await _practiceTestAgent.GenerateTestAsync(request, cvContext, ragQuestions);

            // 6. Trừ 1 AI Credit sau khi tạo bài test thành công (dùng PRACTICE_QUESTION_COST_POINTS)
            if (_sessionDataProvider != null)
            {
                try
                {
                    await _sessionDataProvider.ConsumePracticeTestCostAsync(accountId);
                    _logger.LogInformation("[PracticeTest] Đã trừ AI Credit (PRACTICE_QUESTION_COST_POINTS) cho account {AccountId}.", accountId);
                }
                catch (Exception ex)
                {
                    // Không block flow nếu trừ credit lỗi — log warning để điều tra
                    _logger.LogWarning(ex, "[PracticeTest] Lỗi khi trừ AI Credit cho account {AccountId}.", accountId);
                }
            }

            return result;
        }

        private static void ValidateRequest(GeneratePracticeTestRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Field))
                throw new ArgumentException("Vui lòng chọn lĩnh vực chuyên môn.");

            if (string.IsNullOrWhiteSpace(request.Level))
                throw new ArgumentException("Vui lòng chọn cấp bậc ứng tuyển.");

            // Luôn cố định 15 câu hỏi
            request.NumberOfQuestions = 15;
        }
    }
}