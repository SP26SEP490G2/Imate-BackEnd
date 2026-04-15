using Moq;
using FluentAssertions;
using Imate.API.Business.Services.Mentors;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Business.Helper;
using MockQueryable;
using MockQueryable.Moq;
using Xunit;

namespace Imate.API.UnitTest.Services
{
    public class MentorServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly MentorService _service;

        public MentorServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _service = new MentorService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetListPreviewMentorsAsync_ShouldReturnActiveMentors_WhenNoFiltersApplied()
        {
            // Arrange
            var mentors = new List<Mentor>
            {
                new Mentor 
                { 
                    AccountId = 1, 
                    Account = new Account { Id = 1, FullName = "Mentor 1", Status = AccountStatus.Active },
                    MentorPositions = new List<MentorPosition> { new() { Position = new Position { Name = "Pos 1" } } },
                    MentorCompanies = new List<MentorCompany> { new() { Company = new Company { Name = "Comp 1" } } },
                    AvgRatings = 4.5m,
                    TotalRatingCount = 10
                }
            }.AsQueryable().BuildMock();

            _mockUnitOfWork.Setup(u => u.Mentors.FindAll(false)).Returns(mentors);
            var mentorParams = new CommonParams { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetListPreviewMentorsAsync(mentorParams);

            // Assert
            result.Items.Should().HaveCount(1);
            _mockUnitOfWork.Verify(u => u.Mentors.FindAll(false), Times.Once);
        }

        [Theory]
        [InlineData("Alice", null, null, null, null, 1)] // SearchTerm
        [InlineData(null, 10, null, null, null, 1)]    // PositionId
        [InlineData(null, null, "Developer", null, null, 1)] // PositionName
        [InlineData(null, null, null, "C#", null, 1)]    // SkillName
        [InlineData(null, null, null, null, "Google", 1)]// CompanyName
        public async Task GetListPreviewMentorsAsync_ShouldFilterCorrectly(string? search, int? posId, string? posName, string? skillName, string? compName, int expectedCount)
        {
            // Arrange
            var mentors = new List<Mentor>
            {
                new Mentor 
                { 
                    AccountId = 1, 
                    Account = new Account { Id = 1, FullName = "Alice", Status = AccountStatus.Active },
                    MentorPositions = new List<MentorPosition> { new() { PositionId = 10, Position = new Position { Name = "Developer" } } },
                    MentorSkills = new List<MentorSkill> { new() { Skill = new Skill { Name = "C#" } } },
                    MentorCompanies = new List<MentorCompany> { new() { Company = new Company { Name = "Google" } } },
                    Bio = "Expert"
                },
                new Mentor 
                { 
                    AccountId = 2, 
                    Account = new Account { Id = 2, FullName = "Bob", Status = AccountStatus.Active },
                    MentorPositions = new List<MentorPosition>(),
                    MentorSkills = new List<MentorSkill>(),
                    MentorCompanies = new List<MentorCompany>(),
                    Bio = "Noob"
                }
            }.AsQueryable().BuildMock();

            _mockUnitOfWork.Setup(u => u.Mentors.FindAll(false)).Returns(mentors);
            var mentorParams = new CommonParams 
            { 
                SearchTerm = search, 
                PositionId = posId, 
                PositionName = posName, 
                SkillName = skillName, 
                CompanyName = compName,
                PageNumber = 1, PageSize = 10 
            };

            // Act
            var result = await _service.GetListPreviewMentorsAsync(mentorParams);

            // Assert
            result.Items.Should().HaveCount(expectedCount);
        }

        [Fact]
        public async Task GetListPreviewMentorsAsync_ShouldThrowApplicationException_WhenErrorOccurs()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Mentors.FindAll(false)).Throws(new System.Exception("DB Error"));

            // Act
            var act = () => _service.GetListPreviewMentorsAsync(new CommonParams());

            // Assert
            await act.Should().ThrowAsync<ApplicationException>().WithMessage("An error occurred while retrieving mentors.");
        }
    }
}
