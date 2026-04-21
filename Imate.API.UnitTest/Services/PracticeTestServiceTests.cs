using FluentAssertions;
using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Core.Orchestrators;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Imate.API.Business.Services;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.PracticeTest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Imate.API.UnitTest.Services
{
    public class PracticeTestServiceTests
    {
        private readonly Mock<IPracticeTestSessionRepository> _mockSessionRepo;
        private readonly Mock<IPracticeTestAgent> _mockPracticeTestAgent;
        private readonly Mock<IQuestionDataProvider> _mockQuestionDataProvider;
        private readonly Mock<ICvDataProvider> _mockCvDataProvider;
        private readonly Mock<ILogger<PracticeTestOrchestrator>> _mockOrchestratorLogger;
        private readonly Mock<ILogger<PracticeTestHistoryService>> _mockHistoryServiceLogger;
        private readonly Mock<ILogger<QuestionDataProvider>> _mockQuestionProviderLogger;

        private readonly PracticeTestOrchestrator _orchestrator;
        private readonly PracticeTestHistoryService _historyService;
        private readonly QuestionDataProvider _questionProvider;
        private readonly ImateDbContext _dbContext;

        public PracticeTestServiceTests()
        {
            _mockSessionRepo = new Mock<IPracticeTestSessionRepository>();
            _mockPracticeTestAgent = new Mock<IPracticeTestAgent>();
            _mockQuestionDataProvider = new Mock<IQuestionDataProvider>();
            _mockCvDataProvider = new Mock<ICvDataProvider>();
            _mockOrchestratorLogger = new Mock<ILogger<PracticeTestOrchestrator>>();
            _mockHistoryServiceLogger = new Mock<ILogger<PracticeTestHistoryService>>();
            _mockQuestionProviderLogger = new Mock<ILogger<QuestionDataProvider>>();

            var options = new DbContextOptionsBuilder<ImateDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ImateDbContext(options);

            _questionProvider = new QuestionDataProvider(_dbContext, _mockQuestionProviderLogger.Object);

            _orchestrator = new PracticeTestOrchestrator(
                _mockPracticeTestAgent.Object,
                _mockOrchestratorLogger.Object,
                _mockCvDataProvider.Object,
                _questionProvider
            );

            _historyService = new PracticeTestHistoryService(_mockSessionRepo.Object);
        }

        #region Practice Test
        [Fact]
        public async Task GenerateTestAsync_ShouldSucceed_WithRagDataWhenQuestionsFound()
        {
            var pos = new Position { Id = 1, Name = "Backend" };
            var questions = new List<Question>
            {
                new() { Id = 1, IsActive = true, IsFromSystem = true, Difficulty = DifficultyLevel.Medium, QuestionPositions = new List<QuestionPosition>{ new() { Position = pos } } }
            };
            _dbContext.Questions.AddRange(questions);
            await _dbContext.SaveChangesAsync();

            var request = new GeneratePracticeTestRequest { Field = "Backend", Level = "Middle" };
            _mockPracticeTestAgent.Setup(a => a.GenerateTestAsync(request, It.IsAny<string>(), It.IsAny<List<QuestionBankItem>>()))
                .ReturnsAsync(new PracticeTestResponse { TestTitle = "RAG Test" });
            var result = await _orchestrator.GenerateTestAsync(1, request);

            result.TestTitle.Should().Be("RAG Test");
            _mockPracticeTestAgent.Verify(a => a.GenerateTestAsync(request, null, It.Is<List<QuestionBankItem>>(l => l.Count > 0)), Times.Once);
        }

        [Fact]
        public async Task GenerateTestAsync_ShouldSucceed_WithAiOnlyWhenNoQuestionsFound()
        {
            var request = new GeneratePracticeTestRequest { Field = "Niche Topic", Level = "Expert" };
            _mockPracticeTestAgent.Setup(a => a.GenerateTestAsync(request, It.IsAny<string>(), It.IsAny<List<QuestionBankItem>>()))
                .ReturnsAsync(new PracticeTestResponse { TestTitle = "AI Only Test" });

            var result = await _orchestrator.GenerateTestAsync(1, request);

            result.TestTitle.Should().Be("AI Only Test");
            _mockPracticeTestAgent.Verify(a => a.GenerateTestAsync(request, null, It.Is<List<QuestionBankItem>>(l => l.Count == 0)), Times.Once);
        }

        [Fact]
        public async Task GenerateTestAsync_ShouldThrowArgumentException_WhenFieldOrLevelMissing()
        {
            await _orchestrator.Invoking(o => o.GenerateTestAsync(1, new GeneratePracticeTestRequest { Field = "" }))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GenerateTestAsync_ShouldAdjustNumberOfQuestions_WhenOutsideRange()
        {
            var request = new GeneratePracticeTestRequest { Field = "IT", Level = "Intern", NumberOfQuestions = 50 };
            _mockPracticeTestAgent.Setup(a => a.GenerateTestAsync(It.IsAny<GeneratePracticeTestRequest>(), It.IsAny<string>(), It.IsAny<List<QuestionBankItem>>()))
                .ReturnsAsync(new PracticeTestResponse());

            await _orchestrator.GenerateTestAsync(1, request);

            request.NumberOfQuestions.Should().Be(10);
        }

        [Fact]
        public async Task GetQuestionsAsync_ShouldFilterByPositionAndLevel()
        {
            var pos = new Position { Name = "Frontend" };
            _dbContext.Questions.Add(new Question { IsActive = true, IsFromSystem = true, Difficulty = DifficultyLevel.Easy, QuestionPositions = new[] { new QuestionPosition { Position = pos } } });
            await _dbContext.SaveChangesAsync();

            var result = await _questionProvider.GetQuestionsAsync("Frontend", "Intern", 5);

            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetQuestionsAsync_ShouldFallbackWithoutLevel_WhenNoExactMatch()
        {
            var pos = new Position { Name = "DevOps" };
            _dbContext.Questions.Add(new Question { IsActive = true, IsFromSystem = true, Difficulty = DifficultyLevel.Hard, QuestionPositions = new[] { new QuestionPosition { Position = pos } } });
            await _dbContext.SaveChangesAsync();
            var result = await _questionProvider.GetQuestionsAsync("DevOps", "Intern", 5);

            result.Should().HaveCount(1);
        }
        #endregion

        #region View Test Result History
        [Fact]
        public async Task GetHistoryAsync_ShouldReturnSessions_ForAccount()
        {
            var sessions = new List<PracticeTestSession> { new() { Id = 1, AccountId = 1, TestTitle = "Java" } };
            _mockSessionRepo.Setup(r => r.GetByAccountIdAsync(1)).ReturnsAsync(sessions);

            var result = await _historyService.GetHistoryAsync(1);

            result.Should().HaveCount(1);
            result[0].TestTitle.Should().Be("Java");
        }

        [Fact]
        public async Task GetHistoryAsync_ShouldMapSummaryFieldsCorrectly_ForListView()
        {
            var sessions = new List<PracticeTestSession> 
            { 
                new() { Id = 1, AccountId = 1, TestTitle = "Java", Score = 80 } 
            };
            _mockSessionRepo.Setup(r => r.GetByAccountIdAsync(1)).ReturnsAsync(sessions);

            var result = await _historyService.GetHistoryAsync(1);

            result[0].TestTitle.Should().Be("Java");
            result[0].Score.Should().Be(80);
        }

        [Fact]
        public async Task GetHistoryAsync_ShouldReturnEmpty_WhenNoSessionsExist()
        {
            _mockSessionRepo.Setup(r => r.GetByAccountIdAsync(1)).ReturnsAsync(new List<PracticeTestSession>());
            var result = await _historyService.GetHistoryAsync(1);
            result.Should().BeEmpty();
        }
        #endregion

        #region View Test Result History Detail
        [Fact]
        public async Task GetDetailAsync_ShouldReturnFullDetail_WhenValid()
        {
            var session = new PracticeTestSession 
            { 
                Id = 10, AccountId = 1, 
                Answers = new List<PracticeTestAnswer> { new() { QuestionNumber = 1, Explanation = "E1" } } 
            };
            _mockSessionRepo.Setup(r => r.GetByIdWithAnswersAsync(10)).ReturnsAsync(session);

            var result = await _historyService.GetDetailAsync(1, 10);

            result.Answers.Should().HaveCount(1);
            result.Answers[0].Explanation.Should().Be("E1");
        }

        [Fact]
        public async Task GetDetailAsync_ShouldIncludeDetailedAnswerBreakdown_WithQuestionText()
        {
            var session = new PracticeTestSession 
            { 
                Id = 10, AccountId = 1, 
                Answers = new List<PracticeTestAnswer> 
                { 
                    new() { QuestionNumber = 1, QuestionText = "What is C#?", UserAnswer = "A", CorrectAnswer = "A" } 
                } 
            };
            _mockSessionRepo.Setup(r => r.GetByIdWithAnswersAsync(10)).ReturnsAsync(session);
            var result = await _historyService.GetDetailAsync(1, 10);

            result.Answers[0].QuestionText.Should().Be("What is C#?");
            result.Answers[0].UserAnswer.Should().Be("A");
        }

        [Fact]
        public async Task GetDetailAsync_ShouldIncludeScoreBreakdown_TechnicalLogicalOptimization()
        {
            var session = new PracticeTestSession 
            { 
                Id = 10, AccountId = 1, 
                TechnicalScore = 70, LogicalScore = 80, OptimizationScore = 90 
            };
            _mockSessionRepo.Setup(r => r.GetByIdWithAnswersAsync(10)).ReturnsAsync(session);

            var result = await _historyService.GetDetailAsync(1, 10);

            result.TechnicalScore.Should().Be(70);
            result.LogicalScore.Should().Be(80);
            result.OptimizationScore.Should().Be(90);
        }

        [Fact]
        public async Task GetDetailAsync_ShouldThrowException_WhenSessionNotFound()
        {
            _mockSessionRepo.Setup(r => r.GetByIdWithAnswersAsync(It.IsAny<int>())).ReturnsAsync((PracticeTestSession?)null);

            var act = () => _historyService.GetDetailAsync(1, 99);

            await act.Should().ThrowAsync<Exception>().WithMessage("*không tìm thấy*");
        }

        [Fact]
        public async Task GetDetailAsync_ShouldThrowUnauthorized_WhenNotOwner()
        {
            var session = new PracticeTestSession { Id = 10, AccountId = 1 };
            _mockSessionRepo.Setup(r => r.GetByIdWithAnswersAsync(10)).ReturnsAsync(session);

            var act = () => _historyService.GetDetailAsync(2, 10);
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task SubmitTestAsync_ShouldMapCorrectAnswersAndCalculatedScore()
        {
            var request = new SubmitPracticeTestRequest
            {
                TestTitle = "Demo",
                TotalQuestions = 2,
                Answers = new List<SubmitAnswerItem>
                {
                    new() { QuestionNumber = 1, UserAnswer = "A", CorrectAnswer = "A" },
                    new() { QuestionNumber = 2, UserAnswer = "B", CorrectAnswer = "C" }
                }
            };

            await _historyService.SubmitTestAsync(1, request);

            _mockSessionRepo.Verify(r => r.AddAsync(It.Is<PracticeTestSession>(s =>
                s.CorrectAnswers == 1 && s.Score == 50)), Times.Once);
        }
        #endregion

        #region Receive AI's Feedback
        [Fact]
        public async Task GetDetailAsync_ShouldIncludeFullAiFeedback_IncludingStrengthsAndImprovements()
        {
            var session = new PracticeTestSession 
            { 
                Id = 10, AccountId = 1, 
                AiFeedback = "FB", AiStrengths = "S1", AiImprovements = "I1" 
            };
            _mockSessionRepo.Setup(r => r.GetByIdWithAnswersAsync(10)).ReturnsAsync(session);

            var result = await _historyService.GetDetailAsync(1, 10);
            result.AiFeedback.Should().Be("FB");
            result.AiStrengths.Should().Be("S1");
            result.AiImprovements.Should().Be("I1");
        }

        [Fact]
        public async Task GetDetailAsync_ShouldIncludeExplanationForEachAnswer_FromAiFeedback()
        {
            var session = new PracticeTestSession 
            { 
                Id = 10, AccountId = 1, 
                Answers = new List<PracticeTestAnswer> 
                { 
                    new() { QuestionNumber = 1, Explanation = "AI Explanation" } 
                } 
            };
            _mockSessionRepo.Setup(r => r.GetByIdWithAnswersAsync(10)).ReturnsAsync(session);

            var result = await _historyService.GetDetailAsync(1, 10);

            result.Answers[0].Explanation.Should().Be("AI Explanation");
        }
        #endregion
    }
}
