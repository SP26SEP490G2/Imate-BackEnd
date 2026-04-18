using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.Logging;


namespace Imate.AI.Module.Core.Orchestrators
{
    /// <summary>
    /// Orchestrator bài test luyện tập (Tầng 2 - Orchestrators)
    /// Điều phối workflow: validate → RAG query → Agent
    /// </summary>
    public class PracticeTestOrchestrator : IPracticeTestOrchestrator
    {
        private readonly IPracticeTestAgent _practiceTestAgent;
        private readonly ICvDataProvider? _cvDataProvider;
        private readonly IQuestionDataProvider? _questionDataProvider;
        private readonly ILogger<PracticeTestOrchestrator> _logger;

        public PracticeTestOrchestrator(
            IPracticeTestAgent practiceTestAgent,
            ILogger<PracticeTestOrchestrator> logger,
            ICvDataProvider? cvDataProvider = null,
            IQuestionDataProvider? questionDataProvider = null)
        {
            _practiceTestAgent = practiceTestAgent;
            _logger = logger;
            _cvDataProvider = cvDataProvider;
            _questionDataProvider = questionDataProvider;
        }

        public async Task<PracticeTestResponse> GenerateTestAsync(int accountId, GeneratePracticeTestRequest request)
        {
            // 1. Validate request
            ValidateRequest(request);

            // 2. Lấy CV context nếu cần
            string? cvContext = null;
            if (request.UseCV && !string.IsNullOrWhiteSpace(request.CvText))
            {
                cvContext = request.CvText;
            }

            // 3. RAG: Lấy câu hỏi mẫu từ Question Bank trong DB
            List<QuestionBankItem> ragQuestions = new();
            if (_questionDataProvider != null)
            {
                try
                {
                    ragQuestions = await _questionDataProvider.GetQuestionsAsync(
                        request.Field, request.Level, request.NumberOfQuestions);
                    _logger.LogInformation(
                        "RAG: Retrieved {Count} reference questions from Question Bank for field={Field}, level={Level}",
                        ragQuestions.Count, request.Field, request.Level);
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

            // 4. Gọi Agent tạo test
            return await _practiceTestAgent.GenerateTestAsync(request, cvContext, ragQuestions);
        }

        private static void ValidateRequest(GeneratePracticeTestRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Field))
                throw new ArgumentException("Vui lòng chọn lĩnh vực chuyên môn.");

            if (string.IsNullOrWhiteSpace(request.Level))
                throw new ArgumentException("Vui lòng chọn cấp bậc ứng tuyển.");

            if (request.NumberOfQuestions < 5 || request.NumberOfQuestions > 20)
                request.NumberOfQuestions = 10;
        }
    }
}