using System.Text;
using System.Text.Json;
using FluentAssertions;
using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Core.Orchestrators;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Business.Services;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Imate.API.UnitTest.Services
{
    public class CvServiceTests
    {
        private readonly Mock<IUserCvRepository> _mockCvRepo;
        private readonly Mock<IAwsS3StorageService> _mockS3Storage;
        private readonly Mock<ICvAnalysisOrchestrator> _mockCvAnalysisOrchestrator;
        private readonly Mock<ICvAnalysisAgent> _mockCvAnalysisAgent;
        private readonly Mock<IGeminiService> _mockGeminiService;
        private readonly Mock<ICvDataProvider> _mockCvDataProvider;
        private readonly Mock<ILogger<CvService>> _mockCvServiceLogger;
        private readonly Mock<ILogger<CvAnalysisOrchestrator>> _mockOrchestratorLogger;
        private readonly Mock<ILogger<CvDataProvider>> _mockDataProviderLogger;
        
        private readonly CvService _cvService;
        private readonly CvAnalysisOrchestrator _orchestrator;
        private readonly CvDataProvider _dataProvider;

        public CvServiceTests()
        {
            _mockCvRepo = new Mock<IUserCvRepository>();
            _mockS3Storage = new Mock<IAwsS3StorageService>();
            _mockCvAnalysisOrchestrator = new Mock<ICvAnalysisOrchestrator>();
            _mockCvAnalysisAgent = new Mock<ICvAnalysisAgent>();
            _mockGeminiService = new Mock<IGeminiService>();
            _mockCvDataProvider = new Mock<ICvDataProvider>();
            _mockCvServiceLogger = new Mock<ILogger<CvService>>();
            _mockOrchestratorLogger = new Mock<ILogger<CvAnalysisOrchestrator>>();
            _mockDataProviderLogger = new Mock<ILogger<CvDataProvider>>();

            _cvService = new CvService(
                _mockCvRepo.Object, 
                _mockS3Storage.Object, 
                _mockCvAnalysisOrchestrator.Object
            );

            _orchestrator = new CvAnalysisOrchestrator(
                _mockCvAnalysisAgent.Object,
                _mockGeminiService.Object,
                _mockOrchestratorLogger.Object,
                _mockCvDataProvider.Object
            );

            _dataProvider = new CvDataProvider(
                _mockCvRepo.Object, 
                _mockS3Storage.Object, 
                _mockDataProviderLogger.Object
            );
        }

        private Mock<IFormFile> CreateMockFile(string fileName, long length, string contentType)
        {
            var fileMock = new Mock<IFormFile>();
            var content = "dummy cv content";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(length >= 0 ? length : ms.Length);
            fileMock.Setup(_ => _.ContentType).Returns(contentType);
            return fileMock;
        }

        #region Import CV
        [Fact]
        public async Task UploadCvAsync_ShouldSucceed_WhenPdfFileIsValid()
        {
            var accountId = 1;
            var fileMock = CreateMockFile("test.pdf", 1024, "application/pdf");
            _mockS3Storage.Setup(s => s.UploadFileAsync(fileMock.Object, "cv")).ReturnsAsync("s3://url/test.pdf");

            var result = await _cvService.UploadCvAsync(accountId, fileMock.Object, "My CV");

            result.Should().NotBeNull();
            result.FileName.Should().Be("My CV");
            result.FileUrl.Should().Be("s3://url/test.pdf");
            _mockCvRepo.Verify(r => r.AddAsync(It.IsAny<UserCv>()), Times.Once);
            _mockCvAnalysisOrchestrator.Verify(o => o.ValidateCvIsItAsync(fileMock.Object), Times.Once);
        }
        [Fact]
        public async Task UploadCvAsync_ShouldSucceed_WhenDocxFileIsValid()
        {
            var accountId = 1;
            var fileMock = CreateMockFile("test.docx", 1024, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            _mockS3Storage.Setup(s => s.UploadFileAsync(fileMock.Object, "cv")).ReturnsAsync("s3://url/test.docx");

            var result = await _cvService.UploadCvAsync(accountId, fileMock.Object, "My Docx CV");

            result.FileUrl.Should().Be("s3://url/test.docx");
            _mockCvRepo.Verify(r => r.AddAsync(It.IsAny<UserCv>()), Times.Once);
        }

        [Fact]
        public async Task UploadCvAsync_ShouldSetCustomFileName_WhenProvided()
        {
            var fileMock = CreateMockFile("original.pdf", 1024, "application/pdf");
            _mockS3Storage.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "cv")).ReturnsAsync("url");

            var result = await _cvService.UploadCvAsync(1, fileMock.Object, "Custom Name");

            result.FileName.Should().Be("Custom Name");
        }

        [Fact]
        public async Task UploadCvAsync_ShouldThrowArgumentException_WhenFileIsEmpty()
        {
            var fileMock = CreateMockFile("empty.pdf", 0, "application/pdf");

            var act = () => _cvService.UploadCvAsync(1, fileMock.Object, "Empty");

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*chọn file CV*");
        }

        [Fact]
        public async Task UploadCvAsync_ShouldThrowArgumentException_WhenFileSizeExceedsLimit()
        {
            var fileMock = CreateMockFile("large.pdf", 6 * 1024 * 1024, "application/pdf");
            var act = () => _cvService.UploadCvAsync(1, fileMock.Object, "Large");

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*5MB*");
        }

        [Fact]
        public async Task UploadCvAsync_ShouldThrowArgumentException_WhenExtensionIsInvalid()
        {
            var fileMock = CreateMockFile("test.exe", 1024, "application/x-msdownload");

            var act = () => _cvService.UploadCvAsync(1, fileMock.Object, "Bad Ext");

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*PDF hoặc DOCX*");
        }

        [Fact]
        public async Task UploadCvAsync_ShouldThrowArgumentException_WhenContentTypeIsInvalid()
        {
            var fileMock = CreateMockFile("test.pdf", 1024, "image/jpeg");
            var act = () => _cvService.UploadCvAsync(1, fileMock.Object, "Bad Type");

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*PDF hoặc DOCX*");
        }

        [Fact]
        public async Task ValidateCvIsItAsync_ShouldThrowArgumentException_WhenAiDeterminesNonIt()
        {
            var fileMock = CreateMockFile("chef.pdf", 1024, "application/pdf");
            var aiResponse = "{\"is_it_cv\": false, \"reason\": \"Not IT\"}";
            _mockGeminiService.Setup(g => g.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(aiResponse);

            var act = () => _orchestrator.ValidateCvIsItAsync(fileMock.Object);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task UploadCvAsync_ShouldVerifyS3Sync_BeforeDatabaseRecordFinalized()
        {
            var fileMock = CreateMockFile("sync.pdf", 1024, "application/pdf");
            _mockS3Storage.Setup(s => s.UploadFileAsync(fileMock.Object, "cv")).ReturnsAsync("url");
            await _cvService.UploadCvAsync(1, fileMock.Object, "Sync Test");

            _mockS3Storage.Verify(s => s.UploadFileAsync(fileMock.Object, "cv"), Times.Once);
            _mockCvRepo.Verify(r => r.AddAsync(It.Is<UserCv>(cv => cv.FileUrl == "url")), Times.Once);
        }
        #endregion

        #region Delete CV
        [Fact]
        public async Task DeleteCvAsync_ShouldSucceed_WhenOwnerDeletes()
        {
            var cv = new UserCv { Id = 10, AccountId = 1, FileUrl = "url" };
            _mockCvRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(cv);

            await _cvService.DeleteCvAsync(1, 10);

            _mockS3Storage.Verify(s => s.DeleteFileAsync("url"), Times.Once);
            _mockCvRepo.Verify(r => r.DeleteAsync(cv), Times.Once);
        }

        [Fact]
        public async Task DeleteCvAsync_ShouldThrowException_WhenCvIdDoesNotExist()
        {
            _mockCvRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((UserCv?)null);
            var act = () => _cvService.DeleteCvAsync(1, 99);

            await act.Should().ThrowAsync<Exception>().WithMessage("*không tồn tại*");
        }

        [Fact]
        public async Task DeleteCvAsync_ShouldThrowUnauthorized_WhenNotOwner()
        {
            var cv = new UserCv { Id = 10, AccountId = 1 };
            _mockCvRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(cv);

            var act = () => _cvService.DeleteCvAsync(2, 10);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task DeleteCvAsync_ShouldVerifyS3FileCleanup_WhenCvDeleted()
        {
            var cv = new UserCv { Id = 1, AccountId = 1, FileUrl = "s3://to-be-deleted" };
            _mockCvRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cv);

            await _cvService.DeleteCvAsync(1, 1);

            _mockS3Storage.Verify(s => s.DeleteFileAsync("s3://to-be-deleted"), Times.Once);
        }
        #endregion

        #region Analyse CV
        [Fact]
        public async Task AnalyseCvAsync_ShouldReturnCachedResponse_WhenValidCacheExists()
        {
            var accountId = 1;
            var cvId = 10;
            var cachedJson = JsonSerializer.Serialize(new CvAnalysisResponse { CandidateName = "Cached User" });
            _mockCvDataProvider.Setup(p => p.GetCachedAnalysisAsync(accountId, cvId)).ReturnsAsync(cachedJson);

            var result = await _orchestrator.AnalyseCvAsync(accountId, new AnalyseCvRequest { CvId = cvId });

            result.CandidateName.Should().Be("Cached User");
            _mockCvAnalysisAgent.Verify(a => a.AnalyseCvAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AnalyseCvAsync_ShouldCallAgent_WhenNoCacheExists()
        {
            var accountId = 1;
            var cvId = 10;
            _mockCvDataProvider.Setup(p => p.GetCachedAnalysisAsync(accountId, cvId)).ReturnsAsync((string?)null);
            _mockCvDataProvider.Setup(p => p.GetCvTextAsync(accountId, cvId)).ReturnsAsync("Extracted Text");
            _mockCvAnalysisAgent.Setup(a => a.AnalyseCvAsync("Extracted Text")).ReturnsAsync(new CvAnalysisResponse { CandidateName = "New AI Analysis" });

            var result = await _orchestrator.AnalyseCvAsync(accountId, new AnalyseCvRequest { CvId = cvId });

            result.CandidateName.Should().Be("New AI Analysis");
            _mockCvDataProvider.Verify(p => p.SaveAnalysisResultAsync(accountId, cvId, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task AnalyseCvAsync_ShouldForceReanalyze_WhenForceFlagIsTrue()
        {
            var accountId = 1;
            var cvId = 10;
            _mockCvDataProvider.Setup(p => p.GetCvTextAsync(accountId, cvId)).ReturnsAsync("Text");
            _mockCvAnalysisAgent.Setup(a => a.AnalyseCvAsync(It.IsAny<string>())).ReturnsAsync(new CvAnalysisResponse());

            await _orchestrator.AnalyseCvAsync(accountId, new AnalyseCvRequest { CvId = cvId, ForceReanalyze = true });

            _mockCvDataProvider.Verify(p => p.ClearScannedDataAsync(accountId, cvId), Times.Once);
            _mockCvAnalysisAgent.Verify(a => a.AnalyseCvAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task AnalyseCvAsync_ShouldHandleJsonException_ByReanalyzing()
        {
            var accountId = 1;
            var cvId = 10;
            _mockCvDataProvider.Setup(p => p.GetCachedAnalysisAsync(accountId, cvId)).ReturnsAsync("invalid json");
            _mockCvDataProvider.Setup(p => p.GetCvTextAsync(accountId, cvId)).ReturnsAsync("Text");
            _mockCvAnalysisAgent.Setup(a => a.AnalyseCvAsync(It.IsAny<string>())).ReturnsAsync(new CvAnalysisResponse());

            await _orchestrator.AnalyseCvAsync(accountId, new AnalyseCvRequest { CvId = cvId });

            _mockCvDataProvider.Verify(p => p.SaveAnalysisResultAsync(accountId, cvId, null), Times.Once);
            _mockCvAnalysisAgent.Verify(a => a.AnalyseCvAsync(It.IsAny<string>()), Times.Once);
        }
        [Fact]
        public async Task AnalyseCvAsync_ShouldIncludeSkillsWeaknessesAndSuggestions_InResponse()
        {
            var accountId = 1;
            var cvId = 1;
            var response = new CvAnalysisResponse();
            response.Strengths.Add(new CvInsightDto { Title = "Skill", Description = "Great" });
            response.Improvements.Add(new CvInsightDto { Title = "Weakness", Description = "Needs work" });
            
            _mockCvDataProvider.Setup(p => p.GetCvTextAsync(accountId, cvId)).ReturnsAsync("Text");
            _mockCvAnalysisAgent.Setup(a => a.AnalyseCvAsync(It.IsAny<string>())).ReturnsAsync(response);

            var result = await _orchestrator.AnalyseCvAsync(accountId, new AnalyseCvRequest { CvId = cvId });

            result.Strengths.Should().NotBeEmpty();
            result.Improvements.Should().NotBeEmpty();
        }

        [Fact]
        public async Task AnalyseCvAsync_ShouldThrowArgumentException_WhenNoIdOrTextProvided()
        {
            var act = () => _orchestrator.AnalyseCvAsync(1, new AnalyseCvRequest());
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*CvId hoặc CvText*");
        }
        #endregion
    }
}
