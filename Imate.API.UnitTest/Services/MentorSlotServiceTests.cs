using Moq;
using FluentAssertions;
using Imate.API.Business.Services.Mentors;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.DataAccess.Interfaces.Mentors;
using MockQueryable;
using MockQueryable.Moq;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.UnitTest.Services
{
    public class MentorSlotServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMentorRecurringSlotRepository> _mockMentorRecurringSlotRepo;
        private readonly Mock<ISlotRepository> _mockSlotRepo;
        private readonly Mock<IMentorRepository> _mockMentorRepo;
        private readonly MentorSlotService _service;

        public MentorSlotServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMentorRecurringSlotRepo = new Mock<IMentorRecurringSlotRepository>();
            _mockSlotRepo = new Mock<ISlotRepository>();
            _mockMentorRepo = new Mock<IMentorRepository>();

            _mockUnitOfWork.Setup(u => u.MentorRecurringSlots).Returns(_mockMentorRecurringSlotRepo.Object);
            _mockUnitOfWork.Setup(u => u.Slots).Returns(_mockSlotRepo.Object);
            _mockUnitOfWork.Setup(u => u.Mentors).Returns(_mockMentorRepo.Object);

            _service = new MentorSlotService(_mockUnitOfWork.Object);
        }

        #region View Mentor Slots
        [Fact]
        public async Task GetMentorRecurringSlotsAsync_ShouldReturnGroupedSlots()
        {
            // Arrange
            var mentorId = 1;
            var mentorSlots = new List<MentorRecurringSlot>
            {
                new MentorRecurringSlot 
                { 
                    Id = 1, MentorId = mentorId, SlotId = 10,
                    Slot = new Slot { Id = 10, DayOfWeek = 1, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0) }
                },
                new MentorRecurringSlot 
                { 
                    Id = 2, MentorId = mentorId, SlotId = 11,
                    Slot = new Slot { Id = 11, DayOfWeek = 1, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(15, 0) }
                },
                new MentorRecurringSlot 
                { 
                    Id = 3, MentorId = mentorId, SlotId = 20,
                    Slot = new Slot { Id = 20, DayOfWeek = 2, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0) }
                }
            };

            _mockMentorRecurringSlotRepo.Setup(r => r.GetByMentorIdAsync(mentorId)).ReturnsAsync(mentorSlots);

            // Act
            var result = await _service.GetMentorRecurringSlotsAsync(mentorId);

            // Assert
            result.MentorId.Should().Be(mentorId);
            result.SlotsByDay.Should().HaveCount(2); // Monday and Tuesday
            result.SlotsByDay.First(d => d.DayOfWeek == 1).Slots.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllSlotsAsync_ShouldReturnAllSlots()
        {
            // Arrange
            var slots = new List<Slot>
            {
                new Slot { Id = 1, DayOfWeek = 1, StartTime = new TimeOnly(8,0), EndTime = new TimeOnly(9,0) },
                new Slot { Id = 2, DayOfWeek = 1, StartTime = new TimeOnly(9,0), EndTime = new TimeOnly(10,0) }
            }.AsQueryable().BuildMock();

            _mockSlotRepo.Setup(r => r.FindAll(false)).Returns(slots);

            // Act
            var result = await _service.GetAllSlotsAsync();

            // Assert
            result.Should().HaveCount(2);
        }
        #endregion

        #region Set Available Slot
        [Fact]
        public async Task AddMentorRecurringSlotsAsync_ShouldAddNewSlots_WhenTheyDoNotExist()
        {
            // Arrange
            var mentorId = 1;
            var slotIds = new List<int> { 10, 11 };
            var existingMentor = new Mentor { AccountId = mentorId };
            var existingSlots = new List<MentorRecurringSlot>
            {
                new MentorRecurringSlot { MentorId = mentorId, SlotId = 10 }
            };

            _mockMentorRepo.Setup(r => r.GetMentorByIdAsync(mentorId)).ReturnsAsync(existingMentor);
            _mockMentorRecurringSlotRepo.Setup(r => r.GetByMentorIdAsync(mentorId)).ReturnsAsync(existingSlots);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.AddMentorRecurringSlotsAsync(mentorId, slotIds);

            // Assert
            result.Should().BeTrue();
            _mockMentorRecurringSlotRepo.Verify(r => r.Create(It.Is<MentorRecurringSlot>(ms => ms.SlotId == 11)), Times.Once);
            _mockMentorRecurringSlotRepo.Verify(r => r.Create(It.Is<MentorRecurringSlot>(ms => ms.SlotId == 10)), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
        #endregion

        #region Remove Available Slot
        [Fact]
        public async Task DeleteMentorRecurringSlotAsync_ShouldReturnTrue_WhenSlotExists()
        {
            // Arrange
            var mentorId = 1;
            var mentorRecurringSlotId = 100;
            var mentorSlot = new MentorRecurringSlot { Id = mentorRecurringSlotId, MentorId = mentorId, IsActive = true };
            
            var mockQuery = new List<MentorRecurringSlot> { mentorSlot }.AsQueryable().BuildMock();

            _mockMentorRecurringSlotRepo.Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorRecurringSlot, bool>>>(), true))
                .Returns(mockQuery);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteMentorRecurringSlotAsync(mentorId, mentorRecurringSlotId);

            // Assert
            result.Should().BeTrue();
            mentorSlot.IsActive.Should().BeFalse();
            _mockMentorRecurringSlotRepo.Verify(r => r.Update(mentorSlot), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteMentorRecurringSlotAsync_ShouldReturnFalse_WhenSlotDoesNotExist()
        {
            // Arrange
            var mockQuery = new List<MentorRecurringSlot>().AsQueryable().BuildMock();
            _mockMentorRecurringSlotRepo.Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorRecurringSlot, bool>>>(), true))
                .Returns(mockQuery);

            // Act
            var result = await _service.DeleteMentorRecurringSlotAsync(1, 999);

            // Assert
            result.Should().BeFalse();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }
        #endregion
    }
}
