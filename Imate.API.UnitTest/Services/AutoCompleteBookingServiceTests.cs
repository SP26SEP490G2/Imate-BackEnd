using Moq;
using FluentAssertions;
using Imate.API.Business.Services.Mentors;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Notification;
using Microsoft.EntityFrameworkCore;
using MockQueryable;
using MockQueryable.Moq;

namespace Imate.API.UnitTest.Services
{
    public class AutoCompleteBookingServiceTests
    {
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<AutoCompleteBookingService>> _mockLogger;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ISystemConfigService> _mockSystemConfigService;
        private readonly Mock<ISystemNotificationService> _mockSystemNotificationService;

        private readonly AutoCompleteBookingService _service;

        public AutoCompleteBookingServiceTests()
        {
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<AutoCompleteBookingService>>();
            _mockMediator = new Mock<IMediator>();
            _mockSystemConfigService = new Mock<ISystemConfigService>();
            _mockSystemNotificationService = new Mock<ISystemNotificationService>();

            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
            _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
            
            _mockServiceProvider.Setup(x => x.GetService(typeof(IUnitOfWork))).Returns(_mockUnitOfWork.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(ISystemConfigService))).Returns(_mockSystemConfigService.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(ISystemNotificationService))).Returns(_mockSystemNotificationService.Object);

