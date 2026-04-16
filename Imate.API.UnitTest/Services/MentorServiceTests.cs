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

        #region View Mentors
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
        #endregion

        #region Edit Service Price
        [Fact]
        public async Task UpdateMentorPriceAsync_ShouldUpdatePrice_WhenMentorExists()
        {
            // Arrange
            var mentorId = 1;
            var mentor = new Mentor { AccountId = mentorId, PricePerSession = 100 };
            
            _mockUnitOfWork.Setup(u => u.Mentors.GetMentorByIdAsync(mentorId)).ReturnsAsync(mentor);
            _mockUnitOfWork.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.UpdateMentorPriceAsync(mentorId, 200);

            // Assert
            mentor.PricePerSession.Should().Be(200);
            _mockUnitOfWork.Verify(u => u.Mentors.Update(mentor), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateMentorPriceAsync_ShouldThrowNotFound_WhenMentorDoesNotExist()
        {
            _mockUnitOfWork.Setup(u => u.Mentors.GetMentorByIdAsync(It.IsAny<int>())).ReturnsAsync((Mentor?)null);

            var act = () => _service.UpdateMentorPriceAsync(999, 200);

            await act.Should().ThrowAsync<Imate.API.Business.Exceptions.NotFoundException>();
        }

        [Fact]
        public async Task UpdateMentorProfileAsync_ShouldUpdatePriceAndBio()
        {
            // Arrange
            var mentorId = 1;
            var mentor = new Mentor { AccountId = mentorId, PricePerSession = 100, Bio = "Old" };
            var request = new Imate.API.Presentation.RequestModels.UserManagement.UpdateMentorProfileRequest 
            { 
                Bio = "New", 
                PricePerSession = 200, 
                Phone = "123",
                BirthDate = "1990-01-01" 
            };

            _mockUnitOfWork.Setup(u => u.Mentors.GetMentorByIdAsync(mentorId)).ReturnsAsync(mentor);

            // Act
            await _service.UpdateMentorProfileAsync(mentorId, request);

            // Assert
            mentor.Bio.Should().Be("New");
            mentor.PricePerSession.Should().Be(200);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
        #endregion

        #region Receive Rating
        [Fact]
        public async Task GetCandidateRatingsAsync_ShouldReturnRatings()
        {
            // Arrange
            var mentorId = 1;
            var mentor = new Mentor { AccountId = mentorId };
            var ratings = new List<Imate.API.Presentation.ResponseModels.Mentors.RatingDetailModel>
            {
                new() { RatingScore = 5, ReviewText = "Good" },
                new() { RatingScore = 4, ReviewText = "Nice" }
            };

            var mockBookingRepo = new Mock<Imate.API.DataAccess.Interfaces.Mentors.IBookingRepository>();
            _mockUnitOfWork.Setup(u => u.Mentors.GetMentorByIdAsync(mentorId)).ReturnsAsync(mentor);
            _mockUnitOfWork.Setup(u => u.Bookings).Returns(mockBookingRepo.Object);
            mockBookingRepo.Setup(r => r.GetCandidateRatingsByMentorIdAsync(mentorId)).ReturnsAsync(ratings);

            // Act
            var result = await _service.GetCandidateRatingsAsync(mentorId);

            // Assert
            result.TotalRatingCount.Should().Be(2);
            result.AverageRating.Should().Be(4.5m);
        }

        [Fact]
        public async Task GetCandidateRatingsAsync_ShouldReturnNullAverage_WhenNoRatings()
        {
            // Arrange
            var mentorId = 1;
            _mockUnitOfWork.Setup(u => u.Mentors.GetMentorByIdAsync(mentorId)).ReturnsAsync(new Mentor());
            var mockBookingRepo = new Mock<Imate.API.DataAccess.Interfaces.Mentors.IBookingRepository>();
            _mockUnitOfWork.Setup(u => u.Bookings).Returns(mockBookingRepo.Object);
            mockBookingRepo.Setup(r => r.GetCandidateRatingsByMentorIdAsync(mentorId)).ReturnsAsync(new List<Imate.API.Presentation.ResponseModels.Mentors.RatingDetailModel>());

            // Act
            var result = await _service.GetCandidateRatingsAsync(mentorId);

            // Assert
            result.AverageRating.Should().BeNull();
        }
        #endregion
    }
}
