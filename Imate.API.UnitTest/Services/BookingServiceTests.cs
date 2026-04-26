using Moq;
using FluentAssertions;
using Imate.API.Business.Services.Mentors;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.Mentors;
using Imate.API.Business.Exceptions;
using Microsoft.Extensions.Configuration;
using Xunit;
using MockQueryable;
using MockQueryable.Moq;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Imate.API.DataAccess.Interfaces.Payment;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.UnitTest.Services
{
    public class BookingServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly BookingService _service;

        private readonly Mock<IBookingRepository> _mockBookingRepo;
        private readonly Mock<IMentorRepository> _mockMentorRepo;
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<ISlotRepository> _mockSlotRepo;
        private readonly Mock<ITransactionRepository> _mockTransactionRepo;

        public BookingServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockBookingRepo = new Mock<IBookingRepository>();
            _mockMentorRepo = new Mock<IMentorRepository>();
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockSlotRepo = new Mock<ISlotRepository>();
            _mockTransactionRepo = new Mock<ITransactionRepository>();

            _mockUnitOfWork.Setup(u => u.Bookings).Returns(_mockBookingRepo.Object);
            _mockUnitOfWork.Setup(u => u.Mentors).Returns(_mockMentorRepo.Object);
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(_mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.Slots).Returns(_mockSlotRepo.Object);
            _mockUnitOfWork.Setup(u => u.Transactions).Returns(_mockTransactionRepo.Object);

            _service = new BookingService(_mockUnitOfWork.Object, _mockConfiguration.Object);
        }

        #region CreateBookingAsync

        [Fact]
        public async Task CreateBookingAsync_ShouldSuccess()
        {
            var candidateId = 1;
            var request = new BookingCreateRequest { MentorId = 2, SlotId = 10, BookDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) };
            var mentor = new Mentor { AccountId = 2, PricePerSession = 100 };
            var mentorAccount = new Account { Id = 2, FullName = "Mentor Name" };
            var candidateAccount = new Account { Id = 1, Balance = 500 };
            var slot = new Slot { Id = 10, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0) };

            _mockMentorRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(mentor);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(mentorAccount as Account);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(candidateAccount as Account);
            _mockBookingRepo.Setup(r => r.HasMentorRecurringSlotAsync(2, 10)).ReturnsAsync(true);
            _mockSlotRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(slot);
            _mockBookingRepo.Setup(r => r.IsSlotAvailableAsync(2, 10, request.BookDate)).ReturnsAsync(true);
            _mockBookingRepo.Setup(r => r.HasCandidateBookingAtTimeAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(false);

            var result = await _service.CreateBookingAsync(request, candidateId);

            result.Should().NotBeNull();
            candidateAccount.Balance.Should().Be(400);
            _mockBookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_MentorNotFound()
        {
            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Mentor?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateBookingAsync(new BookingCreateRequest { MentorId = 1 }, 2));
        }
        [Fact]
        public async Task CreateBookingAsync_MentorAccountNotFound()
        {
            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Account?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateBookingAsync(new BookingCreateRequest { MentorId = 1 }, 2));
        }
        [Fact]
        public async Task CreateBookingAsync_CandidateAccountNotFound()
        {
            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Account());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync((Account?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateBookingAsync(new BookingCreateRequest { MentorId = 1 }, 2));
        }
        [Fact]
        public async Task CreateBookingAsync_SlotNotBelongToMentor()
        {
            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Account());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Account());
            _mockBookingRepo.Setup(r => r.HasMentorRecurringSlotAsync(1, 10)).ReturnsAsync(false);

            await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateBookingAsync(new BookingCreateRequest { MentorId = 1, SlotId = 10 }, 2));
        }
        [Fact]
        public async Task CreateBookingAsync_SlotNotFound()
        {
            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Account());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Account());
            _mockBookingRepo.Setup(r => r.HasMentorRecurringSlotAsync(1, 10)).ReturnsAsync(true);
            _mockSlotRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Slot?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateBookingAsync(new BookingCreateRequest { MentorId = 1, SlotId = 10 }, 2));
        }
        [Fact]
        public async Task CreateBookingAsync_DateTooFar()
        {
            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
                new Mentor()); _mockAccountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Account());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Account());
            _mockBookingRepo.Setup(r => r.HasMentorRecurringSlotAsync(1, 10)).ReturnsAsync(true);
            _mockSlotRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Slot());

            var req = new BookingCreateRequest { MentorId = 1, SlotId = 10, BookDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)) };

            await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateBookingAsync(req, 2));
        }
        [Fact]
        public async Task CreateBookingAsync_SlotUnavailable()
        {
            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Account());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Account());
            _mockBookingRepo.Setup(r => r.HasMentorRecurringSlotAsync(1, 10)).ReturnsAsync(true);
            _mockSlotRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Slot { StartTime = new TimeOnly(23, 0) });
            _mockBookingRepo.Setup(r => r.IsSlotAvailableAsync(1, 10, It.IsAny<DateOnly>())).ReturnsAsync(false);

            var req = new BookingCreateRequest { MentorId = 1, SlotId = 10, BookDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) };

            await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateBookingAsync(req, 2));

        }
        [Fact]
        public async Task CreateBookingAsync_CandidateBusy()
        {
            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Account());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Account());
            _mockBookingRepo.Setup(r => r.HasMentorRecurringSlotAsync(1, 10)).ReturnsAsync(true);
            _mockSlotRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Slot { StartTime = new TimeOnly(23, 0), EndTime = new TimeOnly(23, 30) });
            _mockBookingRepo.Setup(r => r.IsSlotAvailableAsync(1, 10, It.IsAny<DateOnly>())).ReturnsAsync(true);
            _mockBookingRepo.Setup(r => r.HasCandidateBookingAtTimeAsync(2, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(true);

            var req = new BookingCreateRequest { MentorId = 1, SlotId = 10, BookDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) };

            await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateBookingAsync(req, 2));
        }
        [Fact]
        public async Task CreateBookingAsync_InsufficientBalance()
        {
            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor { PricePerSession = 100 });
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Account());
            _mockAccountRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Account { Balance = 10 });
            _mockBookingRepo.Setup(r => r.HasMentorRecurringSlotAsync(1, 10)).ReturnsAsync(true);
            _mockSlotRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Slot { StartTime = new TimeOnly(23, 0) });
            _mockBookingRepo.Setup(r => r.IsSlotAvailableAsync(1, 10, It.IsAny<DateOnly>())).ReturnsAsync(true);
            _mockBookingRepo.Setup(r => r.HasCandidateBookingAtTimeAsync(2, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(false);

            var req = new BookingCreateRequest { MentorId = 1, SlotId = 10, BookDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) };

            await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateBookingAsync(req, 2));
        }

        #endregion

        #region GetBookings

        [Fact]
        public async Task GetCandidateBookingsAsync_ShouldReturnList()
        {
            var bookings = new List<Booking>
            {
                new Booking
                {
                    Id = 1, CandidateId = 1, BookDate = new DateOnly(2026, 1, 1),
                    StartTime = DateTimeOffset.UtcNow,
                    Mentor = new Mentor { Account = new Account { FullName = "M", AvatarUrl = "url" } },
                    AgoraChannelName = "room1", PriceAtBooking = 100
                }
            }.AsQueryable().BuildMock();

            _mockBookingRepo.Setup(r => r.GetAllBookings()).Returns(bookings);

            var slots = new List<Slot>
            {
                new Slot { DayOfWeek = (int)new DateOnly(2026, 1, 1).DayOfWeek, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0) }
            }.AsQueryable().BuildMock();
            _mockSlotRepo.Setup(r => r.FindAll(false)).Returns(slots);

            var result = await _service.GetCandidateBookingsAsync(1);

            result.Should().HaveCount(1);
            result[0].ProfileName.Should().Be("M");
        }

        [Fact]
        public async Task GetMentorBookingsAsync_ShouldReturnListAndExerciseAutoComplete()
        {
            var bookings = new List<Booking>
            {
                new Booking
                {
                    Id = 1, MentorId = 1, BookDate = new DateOnly(2026, 1, 1),
                    StartTime = DateTimeOffset.UtcNow,
                    Candidate = new Account { FullName = "C", AvatarUrl = "url" },
                    AgoraChannelName = "room1", PriceAtBooking = 100
                },
                new Booking
                {
                    Id = 2, MentorId = 1, StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                    Status = BookingStatus.Confirmed,
                    Candidate = new Account { FullName = "C2", AvatarUrl = "url2" }
                }
            }.AsQueryable().BuildMock();

            _mockBookingRepo.Setup(r => r.GetAllBookings()).Returns(bookings);
            _mockSlotRepo.Setup(r => r.FindAll(false)).Returns(new List<Slot>().AsQueryable().BuildMock());

            var result = await _service.GetMentorBookingsAsync(1);

            result.Should().HaveCount(2);
            result.Should().Contain(r => r.BookingId == 2 && r.Status == BookingStatus.Completed);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce); // Due to auto-complete
        }

        [Fact]
        public async Task GetBookedSlotsByMentorIdAsync_ShouldReturnSlots()
        {
            var mentorId = 1;
            var bookings = new List<Booking>
            {
                new Booking
                {
                    Id = 123, MentorId = mentorId, BookDate = new DateOnly(2026, 1, 1),
                    StartTime = DateTimeOffset.UtcNow, Status = BookingStatus.Confirmed,
                    Candidate = new Account { FullName = "C" }
                }
            };

            _mockBookingRepo.Setup(r => r.GetMentorUpcomingBookingsAsync(mentorId, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(bookings);

            var result = await _service.GetBookedSlotsByMentorIdAsync(mentorId);

            result.Should().HaveCount(1);
            result[0].BookingId.Should().Be(123);
        }
        #endregion

        #region Session History
        [Fact]
        public async Task GetMentorCompletedBookingsSummaryAsync_ShouldReturnList()
        {
            var mentorId = 1;
            var bookings = new List<Booking>
            {
                new Booking
                {
                    Id = 101, MentorId = mentorId, CandidateId = 2,
                    Status = BookingStatus.Completed, StartTime = DateTimeOffset.UtcNow,
                    Candidate = new Account { FullName = "Candidate 2" }
                }
            };

            _mockMentorRepo.Setup(r => r.GetByIdAsync(mentorId)).ReturnsAsync(new Mentor());
            _mockBookingRepo.Setup(r => r.GetMentorCompletedBookingsAsync(mentorId)).ReturnsAsync(bookings);
            _mockBookingRepo.Setup(r => r.GetAllBookings()).Returns(new List<Booking>().AsQueryable().BuildMock());

            var result = await _service.GetMentorCompletedBookingsSummaryAsync(mentorId);

            result.Should().HaveCount(1);
            result[0].CandidateName.Should().Be("Candidate 2");
        }

        [Fact]
        public async Task GetMentorCompletedBookingsSummaryAsync_MentorNotFound()
        {
            _mockMentorRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Mentor?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetMentorCompletedBookingsSummaryAsync(1));
        }

        [Fact]
        public async Task GetCandidateCompletedBookingsSummaryAsync_ShouldReturnList()
        {
            var candidateId = 1;
            var bookings = new List<Booking>
            {
                new Booking
                {
                    Id = 201, CandidateId = candidateId, Status = BookingStatus.Completed,
                    Mentor = new Mentor { Account = new Account { FullName = "Mentor 1" } }
                }
            }.AsQueryable().BuildMock();

            _mockBookingRepo.Setup(r => r.GetAllBookings()).Returns(bookings);

            var result = await _service.GetCandidateCompletedBookingsSummaryAsync(candidateId);

            result.Should().HaveCount(1);
            result[0].MentorName.Should().Be("Mentor 1");
        }
        #endregion

        #region Session Details

        [Fact]
        public async Task GetCandidateSessionDetailAsync_Success()
        {
            var booking = new Booking { Id = 1, CandidateId = 1, AgoraChannelName = "r", Mentor = new Mentor { Account = new Account { FullName = "M" } }, BookDate = new DateOnly(2026, 1, 1), StartTime = DateTimeOffset.UtcNow };
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(booking);
            _mockSlotRepo.Setup(r => r.FindAll(false)).Returns(new List<Slot>().AsQueryable().BuildMock());
            var result = await _service.GetCandidateSessionDetailAsync(1, 1);
            result.MeetingRoomId.Should().Be("r");
        }

        [Fact]
        public async Task GetCandidateSessionDetailAsync_Unauthorized()
        {
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(new Booking { CandidateId = 10 });
            await Assert.ThrowsAsync<BadRequestException>(() => _service.GetCandidateSessionDetailAsync(1, 1));
        }

        [Fact]
        public async Task GetMentorSessionDetailAsync_Success()
        {
            var booking = new Booking { Id = 1, MentorId = 1, AgoraChannelName = "r", Candidate = new Account { FullName = "C" }, BookDate = new DateOnly(2026, 1, 1), StartTime = DateTimeOffset.UtcNow };
            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor());
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(booking);
            _mockSlotRepo.Setup(r => r.FindAll(false)).Returns(new List<Slot>().AsQueryable().BuildMock());
            var result = await _service.GetMentorSessionDetailAsync(1, 1);
            result.MeetingRoomId.Should().Be("r");
        }

        [Fact]
        public async Task GetMentorSessionDetailAsync_MentorNotFound()
        {
            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Mentor?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetMentorSessionDetailAsync(1, 1));
        }

        [Fact]
        public async Task GetMentorSessionDetailAsync_SessionNotFound()
        {
            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor());
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync((Booking?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetMentorSessionDetailAsync(1, 1));
        }

        [Fact]
        public async Task GetMentorSessionDetailAsync_Unauthorized()
        {
            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor());
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(new Booking { MentorId = 10 });
            await Assert.ThrowsAsync<BadRequestException>(() => _service.GetMentorSessionDetailAsync(1, 1));
        }

        [Fact]
        public async Task GetMentorSessionDetailAsync_Success_WithMatchingSlot()
        {
            // Thursday 10:00 SE Asia (UTC+7) = 03:00 UTC
            var startTime = new DateTimeOffset(2026, 1, 1, 3, 0, 0, TimeSpan.Zero);
            var bookDate = new DateOnly(2026, 1, 1);
            var booking = new Booking
            {
                Id = 1,
                MentorId = 1,
                CandidateId = 5,
                BookDate = bookDate,
                StartTime = startTime,
                Candidate = new Account { FullName = "C" }
            };
            var slot = new Slot { DayOfWeek = (int)bookDate.DayOfWeek, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 30) };

            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor());
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(booking);
            _mockSlotRepo.Setup(r => r.FindAll(false)).Returns(new List<Slot> { slot }.AsQueryable().BuildMock());

            var result = await _service.GetMentorSessionDetailAsync(1, 1);

            // 11:30 SE Asia = 04:30 UTC
            result.EndTime.Should().Be(new DateTimeOffset(2026, 1, 1, 4, 30, 0, TimeSpan.Zero));
        }

        [Fact]
        public async Task GetMentorSessionDetailAsync_Success_WithMultipleFiles()
        {
            var booking = new Booking
            {
                Id = 1,
                MentorId = 1,
                CandidateId = 5,
                BookDate = new DateOnly(2026, 1, 1),
                StartTime = DateTimeOffset.UtcNow,
                Candidate = new Account { FullName = "C" },
                AudioRecordKey = "{\"files\": [{\"fileName\": \"f1.mp4\"}, {\"fileName\": \"f2.mp4\"}]}"
            };

            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor());
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(booking);
            _mockSlotRepo.Setup(r => r.FindAll(false)).Returns(new List<Slot>().AsQueryable().BuildMock());
            _mockConfiguration.Setup(c => c["AwsS3Storage:BucketName"]).Returns("bucket");

            var result = await _service.GetMentorSessionDetailAsync(1, 1);

            result.RecordingUrls.Should().HaveCount(2);
            result.RecordingUrls.Should().Contain(url => url.Contains("f1.mp4"));
            result.RecordingUrls.Should().Contain(url => url.Contains("f2.mp4"));
        }

        [Fact]
        public async Task GetMentorSessionDetailAsync_Success_WithJsonObjectMetadata()
        {
            var booking = new Booking
            {
                Id = 1,
                MentorId = 1,
                CandidateId = 5,
                BookDate = new DateOnly(2026, 1, 1),
                StartTime = DateTimeOffset.UtcNow,
                Candidate = new Account { FullName = "C" },
                AudioRecordKey = "{\"files\": [{\"fileName\": \"f1.mp4\"}], \"sid\": \"sid123\"}" // Object instead of Array
            };

            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor());
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(booking);
            _mockSlotRepo.Setup(r => r.FindAll(false)).Returns(new List<Slot>().AsQueryable().BuildMock());
            _mockConfiguration.Setup(c => c["AwsS3Storage:BucketName"]).Returns("bucket");
            _mockConfiguration.Setup(c => c["AwsS3Storage:RegionName"]).Returns("region");

            var result = await _service.GetMentorSessionDetailAsync(1, 1);

            result.RecordingUrls.Should().Contain(url => url.Contains("f1.mp4"));
        }

        [Fact]
        public async Task GetMentorSessionDetailAsync_Success_WithSidFallback()
        {
            var booking = new Booking
            {
                Id = 123,
                MentorId = 1,
                CandidateId = 5,
                BookDate = new DateOnly(2026, 1, 1),
                StartTime = DateTimeOffset.UtcNow,
                Candidate = new Account { FullName = "C" },
                AudioRecordKey = "{\"sid\": \"sid789\"}" // No files array, but has sid
            };

            _mockMentorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Mentor());
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(booking);
            _mockSlotRepo.Setup(r => r.FindAll(false)).Returns(new List<Slot>().AsQueryable().BuildMock());
            _mockConfiguration.Setup(c => c["AwsS3Storage:BucketName"]).Returns("bucket");
            _mockConfiguration.Setup(c => c["AwsS3Storage:RegionName"]).Returns("region");

            var result = await _service.GetMentorSessionDetailAsync(1, 1);

            // Standard naming: recordings/booking{ID}/{ID}/{ID}_{SID}.mp4
            result.RecordingUrls.Should().Contain(url => url.Contains("recordings/booking123/123/123_sid789.mp4"));
        }

        #endregion

        #region CancelBookingAsync

        [Fact]
        public async Task CancelBookingAsync_Success()
        {
            var booking = new Booking { Id = 1, CandidateId = 1, Status = BookingStatus.Confirmed, StartTime = DateTimeOffset.UtcNow.AddHours(24) };
            var account = new Account { Id = 1, Balance = 0 };
            var trans = new Transaction { BookingId = 1, Status = TransactionStatus.Escrow, Amount = 100 };
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(booking);
            _mockTransactionRepo.Setup(r => r.GetBookingTransactionAsync(1)).ReturnsAsync(trans);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);
            await _service.CancelBookingAsync(1, 1);
            booking.Status.Should().Be(BookingStatus.Cancelled);
            account.Balance.Should().Be(100);
        }

        [Fact]
        public async Task CancelBookingAsync_NotFound()
        {
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync((Booking?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _service.CancelBookingAsync(1, 1));
        }
        [Fact]
        public async Task CancelBookingAsync_Unauthorized()
        {
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(new Booking { CandidateId = 10 });
            await Assert.ThrowsAsync<BadRequestException>(() => _service.CancelBookingAsync(1, 1));
        }
        [Fact]
        public async Task CancelBookingAsync_WrongStatus()
        {
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(new Booking { CandidateId = 1, Status = BookingStatus.Completed });
            await Assert.ThrowsAsync<BadRequestException>(() => _service.CancelBookingAsync(1, 1));
        }
        [Fact]
        public async Task CancelBookingAsync_TooLate()
        {
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(new Booking { CandidateId = 1, Status = BookingStatus.Confirmed, StartTime = DateTimeOffset.UtcNow.AddHours(2) });
            await Assert.ThrowsAsync<BadRequestException>(() => _service.CancelBookingAsync(1, 1));
        }

        #endregion

        #region RateMentorAsync

        [Fact]
        public async Task RateMentorAsync_Success()
        {
            var booking = new Booking { Id = 1, CandidateId = 1, MentorId = 2, Status = BookingStatus.Completed };
            var mentor = new Mentor { AccountId = 2 };
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(booking);
            _mockMentorRepo.Setup(r => r.GetMentorByIdAsync(2)).ReturnsAsync(mentor);
            _mockBookingRepo.Setup(r => r.GetMentorCompletedBookingsAsync(2)).ReturnsAsync(new List<Booking> { booking });
            await _service.RateMentorAsync(1, 1, new RateMentorRequest { RatingScore = 5 });
            booking.RatingScore.Should().Be(5);
            mentor.AvgRatings.Should().Be(5);
        }

        [Fact]
        public async Task RateMentorAsync_AlreadyRated()
        {
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(new Booking { CandidateId = 1, RatingCreatedAt = DateTime.UtcNow });
            await Assert.ThrowsAsync<BadRequestException>(() => _service.RateMentorAsync(1, 1, new RateMentorRequest()));
        }

        #endregion

        #region Internal / Helper Coverage
        [Fact]
        public async Task AutoCompleteExpiredBookingsAsync_ShouldHandleException()
        {
            // This tests the catch block in the private method. 
            // We trigger it via GetMentorBookingsAsync which calls AutoComplete...
            // Mock SaveChangesAsync to throw - this is inside the try-catch of AutoComplete...

            var bookings = new List<Booking>
            {
                new Booking
                {
                    Id = 2, MentorId = 1, StartTime = DateTimeOffset.UtcNow.AddHours(-2),
                    Status = BookingStatus.Confirmed, BookDate = new DateOnly(2026, 1, 1),
                    Candidate = new Account { FullName = "C2", AvatarUrl = "url2" }
                }
            }.AsQueryable().BuildMock();

            _mockBookingRepo.Setup(r => r.GetAllBookings()).Returns(bookings);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Throws(new Exception("DB Error"));
            _mockSlotRepo.Setup(r => r.FindAll(false)).Returns(new List<Slot>().AsQueryable().BuildMock());

            // Should not throw because of try-catch in AutoCompleteExpiredBookingsAsync
            var act = () => _service.GetMentorBookingsAsync(1);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ParseRecordingInfo_ShouldParseValidJson()
        {
            // Use reflection or a public method that calls it if possible, 
            // but here we can just test the public method GetCandidateSessionDetailAsync which uses it.
            var booking = new Booking
            {
                Id = 1,
                CandidateId = 1,
                Status = BookingStatus.Completed,
                MentorId = 2,
                Mentor = new Mentor { Account = new Account { FullName = "M", AvatarUrl = "url" } },
                AudioRecordKey = "{\"files\": [{\"fileName\": \"f1.mp4\"}]}",
                StartTime = DateTimeOffset.UtcNow,
                BookDate = new DateOnly(2026, 1, 1)
            };
            _mockBookingRepo.Setup(r => r.GetBookingByIdAsync(1)).ReturnsAsync(booking);
            _mockSlotRepo.Setup(r => r.FindAll(false)).Returns(new List<Slot>().AsQueryable().BuildMock());
            _mockConfiguration.Setup(c => c["AwsS3Storage:BucketName"]).Returns("bucket");
            _mockConfiguration.Setup(c => c["AwsS3Storage:RegionName"]).Returns("region");

            var result = await _service.GetCandidateSessionDetailAsync(1, 1);

            result.RecordingUrls.Should().Contain("https://bucket.s3.region.amazonaws.com/f1.mp4");
        }
        #endregion
    }
}
