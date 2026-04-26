using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using FluentAssertions;
using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Core.Orchestrators;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Imate.API.UnitTest.Services
{
    public class AIInterviewServiceTests
    {
        private readonly Mock<IInterviewAgent> _mockInterviewAgent;
        private readonly Mock<IFeedbackAgent> _mockFeedbackAgent;
        private readonly Mock<IInterviewSessionDataProvider> _mockDataProvider;
        private readonly Mock<ICvDataProvider> _mockCvDataProvider;
        private readonly Mock<IAzureSpeechSynthesisService> _mockSpeechService;
        private readonly Mock<ITrainingJourneyDataProvider> _mockTrainingJourneyDataProvider;
        private readonly Mock<ITrainingJourneyOrchestrator> _mockTrainingJourneyOrchestrator;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<ILogger<InterviewOrchestrator>> _mockLogger;

        private readonly InterviewOrchestrator _orchestrator;

        public AIInterviewServiceTests()
        {
            _mockInterviewAgent = new Mock<IInterviewAgent>();
            _mockFeedbackAgent = new Mock<IFeedbackAgent>();
            _mockDataProvider = new Mock<IInterviewSessionDataProvider>();
            _mockCvDataProvider = new Mock<ICvDataProvider>();
            _mockSpeechService = new Mock<IAzureSpeechSynthesisService>();
            _mockTrainingJourneyDataProvider = new Mock<ITrainingJourneyDataProvider>();
            _mockTrainingJourneyOrchestrator = new Mock<ITrainingJourneyOrchestrator>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockLogger = new Mock<ILogger<InterviewOrchestrator>>();

            _orchestrator = new InterviewOrchestrator(
                _mockInterviewAgent.Object,
                _mockFeedbackAgent.Object,
                _mockDataProvider.Object,
                _mockCvDataProvider.Object,
                _mockSpeechService.Object,
                _mockTrainingJourneyDataProvider.Object,
                _mockScopeFactory.Object,
                _mockTrainingJourneyOrchestrator.Object,
                _mockLogger.Object
            );
        }

        #region Setup Mock Interview
        [Fact]
        public async Task SetupInterviewAsync_ShouldSucceed_WhenJdAndCvAreItRelated()
        {
            var accountId = 1;
            var jdText = "Tuyển dụng Backend Developer với .NET và Azure";
            var cvId = 10;
            _mockCvDataProvider.Setup(p => p.GetCvTextAsync(accountId, cvId)).ReturnsAsync("Senior .NET Developer");
            _mockInterviewAgent.Setup(a => a.ClassifyJobDescriptionAsync(jdText, "Senior .NET Developer"))
                .ReturnsAsync(new SetupInterviewResult { IsItRelatedJd = true, IsItRelatedCv = true, Level = "Senior", CvEstimatedLevel = "Senior" });
            var result = await _orchestrator.SetupInterviewAsync(accountId, jdText, cvId);

            result.IsItRelatedJd.Should().BeTrue();
            result.Level.Should().Be("Senior");
        }

        [Fact]
        public async Task SetupInterviewAsync_ShouldThrowException_WhenJdIsNotItRelated()
        {
            var jdText = "Tuyển dụng đầu bếp món Á chuyên nghiệp";
            _mockInterviewAgent.Setup(a => a.ClassifyJobDescriptionAsync(jdText, null))
                .ReturnsAsync(new SetupInterviewResult { IsItRelatedJd = false });

            var act = () => _orchestrator.SetupInterviewAsync(1, jdText);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*JD*không thuộc ngành Công nghệ thông tin*");
        }

        [Fact]
        public async Task SetupInterviewAsync_ShouldThrowException_WhenCvIsNotItRelated()
        {
            var jdText = "Backend Dev";
            _mockCvDataProvider.Setup(p => p.GetCvTextAsync(1, 10)).ReturnsAsync("Bachelor of Nursing");
            _mockInterviewAgent.Setup(a => a.ClassifyJobDescriptionAsync(jdText, "Bachelor of Nursing"))
                .ReturnsAsync(new SetupInterviewResult { IsItRelatedJd = true, IsItRelatedCv = false });

            var act = () => _orchestrator.SetupInterviewAsync(1, jdText, 10);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*CV của bạn không thuộc ngành Công nghệ thông tin (IT)*");
        }

        [Fact]
        public async Task SetupInterviewAsync_ShouldNotThrowException_WhenLevelGapIsTwoOrMore()
        {
            _mockCvDataProvider.Setup(p => p.GetCvTextAsync(1, 10)).ReturnsAsync("Intern CV");
            _mockInterviewAgent.Setup(a => a.ClassifyJobDescriptionAsync(It.IsAny<string>(), "Intern CV"))
                .ReturnsAsync(new SetupInterviewResult { IsItRelatedJd = true, IsItRelatedCv = true, Level = "Senior", CvEstimatedLevel = "Intern" });

            var result = await _orchestrator.SetupInterviewAsync(1, "Senior Job", 10);

            result.Level.Should().Be("Senior");
        }

        [Fact]
        public async Task CreateSessionAsync_ShouldSucceed_WhenUsageLimitNotReached()
        {
            _mockDataProvider.Setup(p => p.GetInterviewLimitStatusAsync(1))
                .ReturnsAsync(new InterviewLimitStatus { CanStart = true });
            _mockDataProvider.Setup(p => p.CreateSessionAsync(It.IsAny<InterviewSessionData>())).ReturnsAsync(100);

            var sessionId = await _orchestrator.CreateSessionAsync(1, new CreateInterviewSessionRequest { PositionName = "Dev" });

            sessionId.Should().Be(100);
            _mockDataProvider.Verify(p => p.IncrementMockInterviewUsageAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetWelcomeMessageAsync_ShouldReturnTextAndAudio()
        {
            var session = new InterviewSessionData { AccountId = 1, Status = "InProgress", PositionName = "Dev" };
            _mockDataProvider.Setup(p => p.GetSessionByIdAsync(100)).ReturnsAsync(session);
            _mockInterviewAgent.Setup(a => a.GenerateWelcomeMessageAsync(null, "Dev", null, null))
                .ReturnsAsync("Chào bạn!");
            _mockSpeechService.Setup(s => s.SynthesizeToBase64Async("Chào bạn!", "vi-VN", null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AzureSynthesizedSpeechResult { AudioBase64 = "base64data", MimeType = "audio/wav", Text = "Chào bạn!", Voice = "vi-VN-HieuNeural", Language = "vi-VN", AudioUrl = "" });

            var result = await _orchestrator.GetWelcomeMessageAsync(1, 100, CancellationToken.None);

            result.WelcomeMessage.Should().Be("Chào bạn!");
            result.AudioBase64.Should().Be("base64data");
        }
        #endregion

        #region Practice Mock Interview
        [Fact]
        public async Task GenerateQuestionAsync_ShouldReturnQuestion_WhenTurnUnderLimit()
        {
            var session = new InterviewSessionData { Id = 100, AccountId = 1, Status = "InProgress", StartTime = DateTimeOffset.UtcNow };
            _mockDataProvider.Setup(p => p.GetSessionByIdAsync(100)).ReturnsAsync(session);
            _mockDataProvider.Setup(p => p.GetResponsesBySessionIdAsync(100)).ReturnsAsync(new List<InterviewResponseData>());
            _mockInterviewAgent.Setup(a => a.GenerateQuestionAsync(session, It.IsAny<List<InterviewResponseData>>(), null, null))
                .ReturnsAsync(new GenerateQuestionResult { QuestionText = "Câu hỏi 1?" });
            _mockSpeechService.Setup(s => s.SynthesizeToBase64Async(It.IsAny<string>(), It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AzureSynthesizedSpeechResult { Text = "", Voice = "", Language = "", AudioUrl = "" });

            var result = await _orchestrator.GenerateQuestionAsync(1, 100, null, CancellationToken.None);

            result.QuestionText.Should().Be("Câu hỏi 1?");
            _mockDataProvider.Verify(p => p.CreateResponseAsync(It.IsAny<InterviewResponseData>()), Times.Once);
        }

        [Fact]
        public async Task GenerateQuestionAsync_ShouldTerminate_WhenMaxQuestionsReached()
        {
            var session = new InterviewSessionData { Id = 100, AccountId = 1, Status = "InProgress", StartTime = DateTimeOffset.UtcNow };
            _mockDataProvider.Setup(p => p.GetSessionByIdAsync(100)).ReturnsAsync(session);
            _mockInterviewAgent.Setup(a => a.GenerateQuestionAsync(session, It.IsAny<List<InterviewResponseData>>(), null, null))
                .ReturnsAsync(new GenerateQuestionResult { IsTerminated = true, TerminationReason = "MaxQuestionsReached" });

            var result = await _orchestrator.GenerateQuestionAsync(1, 100, null, CancellationToken.None);

            result.IsTerminated.Should().BeTrue();
            result.TerminationReason.Should().Be("MaxQuestionsReached");
        }

        [Fact]
        public async Task GenerateQuestionAsync_ShouldTerminate_WhenTimeLimitReached()
        {
            var oldStartTime = DateTimeOffset.UtcNow.AddMinutes(-31);
            var session = new InterviewSessionData { Id = 100, AccountId = 1, Status = "InProgress", StartTime = oldStartTime };
            _mockDataProvider.Setup(p => p.GetSessionByIdAsync(100)).ReturnsAsync(session);

            var result = await _orchestrator.GenerateQuestionAsync(1, 100, null, CancellationToken.None);

            result.IsTerminated.Should().BeTrue();
            result.TerminationReason.Should().Be("TimeLimitReached");
        }

        [Fact]
        public async Task SubmitAnswerAsync_ShouldRecordAnswer_AndReturnAiReaction()
        {
            var session = new InterviewSessionData { Id = 100, AccountId = 1, Status = "InProgress" };
            var response = new InterviewResponseData { Id = 500, InterviewSessionId = 100 };
            _mockDataProvider.Setup(p => p.GetSessionByIdAsync(100)).ReturnsAsync(session);
            _mockDataProvider.Setup(p => p.GetResponseByIdAsync(500)).ReturnsAsync(response);
            _mockInterviewAgent.Setup(a => a.GenerateReactionAsync(It.IsAny<string>(), It.IsAny<string>(), "User Answer"))
                .ReturnsAsync("Phản hồi phỏng vấn viên");

            var result = await _orchestrator.SubmitAnswerAsync(1, new SubmitAnswerRequest { InterviewSessionId = 100, InterviewResponseId = 500, UserAnswer = "User Answer" }, CancellationToken.None);
            result.AiReaction.Should().Be("Phản hồi phỏng vấn viên");
            response.UserAnswer.Should().Be("User Answer");
            _mockDataProvider.Verify(p => p.UpdateResponseAsync(response), Times.Once);
        }

        [Fact]
        public async Task EndInterviewAsync_ShouldUpdateStatus_AndStartFeedback()
        {
            var session = new InterviewSessionData { Id = 100, AccountId = 1, Status = "InProgress" };
            _mockDataProvider.Setup(p => p.GetSessionByIdAsync(100)).ReturnsAsync(session);

            await _orchestrator.EndInterviewAsync(1, 100);

            session.EndTime.Should().NotBeNull();
            _mockDataProvider.Verify(p => p.UpdateSessionAsync(session), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GenerateQuestionAsync_ShouldNotAnalyzeGap_LegacyRemoved()
        {
            var session = new InterviewSessionData { Id = 100, AccountId = 1, Status = "InProgress", StartTime = DateTimeOffset.UtcNow, CvContent = "CV", JobDescriptionText = "JD" };
            _mockDataProvider.Setup(p => p.GetSessionByIdAsync(100)).ReturnsAsync(session);
            _mockDataProvider.Setup(p => p.GetResponsesBySessionIdAsync(100)).ReturnsAsync(new List<InterviewResponseData>());
            _mockInterviewAgent.Setup(a => a.GenerateQuestionAsync(It.IsAny<InterviewSessionData>(), It.IsAny<List<InterviewResponseData>>(), null, null))
                .ReturnsAsync(new GenerateQuestionResult());

            await _orchestrator.GenerateQuestionAsync(1, 100, null, CancellationToken.None);

            _mockInterviewAgent.Verify(a => a.AnalyzeGapsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SubmitAnswerAsync_ShouldSucceed_WhenUserAnswerIsEmpty()
        {
            var session = new InterviewSessionData { Id = 100, AccountId = 1, Status = "InProgress" };
            var response = new InterviewResponseData { Id = 500, InterviewSessionId = 100 };
            _mockDataProvider.Setup(p => p.GetSessionByIdAsync(100)).ReturnsAsync(session);
            _mockDataProvider.Setup(p => p.GetResponseByIdAsync(500)).ReturnsAsync(response);

            await _orchestrator.SubmitAnswerAsync(1, new SubmitAnswerRequest { InterviewSessionId = 100, InterviewResponseId = 500, UserAnswer = "" }, CancellationToken.None);
            response.UserAnswer.Should().Be("");
            _mockDataProvider.Verify(p => p.UpdateResponseAsync(response), Times.Once);
        }

        [Fact]
        public async Task GenerateQuestionAsync_ShouldReturnQuestionText_EvenIfTtsServiceFails()
        {
            var session = new InterviewSessionData { Id = 100, AccountId = 1, Status = "InProgress", StartTime = DateTimeOffset.UtcNow };
            _mockDataProvider.Setup(p => p.GetSessionByIdAsync(100)).ReturnsAsync(session);
            _mockDataProvider.Setup(p => p.GetResponsesBySessionIdAsync(100)).ReturnsAsync(new List<InterviewResponseData>());
            _mockInterviewAgent.Setup(a => a.GenerateQuestionAsync(session, It.IsAny<List<InterviewResponseData>>(), null, null))
                .ReturnsAsync(new GenerateQuestionResult { QuestionText = "Câu hỏi 1?" });
            _mockSpeechService.Setup(s => s.SynthesizeToBase64Async(It.IsAny<string>(), It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Azure Failure"));
            var result = await _orchestrator.GenerateQuestionAsync(1, 100, null, CancellationToken.None);

            result.QuestionText.Should().Be("Câu hỏi 1?");
            result.AudioBase64.Should().BeNull();
        }
        #endregion

        #region View AI Practice History
        [Fact]
        public async Task GetInterviewHistoryAsync_ShouldReturnList_ForAccount()
        {
            var sessions = new List<InterviewSessionData> { new() { Id = 100, AccountId = 1, PositionName = "Dev" } };
            _mockDataProvider.Setup(p => p.GetSessionsByAccountIdAsync(1)).ReturnsAsync(sessions);

            var result = await _orchestrator.GetInterviewHistoryAsync(1);

            result.Should().HaveCount(1);
            result[0].PositionName.Should().Be("Dev");
        }

        [Fact]
        public async Task GetInterviewHistoryAsync_ShouldCorrectlyIdentifySessionType_BasedOnInputs()
        {
            var sessions = new List<InterviewSessionData> 
            { 
                new() { Id = 1, AccountId = 1, UserCvId = 10, InterviewType = "FullSession" },
                new() { Id = 2, AccountId = 1, QuestionId = 5, InterviewType = "Single" },
                new() { Id = 3, AccountId = 1, InterviewType = "Text" }
            };
            _mockDataProvider.Setup(p => p.GetSessionsByAccountIdAsync(1)).ReturnsAsync(sessions);
            var result = await _orchestrator.GetInterviewHistoryAsync(1);

            result.First(s => s.Id == 1).InterviewType.Should().Be("CV_JD");
            result.First(s => s.Id == 2).InterviewType.Should().Be("Single_Question");
            result.First(s => s.Id == 3).InterviewType.Should().Be("Text");
        }

        [Fact]
        public async Task GetInterviewHistoryAsync_ShouldMapPerformanceMetrics_ForListItems()
        {
            var sessions = new List<InterviewSessionData> 
            { 
                new() { Id = 1, AccountId = 1, TotalQuestionsAnswered = 5, EstimatedAbility = 0.85 } 
            };
            _mockDataProvider.Setup(p => p.GetSessionsByAccountIdAsync(1)).ReturnsAsync(sessions);

            var result = await _orchestrator.GetInterviewHistoryAsync(1);
            result[0].TotalQuestionsAnswered.Should().Be(5);
            result[0].EstimatedAbility.Should().Be(0.85);
        }
        #endregion

        #region View AI Practice History Details
        [Fact]
        public async Task GetInterviewResultAsync_ShouldReturnFullTranscriptAndScores()
        {
            var session = new InterviewSessionData { Id = 100, AccountId = 1, PositionName = "Dev", OverallFeedback = "Good" };
            var responses = new List<InterviewResponseData> { new() { Id = 1, TurnNumber = 1, QuestionContent = "Q", UserAnswer = "A" } };
            _mockDataProvider.Setup(p => p.GetSessionByIdAsync(100)).ReturnsAsync(session);
            _mockDataProvider.Setup(p => p.GetResponsesBySessionIdAsync(100)).ReturnsAsync(responses);

            var result = await _orchestrator.GetInterviewResultAsync(1, 100);

            var sessionJson = JsonSerializer.SerializeToElement(result.Session);
            var responsesJson = JsonSerializer.SerializeToElement(result.Responses);

            sessionJson.GetProperty("positionName").GetString().Should().Be("Dev");
            responsesJson.GetArrayLength().Should().Be(1);
            responsesJson[0].GetProperty("questionContent").GetString().Should().Be("Q");
        }

        [Fact]
        public async Task GetInterviewResultAsync_ShouldThrowUnauthorized_WhenNotOwner()
        {
            _mockDataProvider.Setup(p => p.GetSessionByIdAsync(100)).ReturnsAsync(new InterviewSessionData { AccountId = 1 });

            var act = () => _orchestrator.GetInterviewResultAsync(2, 100);
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task ResumeSessionAsync_ShouldReturnCurrentState_WhenValid()
        {
            var session = new InterviewSessionData { Id = 100, AccountId = 1, Status = "InProgress" };
            var responses = new List<InterviewResponseData> { new() { Id = 1, UserAnswer = "A" }, new() { Id = 2, UserAnswer = "" } };
            _mockDataProvider.Setup(p => p.GetSessionByIdAsync(100)).ReturnsAsync(session);
            _mockDataProvider.Setup(p => p.GetResponsesBySessionIdAsync(100)).ReturnsAsync(responses);

            var result = await _orchestrator.ResumeSessionAsync(1, 100);

            ((int?)result.CurrentResponseId).Should().Be(2);
            ((int)result.AnsweredCount).Should().Be(1);
        }

        [Fact]
        public async Task GetInterviewResultAsync_ShouldHandleShortSessions_WithMinimalTranscript()
        {
            var session = new InterviewSessionData { Id = 100, AccountId = 1 };
            var responses = new List<InterviewResponseData> { new() { Id = 1, UserAnswer = "A" } };
            _mockDataProvider.Setup(p => p.GetSessionByIdAsync(100)).ReturnsAsync(session);
            _mockDataProvider.Setup(p => p.GetResponsesBySessionIdAsync(100)).ReturnsAsync(responses);

            var result = await _orchestrator.GetInterviewResultAsync(1, 100);
            var responsesJson = JsonSerializer.SerializeToElement(result.Responses);
            var sessionJson = JsonSerializer.SerializeToElement(result.Session);

            responsesJson.GetArrayLength().Should().Be(1);
            sessionJson.GetProperty("totalQuestionsAnswered").GetInt32().Should().Be(1);
        }
        #endregion
    }
}