            _service = new AutoCompleteBookingService(_mockScopeFactory.Object, _mockLogger.Object, _mockMediator.Object);
        }

        [Fact]
        public async Task ProcessExpiredBookingsAsync_ShouldCompleteBooking_WhenStartTimePlusOneHourPassed()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var expiredStartTime = now.AddHours(-1).AddMinutes(-5); // Already passed 1h limit
            
            var expiredBooking = new Booking 
            { 
                Id = 1, 
                Status = BookingStatus.Confirmed, 
                StartTime = expiredStartTime 
            };

            var mockBookingRepo = new Mock<Imate.API.DataAccess.Interfaces.Mentors.IBookingRepository>();
            mockBookingRepo.Setup(r => r.GetExpiredConfirmedBookingsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Booking> { expiredBooking });
            
            _mockUnitOfWork.Setup(u => u.Bookings).Returns(mockBookingRepo.Object);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Mock other dependencies to return empty lists for this specific test
            mockBookingRepo.Setup(r => r.GetBookingsPendingEscrowReleaseAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Booking>());

            // Act: Using reflection to call the private protected method for testing
            var method = typeof(AutoCompleteBookingService).GetMethod("ProcessExpiredBookingsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method.Invoke(_service, null);

            // Assert
            expiredBooking.Status.Should().Be(BookingStatus.Completed);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessExpiredBookingsAsync_ShouldReleaseEscrow_WhenDeadlinePassedAndNoReports()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var mentorId = 10;
            var mentorAccount = new Account { Id = mentorId, Balance = 1000, FullName = "Mentor A" };
            
            var booking = new Booking 
            { 
                Id = 50, 
                MentorId = mentorId, 
                CandidateId = 20,
                Status = BookingStatus.Completed 
            };

            var escrowTransaction = new Transaction 
            { 
                Id = 100,
                BookingId = 50,
                Amount = 5000, 
                Status = TransactionStatus.Escrow,
                EscrowDeadline = now.AddMinutes(-10) // Window (24h) has expired
            };
            booking.Transactions = new List<Transaction> { escrowTransaction };

            var mockBookingRepo = new Mock<Imate.API.DataAccess.Interfaces.Mentors.IBookingRepository>();
            var mockTransRepo = new Mock<Imate.API.DataAccess.Interfaces.Payment.ITransactionRepository>();
            var mockAccountRepo = new Mock<Imate.API.DataAccess.Interfaces.UserManagement.IAccountRepository>();
            var mockAppRepo = new Mock<Imate.API.DataAccess.Interfaces.Applications.IApplicationRepository>();

            mockBookingRepo.Setup(r => r.GetBookingsPendingEscrowReleaseAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Booking> { booking });
            
            mockTransRepo.Setup(r => r.GetBookingTransactionAsync(50)).ReturnsAsync(escrowTransaction);
            mockAccountRepo.Setup(r => r.GetByIdAsync(mentorId)).ReturnsAsync(mentorAccount);
            
            // Simulating no pending reports
            var applications = new List<Application>().AsQueryable().BuildMock();
            mockAppRepo.Setup(r => r.GetAllApplications()).Returns(applications);

            _mockUnitOfWork.Setup(u => u.Bookings).Returns(mockBookingRepo.Object);
            _mockUnitOfWork.Setup(u => u.Transactions).Returns(mockTransRepo.Object);
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.Applications).Returns(mockAppRepo.Object);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
            
            _mockSystemConfigService.Setup(s => s.GetCommissionRateAsync()).ReturnsAsync(10); // 10% commission

            // Act
            var method = typeof(AutoCompleteBookingService).GetMethod("ProcessExpiredBookingsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method.Invoke(_service, null);

            // Assert
            // 5000 - 10% = 4500. Balance: 1000 + 4500 = 5500
            mentorAccount.Balance.Should().Be(5500);
            escrowTransaction.Status.Should().Be(TransactionStatus.Completed);
            
            // Verify payout transaction created
            mockTransRepo.Verify(r => r.AddAsync(It.Is<Transaction>(t => 
                t.TransactionType == TransactionType.BookingPayout && 
                t.Amount == 4500 && 
                t.TargetAccountId == mentorId)), Times.Once);
            
            _mockMediator.Verify(m => m.Publish(It.IsAny<INotification>(), default), Times.Once); // BalanceUpdatedEvent
        }

        [Fact]
        public async Task ProcessExpiredBookingsAsync_ShouldNOTReleaseEscrow_WhenPendingReportExists()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var booking = new Booking { Id = 50, Status = BookingStatus.Completed };
            var escrowTransaction = new Transaction { Id = 100, Status = TransactionStatus.Escrow, EscrowDeadline = now.AddMinutes(-10) };
            booking.Transactions = new List<Transaction> { escrowTransaction };

            var mockBookingRepo = new Mock<Imate.API.DataAccess.Interfaces.Mentors.IBookingRepository>();
            var mockTransRepo = new Mock<Imate.API.DataAccess.Interfaces.Payment.ITransactionRepository>();
            var mockAppRepo = new Mock<Imate.API.DataAccess.Interfaces.Applications.IApplicationRepository>();

            mockBookingRepo.Setup(r => r.GetBookingsPendingEscrowReleaseAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Booking> { booking });
            mockTransRepo.Setup(r => r.GetBookingTransactionAsync(50)).ReturnsAsync(escrowTransaction);

            // Simulating a pending report for this booking
            var applications = new List<Application> 
            { 
                new Application { BookingId = 50, ApplicationType = ApplicationType.ReportMentor, Status = ApplicationStatus.Pending } 
            }.AsQueryable().BuildMock();
            mockAppRepo.Setup(r => r.GetAllApplications()).Returns(applications);

            _mockUnitOfWork.Setup(u => u.Bookings).Returns(mockBookingRepo.Object);
            _mockUnitOfWork.Setup(u => u.Transactions).Returns(mockTransRepo.Object);
            _mockUnitOfWork.Setup(u => u.Applications).Returns(mockAppRepo.Object);

            // Act
            var method = typeof(AutoCompleteBookingService).GetMethod("ProcessExpiredBookingsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method.Invoke(_service, null);

            // Assert
            escrowTransaction.Status.Should().Be(TransactionStatus.Escrow); // Status should NOT change
            _mockUnitOfWork.Verify(u => u.Accounts.GetByIdAsync(It.IsAny<int>()), Times.Never); // No payment to mentor
        }
    }
}
