using Moq;
using FluentAssertions;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Services.Payment;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.Payment;
using Imate.API.Presentation.ResponseModels.Payment;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;
using Xunit;
using MockQueryable;
using MockQueryable.Moq;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces.Notification;

namespace Imate.API.UnitTest.Services
{
    public class TransactionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITransactionRepository> _mockTransactionRepo;
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<PayOSClient> _mockPayosClient;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<TransactionService>> _mockLogger;
        private readonly Mock<ISystemConfigService> _mockSystemConfigService;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IMentorRepository> _mockMentorRepo;
        private readonly Mock<IBookingRepository> _mockBookingRepo;
        private readonly Mock<ISystemNotificationService> _mockSystemNotificationService;

        public TransactionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTransactionRepo = new Mock<ITransactionRepository>();
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockPayosClient = new Mock<PayOSClient>(new PayOSOptions
            {
                ClientId = "test-client-id",
                ApiKey = "test-api-key",
                ChecksumKey = "test-checksum-key"
            });
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<TransactionService>>();
            _mockSystemConfigService = new Mock<ISystemConfigService>();
            _mockMediator = new Mock<IMediator>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockMentorRepo = new Mock<IMentorRepository>();
            _mockBookingRepo = new Mock<IBookingRepository>();
            _mockSystemNotificationService = new Mock<ISystemNotificationService>();

            _mockUnitOfWork.Setup(u => u.Transactions).Returns(_mockTransactionRepo.Object);
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(_mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.Mentors).Returns(_mockMentorRepo.Object);
            _mockUnitOfWork.Setup(u => u.Bookings).Returns(_mockBookingRepo.Object);

            _mockConfiguration.Setup(c => c["FrontendSettings:BaseUrl"]).Returns("https://imate.vn");
            _mockConfiguration.Setup(c => c["PayOS:ClientId"]).Returns("client-id");
            _mockConfiguration.Setup(c => c["PayOS:ApiKey"]).Returns("api-key");
            _mockConfiguration.Setup(c => c["PayOS:ChecksumKey"]).Returns("checksum-key");

            _mockSystemConfigService
                .Setup(s => s.GetMinDepositAmountAsync())
                .ReturnsAsync(1000);
        }

        private TransactionService CreateService()
        {
            return new TransactionService(
                _mockUnitOfWork.Object,
                _mockPayosClient.Object,
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockSystemConfigService.Object,
                _mockMediator.Object,
                _mockAuditLogService.Object,
                _mockSystemNotificationService.Object
            );
        }

        #region CreateDepositAsync



        // UTC_02: Nạp tiền với Amount <= 0 → throw ArgumentException, không tạo transaction
        [Theory]
        [InlineData(0)]
        [InlineData(-1000)]
        public async Task CreateDepositAsync_ShouldThrowArgumentException_WhenAmountIsZeroOrNegative(int amount)
        {
            var request = new DepositRequest { Amount = amount };

            var service = CreateService();
            var act = () => service.CreateDepositAsync(1, request);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Số tiền nạp phải lớn hơn 0.");

            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        // UTC_03: Nạp tiền với Amount < MinDeposit (ví dụ 100 < 1000) → throw ArgumentException với thông báo số tiền tối thiểu
        [Fact]
        public async Task CreateDepositAsync_ShouldThrowArgumentException_WhenAmountBelowMinimum()
        {
            var request = new DepositRequest { Amount = 100 };

            _mockSystemConfigService
                .Setup(s => s.GetMinDepositAmountAsync())
                .ReturnsAsync(1000);

            var service = CreateService();
            var act = () => service.CreateDepositAsync(1, request);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*tối thiểu*1,000*");

            _mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        }

        // UTC_04: FrontendBaseUrl không được cấu hình → throw InvalidOperationException, không gọi PayOS
        [Fact]
        public async Task CreateDepositAsync_ShouldThrowInvalidOperationException_WhenFrontendUrlMissing()
        {
            _mockConfiguration.Setup(c => c["FrontendSettings:BaseUrl"]).Returns((string?)null);

            var act = () => new TransactionService(
                _mockUnitOfWork.Object,
                _mockPayosClient.Object,
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockSystemConfigService.Object,
                _mockMediator.Object,
                _mockAuditLogService.Object,
                _mockSystemNotificationService.Object
            );

            act.Should().Throw<ArgumentNullException>()
                .Where(e => e.Message.Contains("Url is not set"));
        }



        // UTC_06: Lỗi CSDL khi tạo transaction PENDING → rollback, throw Exception với message lỗi DB
        [Fact]
        public async Task CreateDepositAsync_ShouldRollbackAndThrow_WhenDatabaseFailsOnPendingCreation()
        {
            var request = new DepositRequest { Amount = 10000 };

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _mockTransactionRepo
                .Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = 100; return t; });

            _mockUnitOfWork
                .SetupSequence(u => u.SaveChangesAsync())
                .ThrowsAsync(new Exception("DB connection lost"));

            var service = CreateService();
            var act = () => service.CreateDepositAsync(1, request);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("*CSDL*");

            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        #endregion

        #region GetBalanceSummaryAsync (UC-16: View Income)

        [Fact]
        public async Task GetBalanceSummaryAsync_ShouldReturnSummary_ForRegularUser()
        {
            var accountId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId))
                .ReturnsAsync(new Account { Id = accountId, Balance = 50000, UpdatedAt = DateTime.UtcNow });
            _mockTransactionRepo.Setup(r => r.GetTotalAmountAsync(accountId, TransactionType.Deposit, TransactionStatus.Completed, true))
                .ReturnsAsync(100000);
            _mockTransactionRepo.Setup(r => r.GetTotalAmountAsync(accountId, TransactionType.Withdrawal, TransactionStatus.Completed, false))
                .ReturnsAsync(50000);
            _mockMentorRepo.Setup(r => r.GetMentorByIdAsync(accountId))
                .ReturnsAsync((Mentor?)null);

            var service = CreateService();
            var result = await service.GetBalanceSummaryAsync(accountId);

            result.CurrentBalance.Should().Be(50000);
            result.TotalDeposit.Should().Be(100000);
            result.TotalWithdrawal.Should().Be(50000);
        }

        [Fact]
        public async Task GetBalanceSummaryAsync_ShouldIncludeMentorGuaranteeInfo()
        {
            var accountId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId))
                .ReturnsAsync(new Account { Id = accountId, Balance = 100000 });
            _mockTransactionRepo.Setup(r => r.GetTotalAmountAsync(accountId, TransactionType.Deposit, TransactionStatus.Completed, true))
                .ReturnsAsync(200000);
            _mockTransactionRepo.Setup(r => r.GetTotalAmountAsync(accountId, TransactionType.Withdrawal, TransactionStatus.Completed, false))
                .ReturnsAsync(100000);

            _mockMentorRepo.Setup(r => r.GetMentorByIdAsync(accountId))
                .ReturnsAsync(new Mentor { AccountId = accountId, PricePerSession = 100 });
            _mockSystemConfigService.Setup(s => s.GetGuaranteeDepositRateAsync()).ReturnsAsync(30m);
            _mockSystemConfigService.Setup(s => s.GetReportDeadlineHoursAsync()).ReturnsAsync(24);

            var emptyBookings = new List<Booking>().AsQueryable().BuildMock();
            _mockBookingRepo.Setup(r => r.GetAllBookings()).Returns(emptyBookings);

            var service = CreateService();
            var result = await service.GetBalanceSummaryAsync(accountId);

            result.PricePerSession.Should().Be(100);
            result.GuaranteeDepositRate.Should().Be(30m);
            result.CurrentEscrowBookings.Should().Be(0);
        }

        [Fact]
        public async Task GetBalanceSummaryAsync_ShouldCalculateEscrowBookings_WhenMentorHasConfirmedBookings()
        {
            var accountId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId))
                .ReturnsAsync(new Account { Id = accountId, Balance = 100000 });
            _mockTransactionRepo.Setup(r => r.GetTotalAmountAsync(accountId, TransactionType.Deposit, TransactionStatus.Completed, true))
                .ReturnsAsync(200000);
            _mockTransactionRepo.Setup(r => r.GetTotalAmountAsync(accountId, TransactionType.Withdrawal, TransactionStatus.Completed, false))
                .ReturnsAsync(100000);

            _mockMentorRepo.Setup(r => r.GetMentorByIdAsync(accountId))
                .ReturnsAsync(new Mentor { AccountId = accountId, PricePerSession = 100 });
            _mockSystemConfigService.Setup(s => s.GetGuaranteeDepositRateAsync()).ReturnsAsync(30m);
            _mockSystemConfigService.Setup(s => s.GetReportDeadlineHoursAsync()).ReturnsAsync(24);

            var bookings = new List<Booking>
            {
                new Booking { MentorId = accountId, Status = BookingStatus.Confirmed, PriceAtBooking = 100, StartTime = DateTimeOffset.UtcNow.AddDays(1) },
                new Booking { MentorId = accountId, Status = BookingStatus.Confirmed, PriceAtBooking = 100, StartTime = DateTimeOffset.UtcNow.AddDays(2) }
            }.AsQueryable().BuildMock();
            _mockBookingRepo.Setup(r => r.GetAllBookings()).Returns(bookings);

            var service = CreateService();
            var result = await service.GetBalanceSummaryAsync(accountId);

            result.CurrentEscrowBookings.Should().Be(2);
        }

        [Fact]
        public async Task GetBalanceSummaryAsync_ShouldThrowKeyNotFound_WhenAccountDoesNotExist()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Account?)null);

            var service = CreateService();
            var act = () => service.GetBalanceSummaryAsync(999);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        #endregion

        #region GetTransactionsAsync (UC-5, UC-15: View Transactions History)

        [Fact]
        public async Task GetTransactionsAsync_ShouldReturnMappedTransactions()
        {
            var accountId = 1;
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, Amount = 10000, TransactionType = TransactionType.Deposit, Status = TransactionStatus.Completed, CreatedAt = DateTime.UtcNow, ExternalTransactionCode = "EXT001" },
                new Transaction { Id = 2, Amount = 5000, TransactionType = TransactionType.Withdrawal, Status = TransactionStatus.Pending, CreatedAt = DateTime.UtcNow,
                    WithdrawalDetail = new WithdrawalDetail { BankCode = "VCB", BankAccountHolder = "NGUYEN VAN A", BankAccountNumber = "1234567890" } }
            };
            var pagedList = new PagedList<Transaction>(transactions, 2, 1, 10);

            _mockTransactionRepo.Setup(r => r.GetTransactionsForAccountAsync(accountId, It.IsAny<TransactionQueryParameters>()))
                .ReturnsAsync(pagedList);

            var service = CreateService();
            var result = await service.GetTransactionsAsync(accountId, new TransactionQueryParameters());

            result.Items.Should().HaveCount(2);
            result.Items[0].TransactionId.Should().Be(1);
            result.Items[0].Amount.Should().Be(10000);
            result.Items[1].WithdrawalDetail.Should().NotBeNull();
            result.Items[1].WithdrawalDetail.BankAccountNumber.Should().Be("XXXXXX7890");
        }

        [Fact]
        public async Task GetTransactionsAsync_ShouldDisplayEscrowAsCompleted()
        {
            var accountId = 1;
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, Amount = 10000, TransactionType = TransactionType.BookingFee, Status = TransactionStatus.Escrow, CreatedAt = DateTime.UtcNow }
            };
            var pagedList = new PagedList<Transaction>(transactions, 1, 1, 10);

            _mockTransactionRepo.Setup(r => r.GetTransactionsForAccountAsync(accountId, It.IsAny<TransactionQueryParameters>()))
                .ReturnsAsync(pagedList);

            var service = CreateService();
            var result = await service.GetTransactionsAsync(accountId, new TransactionQueryParameters());

            result.Items.Should().HaveCount(1);
            result.Items[0].Status.Should().Be("Completed");
        }

        [Fact]
        public async Task GetTransactionsAsync_ShouldMapTransactionType()
        {
            var accountId = 1;
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, Amount = 10000, TransactionType = TransactionType.Deposit, Status = TransactionStatus.Completed, CreatedAt = DateTime.UtcNow }
            };
            var pagedList = new PagedList<Transaction>(transactions, 1, 1, 10);

            _mockTransactionRepo.Setup(r => r.GetTransactionsForAccountAsync(accountId, It.IsAny<TransactionQueryParameters>()))
                .ReturnsAsync(pagedList);

            var service = CreateService();
            var result = await service.GetTransactionsAsync(accountId, new TransactionQueryParameters());

            result.Items[0].TransactionType.Should().Be("Deposit");
        }

        #endregion

        #region GetRecentTransactionsAsync

        [Fact]
        public async Task GetRecentTransactionsAsync_ShouldReturnRecentTransactions()
        {
            var accountId = 1;
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, Amount = 10000, TransactionType = TransactionType.Deposit, Status = TransactionStatus.Completed, CreatedAt = DateTime.UtcNow },
                new Transaction { Id = 2, Amount = 5000, TransactionType = TransactionType.Withdrawal, Status = TransactionStatus.Completed, CreatedAt = DateTime.UtcNow }
            };

            _mockTransactionRepo.Setup(r => r.GetRecentTransactionsAsync(accountId, 5))
                .ReturnsAsync(transactions);

            var service = CreateService();
            var result = await service.GetRecentTransactionsAsync(accountId);

            result.Should().HaveCount(2);
            result[0].TransactionId.Should().Be(1);
        }

        [Fact]
        public async Task GetRecentTransactionsAsync_ShouldDisplayEscrowAsCompleted()
        {
            var accountId = 1;
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, Amount = 10000, TransactionType = TransactionType.BookingFee, Status = TransactionStatus.Escrow, CreatedAt = DateTime.UtcNow }
            };

            _mockTransactionRepo.Setup(r => r.GetRecentTransactionsAsync(accountId, 5))
                .ReturnsAsync(transactions);

            var service = CreateService();
            var result = await service.GetRecentTransactionsAsync(accountId);

            result[0].Status.Should().Be("Completed");
        }

        #endregion

        #region CreateWithdrawalAsync (UC-7, UC-19: Send Withdraw Request)

        [Fact]
        public async Task CreateWithdrawalAsync_ShouldSucceed_WhenCandidateWithValidRequest()
        {
            var accountId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId))
                .ReturnsAsync(new Account { Id = accountId, Balance = 50000 });
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockTransactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = 100; return t; });

            var request = new WithdrawRequest
            {
                Amount = 20000,
                BankCode = "VCB",
                BankAccountHolder = "NGUYEN VAN A",
                BankAccountNumber = "1234567890"
            };

            var service = CreateService();
            var result = await service.CreateWithdrawalAsync(accountId, "Candidate", request);

            result.Should().NotBeNull();
            result.Amount.Should().Be(20000);
            result.TransactionType.Should().Be("Withdrawal");
            result.Status.Should().Be("Pending");
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateWithdrawalAsync_ShouldSucceed_WhenMentorWithValidRequest()
        {
            var accountId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId))
                .ReturnsAsync(new Account { Id = accountId, Balance = 50000 });
            _mockMentorRepo.Setup(r => r.GetMentorByIdAsync(accountId))
                .ReturnsAsync(new Mentor
                {
                    AccountId = accountId,
                    PricePerSession = 100,
                    BankCode = "TCB",
                    BankAccountHolderName = "TRAN VAN B",
                    BankAccountNumber = "9876543210"
                });
            _mockSystemConfigService.Setup(s => s.GetGuaranteeDepositRateAsync()).ReturnsAsync(30m);
            _mockSystemConfigService.Setup(s => s.GetReportDeadlineHoursAsync()).ReturnsAsync(24);

            var emptyBookings = new List<Booking>().AsQueryable().BuildMock();
            _mockBookingRepo.Setup(r => r.GetAllBookings()).Returns(emptyBookings);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockTransactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = 100; return t; });

            var request = new WithdrawRequest { Amount = 20000 };

            var service = CreateService();
            var result = await service.CreateWithdrawalAsync(accountId, "Mentor", request);

            result.Should().NotBeNull();
            result.Amount.Should().Be(20000);
            result.WithdrawalDetail.BankCode.Should().Be("TCB");
        }

        [Fact]
        public async Task CreateWithdrawalAsync_ShouldDeductBalance()
        {
            var accountId = 1;
            var account = new Account { Id = accountId, Balance = 50000 };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockTransactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = 100; return t; });

            var request = new WithdrawRequest { Amount = 20000, BankCode = "VCB", BankAccountHolder = "A", BankAccountNumber = "123" };

            var service = CreateService();
            await service.CreateWithdrawalAsync(accountId, "Candidate", request);

            account.Balance.Should().Be(30000);
        }

        [Fact]
        public async Task CreateWithdrawalAsync_ShouldThrowKeyNotFound_WhenAccountDoesNotExist()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Account?)null);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            var request = new WithdrawRequest { Amount = 1000, BankCode = "VCB", BankAccountHolder = "A", BankAccountNumber = "123" };

            var service = CreateService();
            var act = () => service.CreateWithdrawalAsync(1, "Candidate", request);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task CreateWithdrawalAsync_ShouldThrowArgumentException_WhenInsufficientBalance()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Account { Id = 1, Balance = 1000 });
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            var request = new WithdrawRequest { Amount = 50000, BankCode = "VCB", BankAccountHolder = "A", BankAccountNumber = "123" };

            var service = CreateService();
            var act = () => service.CreateWithdrawalAsync(1, "Candidate", request);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Số dư không đủ*");
        }

        [Fact]
        public async Task CreateWithdrawalAsync_ShouldThrowArgumentException_WhenCandidateMissingBankInfo()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Account { Id = 1, Balance = 50000 });
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            var request = new WithdrawRequest { Amount = 10000 };

            var service = CreateService();
            var act = () => service.CreateWithdrawalAsync(1, "Candidate", request);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Thiếu thông tin ngân hàng*");
        }

        [Fact]
        public async Task CreateWithdrawalAsync_ShouldThrowKeyNotFound_WhenMentorProfileNotFound()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Account { Id = 1, Balance = 50000 });
            _mockMentorRepo.Setup(r => r.GetMentorByIdAsync(1)).ReturnsAsync((Mentor?)null);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            var request = new WithdrawRequest { Amount = 10000 };

            var service = CreateService();
            var act = () => service.CreateWithdrawalAsync(1, "Mentor", request);

            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*hồ sơ Mentor*");
        }

        [Fact]
        public async Task CreateWithdrawalAsync_ShouldThrowArgumentException_WhenMentorMissingBankInfo()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Account { Id = 1, Balance = 50000 });
            _mockMentorRepo.Setup(r => r.GetMentorByIdAsync(1))
                .ReturnsAsync(new Mentor { AccountId = 1, BankCode = null, BankAccountNumber = null, BankAccountHolderName = null });
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            var request = new WithdrawRequest { Amount = 10000 };

            var service = CreateService();
            var act = () => service.CreateWithdrawalAsync(1, "Mentor", request);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*ngân hàng*chưa đầy đủ*");
        }

        [Fact]
        public async Task CreateWithdrawalAsync_ShouldThrowArgumentException_WhenInvalidRole()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Account { Id = 1, Balance = 50000 });
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            var request = new WithdrawRequest { Amount = 10000 };

            var service = CreateService();
            var act = () => service.CreateWithdrawalAsync(1, "Admin", request);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Vai trò*không được phép*");
        }

        [Fact]
        public async Task CreateWithdrawalAsync_ShouldThrowArgumentException_WhenMentorGuaranteeInsufficient()
        {
            var accountId = 1;
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId))
                .ReturnsAsync(new Account { Id = accountId, Balance = 50 });
            _mockMentorRepo.Setup(r => r.GetMentorByIdAsync(accountId))
                .ReturnsAsync(new Mentor
                {
                    AccountId = accountId,
                    PricePerSession = 100,
                    BankCode = "TCB",
                    BankAccountHolderName = "TRAN",
                    BankAccountNumber = "111222333"
                });
            _mockSystemConfigService.Setup(s => s.GetGuaranteeDepositRateAsync()).ReturnsAsync(30m);
            _mockSystemConfigService.Setup(s => s.GetReportDeadlineHoursAsync()).ReturnsAsync(24);

            var bookings = new List<Booking>
            {
                new Booking { MentorId = accountId, Status = BookingStatus.Confirmed, PriceAtBooking = 100, StartTime = DateTimeOffset.UtcNow.AddDays(1) }
            }.AsQueryable().BuildMock();
            _mockBookingRepo.Setup(r => r.GetAllBookings()).Returns(bookings);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            var request = new WithdrawRequest { Amount = 50 };

            var service = CreateService();
            var act = () => service.CreateWithdrawalAsync(accountId, "Mentor", request);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*booking*tiền đảm bảo*");
        }

        #endregion

        #region CancelTransactionAsync

        [Fact]
        public async Task CancelTransactionAsync_ShouldSucceed_WhenTransactionHasExternalCode()
        {
            var transaction = new Transaction
            {
                Id = 1,
                TargetAccountId = 1,
                Status = TransactionStatus.Pending,
                ExternalTransactionCode = "pl_123"
            };
            _mockTransactionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(transaction);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var service = CreateService();
            await service.CancelTransactionAsync(1, 1);

            transaction.Status.Should().Be(TransactionStatus.Cancelled);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelTransactionAsync_ShouldSucceed_WithoutExternalCode()
        {
            var transaction = new Transaction
            {
                Id = 1,
                TargetAccountId = 1,
                Status = TransactionStatus.Pending,
                ExternalTransactionCode = null
            };
            _mockTransactionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(transaction);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var service = CreateService();
            await service.CancelTransactionAsync(1, 1);

            transaction.Status.Should().Be(TransactionStatus.Cancelled);
        }

        [Fact]
        public async Task CancelTransactionAsync_ShouldThrowKeyNotFound_WhenTransactionDoesNotExist()
        {
            _mockTransactionRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Transaction?)null);

            var service = CreateService();
            var act = () => service.CancelTransactionAsync(999, 1);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task CancelTransactionAsync_ShouldThrowUnauthorized_WhenWrongAccount()
        {
            var transaction = new Transaction { Id = 1, TargetAccountId = 99, Status = TransactionStatus.Pending };
            _mockTransactionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(transaction);

            var service = CreateService();
            var act = () => service.CancelTransactionAsync(1, 1);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task CancelTransactionAsync_ShouldThrowInvalidOperation_WhenNotPendingStatus()
        {
            var transaction = new Transaction { Id = 1, TargetAccountId = 1, Status = TransactionStatus.Completed };
            _mockTransactionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(transaction);

            var service = CreateService();
            var act = () => service.CancelTransactionAsync(1, 1);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        #endregion

        #region UC-7: GetSystemStatisticsAsync (View Trend Reports)

        // UTC_TR_01: Trả về thống kê hệ thống đúng khi có dữ liệu
        [Fact]
        public async Task GetSystemStatisticsAsync_ShouldReturnCorrectStatistics()
        {
            // Arrange
            _mockTransactionRepo.Setup(r => r.GetSystemTotalAmountAsync(TransactionType.Deposit, TransactionStatus.Completed))
                .ReturnsAsync(500000);
            _mockTransactionRepo.Setup(r => r.GetSystemTotalAmountAsync(TransactionType.Withdrawal, TransactionStatus.Completed))
                .ReturnsAsync(200000);

            var service = CreateService();

            // Act
            var result = await service.GetSystemStatisticsAsync();

            // Assert
            result.TotalDeposit.Should().Be(500000);
            result.TotalWithdrawal.Should().Be(200000);
            result.NetProfit.Should().Be(300000); // 500000 - 200000
        }

        // UTC_TR_02: Trả về 0 cho tất cả khi không có giao dịch
        [Fact]
        public async Task GetSystemStatisticsAsync_ShouldReturnZeros_WhenNoTransactions()
        {
            // Arrange
            _mockTransactionRepo.Setup(r => r.GetSystemTotalAmountAsync(It.IsAny<TransactionType>(), It.IsAny<TransactionStatus>()))
                .ReturnsAsync(0);

            var service = CreateService();

            // Act
            var result = await service.GetSystemStatisticsAsync();

            // Assert
            result.TotalDeposit.Should().Be(0);
            result.TotalWithdrawal.Should().Be(0);
            result.NetProfit.Should().Be(0);
        }

        // UTC_TR_03: NetProfit âm khi rút nhiều hơn nạp
        [Fact]
        public async Task GetSystemStatisticsAsync_ShouldReturnNegativeNetProfit_WhenWithdrawalExceedsDeposit()
        {
            // Arrange
            _mockTransactionRepo.Setup(r => r.GetSystemTotalAmountAsync(TransactionType.Deposit, TransactionStatus.Completed))
                .ReturnsAsync(100000);
            _mockTransactionRepo.Setup(r => r.GetSystemTotalAmountAsync(TransactionType.Withdrawal, TransactionStatus.Completed))
                .ReturnsAsync(300000);

            var service = CreateService();

            // Act
            var result = await service.GetSystemStatisticsAsync();

            // Assert
            result.NetProfit.Should().Be(-200000);
        }

        #endregion

        #region UC-8: GetRevenueAsync (View Revenue Reports)

        // UTC_RV_01: Trả về doanh thu đúng khi có subscription và penalty
        [Fact]
        public async Task GetRevenueAsync_ShouldReturnCorrectRevenue_WhenDataExists()
        {
            // Arrange
            var currentYear = DateTime.UtcNow.Year;
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, TransactionType = TransactionType.Subscription, Status = TransactionStatus.Completed, Amount = 100000, CreatedAt = new DateTime(currentYear, 3, 1) },
                new Transaction { Id = 2, TransactionType = TransactionType.Penalty, Status = TransactionStatus.Completed, Amount = 50000, CreatedAt = new DateTime(currentYear, 3, 15) },
                new Transaction { Id = 3, TransactionType = TransactionType.Deposit, Status = TransactionStatus.Completed, Amount = 200000, CreatedAt = new DateTime(currentYear, 3, 20) }
            }.AsQueryable().BuildMock();

            _mockTransactionRepo.Setup(r => r.GetAllTransactionsQueryable()).Returns(transactions);
            _mockSystemConfigService.Setup(s => s.GetCancellationRefundRateAsync()).ReturnsAsync(80m);

            var service = CreateService();
            var parameters = new RevenueQueryParameters { Year = currentYear };

            // Act
            var result = await service.GetRevenueAsync(parameters);

            // Assert
            result.Year.Should().Be(currentYear);
            result.Breakdown.PointSubscriptionFee.Should().Be(100000);
            result.Breakdown.PointPenalty.Should().Be(50000);
            result.Breakdown.PointDeposit.Should().Be(200000);
        }

        // UTC_RV_02: Lọc theo tháng cụ thể
        [Fact]
        public async Task GetRevenueAsync_ShouldFilterByMonth_WhenMonthProvided()
        {
            // Arrange
            var currentYear = DateTime.UtcNow.Year;
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, TransactionType = TransactionType.Subscription, Status = TransactionStatus.Completed, Amount = 100000, CreatedAt = new DateTime(currentYear, 3, 1) },
                new Transaction { Id = 2, TransactionType = TransactionType.Subscription, Status = TransactionStatus.Completed, Amount = 50000, CreatedAt = new DateTime(currentYear, 5, 1) }
            }.AsQueryable().BuildMock();

            _mockTransactionRepo.Setup(r => r.GetAllTransactionsQueryable()).Returns(transactions);
            _mockSystemConfigService.Setup(s => s.GetCancellationRefundRateAsync()).ReturnsAsync(80m);

            var service = CreateService();
            var parameters = new RevenueQueryParameters { Year = currentYear, Month = 3 };

            // Act
            var result = await service.GetRevenueAsync(parameters);

            // Assert
            result.Month.Should().Be(3);
            result.Breakdown.PointSubscriptionFee.Should().Be(100000); // Chỉ tháng 3
        }

        // UTC_RV_03: Lọc theo loại giao dịch
        [Fact]
        public async Task GetRevenueAsync_ShouldFilterByTransactionType_WhenTypeProvided()
        {
            // Arrange
            var currentYear = DateTime.UtcNow.Year;
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, TransactionType = TransactionType.Subscription, Status = TransactionStatus.Completed, Amount = 100000, CreatedAt = new DateTime(currentYear, 3, 1) },
                new Transaction { Id = 2, TransactionType = TransactionType.Penalty, Status = TransactionStatus.Completed, Amount = 50000, CreatedAt = new DateTime(currentYear, 3, 1) }
            }.AsQueryable().BuildMock();

            _mockTransactionRepo.Setup(r => r.GetAllTransactionsQueryable()).Returns(transactions);
            _mockSystemConfigService.Setup(s => s.GetCancellationRefundRateAsync()).ReturnsAsync(80m);

            var service = CreateService();
            var parameters = new RevenueQueryParameters { Year = currentYear, TransactionType = "Subscription" };

            // Act
            var result = await service.GetRevenueAsync(parameters);

            // Assert
            result.TransactionType.Should().Be("Subscription");
            result.Breakdown.PointSubscriptionFee.Should().Be(100000);
            result.Breakdown.PointPenalty.Should().Be(0); // Bị filter ra
        }

        // UTC_RV_04: Throw ArgumentException khi year không hợp lệ
        [Theory]
        [InlineData(2019)]
        [InlineData(2099)]
        public async Task GetRevenueAsync_ShouldThrowArgumentException_WhenYearIsInvalid(int invalidYear)
        {
            var service = CreateService();
            var parameters = new RevenueQueryParameters { Year = invalidYear };

            var act = () => service.GetRevenueAsync(parameters);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Năm không hợp lệ*");
        }

        // UTC_RV_05: Throw ArgumentException khi month không hợp lệ
        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public async Task GetRevenueAsync_ShouldThrowArgumentException_WhenMonthIsInvalid(int invalidMonth)
        {
            var service = CreateService();
            var parameters = new RevenueQueryParameters { Year = DateTime.UtcNow.Year, Month = invalidMonth };

            var act = () => service.GetRevenueAsync(parameters);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Tháng không hợp lệ*");
        }

        // UTC_RV_06: Throw ArgumentException khi TransactionType không hợp lệ
        [Fact]
        public async Task GetRevenueAsync_ShouldThrowArgumentException_WhenTransactionTypeIsInvalid()
        {
            var currentYear = DateTime.UtcNow.Year;
            var transactions = new List<Transaction>().AsQueryable().BuildMock();
            _mockTransactionRepo.Setup(r => r.GetAllTransactionsQueryable()).Returns(transactions);

            var service = CreateService();
            var parameters = new RevenueQueryParameters { Year = currentYear, TransactionType = "InvalidType" };

            var act = () => service.GetRevenueAsync(parameters);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*không hợp lệ*");
        }

        // UTC_RV_07: Trả về 0 khi không có giao dịch trong khoảng thời gian
        [Fact]
        public async Task GetRevenueAsync_ShouldReturnZeroRevenue_WhenNoTransactionsInPeriod()
        {
            var currentYear = DateTime.UtcNow.Year;
            var transactions = new List<Transaction>().AsQueryable().BuildMock();
            _mockTransactionRepo.Setup(r => r.GetAllTransactionsQueryable()).Returns(transactions);
            _mockSystemConfigService.Setup(s => s.GetCancellationRefundRateAsync()).ReturnsAsync(80m);

            var service = CreateService();
            var parameters = new RevenueQueryParameters { Year = currentYear };

            var result = await service.GetRevenueAsync(parameters);

            result.TotalRevenue.Should().Be(0);
            result.TotalIncome.Should().Be(0);
            result.Breakdown.PointSubscriptionFee.Should().Be(0);
            result.Breakdown.PointPenalty.Should().Be(0);
        }

        // UTC_RV_08: Chỉ tính Completed transactions, bỏ qua Pending/Failed
        [Fact]
        public async Task GetRevenueAsync_ShouldOnlyCountCompletedTransactions()
        {
            var currentYear = DateTime.UtcNow.Year;
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, TransactionType = TransactionType.Subscription, Status = TransactionStatus.Completed, Amount = 100000, CreatedAt = new DateTime(currentYear, 3, 1) },
                new Transaction { Id = 2, TransactionType = TransactionType.Subscription, Status = TransactionStatus.Pending, Amount = 50000, CreatedAt = new DateTime(currentYear, 3, 1) },
                new Transaction { Id = 3, TransactionType = TransactionType.Subscription, Status = TransactionStatus.Failed, Amount = 30000, CreatedAt = new DateTime(currentYear, 3, 1) }
            }.AsQueryable().BuildMock();

            _mockTransactionRepo.Setup(r => r.GetAllTransactionsQueryable()).Returns(transactions);
            _mockSystemConfigService.Setup(s => s.GetCancellationRefundRateAsync()).ReturnsAsync(80m);

            var service = CreateService();
            var parameters = new RevenueQueryParameters { Year = currentYear };

            var result = await service.GetRevenueAsync(parameters);

            result.Breakdown.PointSubscriptionFee.Should().Be(100000); // Chỉ Completed
        }

        // UTC_RV_09: TotalRevenue = TotalIncome - PointDeposit
        [Fact]
        public async Task GetRevenueAsync_ShouldCalculateTotalRevenueCorrectly()
        {
            var currentYear = DateTime.UtcNow.Year;
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, TransactionType = TransactionType.Subscription, Status = TransactionStatus.Completed, Amount = 100000, CreatedAt = new DateTime(currentYear, 3, 1) },
                new Transaction { Id = 2, TransactionType = TransactionType.InterviewFee, Status = TransactionStatus.Completed, Amount = 30000, CreatedAt = new DateTime(currentYear, 3, 1) },
                new Transaction { Id = 3, TransactionType = TransactionType.Deposit, Status = TransactionStatus.Completed, Amount = 50000, CreatedAt = new DateTime(currentYear, 3, 1) }
            }.AsQueryable().BuildMock();

            _mockTransactionRepo.Setup(r => r.GetAllTransactionsQueryable()).Returns(transactions);
            _mockSystemConfigService.Setup(s => s.GetCancellationRefundRateAsync()).ReturnsAsync(80m);

            var service = CreateService();
            var parameters = new RevenueQueryParameters { Year = currentYear };

            var result = await service.GetRevenueAsync(parameters);

            // TotalIncome = subscription(100k) + interview(30k) = 130k
            // TotalRevenue = TotalIncome - Deposit = 130k - 50k = 80k
            result.TotalIncome.Should().Be(130000);
            result.TotalDeposit.Should().Be(50000);
            result.TotalRevenue.Should().Be(80000);
        }

        #endregion

        #region UC-8: GetRevenueTransactionsAsync (View Revenue Transaction Details)

        // UTC_RVT_01: Trả về danh sách giao dịch doanh thu phân trang
        [Fact]
        public async Task GetRevenueTransactionsAsync_ShouldReturnPagedTransactions()
        {
            var currentYear = DateTime.UtcNow.Year;
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, TransactionType = TransactionType.Subscription, Status = TransactionStatus.Completed, Amount = 100000, CreatedAt = new DateTime(currentYear, 3, 1) },
                new Transaction { Id = 2, TransactionType = TransactionType.Penalty, Status = TransactionStatus.Completed, Amount = 50000, CreatedAt = new DateTime(currentYear, 3, 15) }
            }.AsQueryable().BuildMock();

            _mockTransactionRepo.Setup(r => r.GetAllTransactionsQueryable()).Returns(transactions);
            _mockSystemConfigService.Setup(s => s.GetCancellationRefundRateAsync()).ReturnsAsync(80m);

            var service = CreateService();
            var parameters = new RevenueTransactionQueryParameters { Year = currentYear, PageNumber = 1, PageSize = 10 };

            var result = await service.GetRevenueTransactionsAsync(parameters);

            result.Items.Should().HaveCount(2);
            result.Items[0].TransactionType.Should().Be("Penalty"); // OrderBy CreatedAt desc → Mar 15 trước Mar 1
        }

        // UTC_RVT_02: Throw ArgumentException khi year không hợp lệ
        [Fact]
        public async Task GetRevenueTransactionsAsync_ShouldThrowArgumentException_WhenYearIsInvalid()
        {
            var service = CreateService();
            var parameters = new RevenueTransactionQueryParameters { Year = 2019, PageNumber = 1, PageSize = 10 };

            var act = () => service.GetRevenueTransactionsAsync(parameters);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Năm không hợp lệ*");
        }

        // UTC_RVT_03: Throw ArgumentException khi month không hợp lệ
        [Fact]
        public async Task GetRevenueTransactionsAsync_ShouldThrowArgumentException_WhenMonthIsInvalid()
        {
            var service = CreateService();
            var parameters = new RevenueTransactionQueryParameters { Year = DateTime.UtcNow.Year, Month = 13, PageNumber = 1, PageSize = 10 };

            var act = () => service.GetRevenueTransactionsAsync(parameters);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Tháng không hợp lệ*");
        }

        // UTC_RVT_04: Lọc theo TransactionType
        [Fact]
        public async Task GetRevenueTransactionsAsync_ShouldFilterByTransactionType()
        {
            var currentYear = DateTime.UtcNow.Year;
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, TransactionType = TransactionType.Subscription, Status = TransactionStatus.Completed, Amount = 100000, CreatedAt = new DateTime(currentYear, 3, 1) },
                new Transaction { Id = 2, TransactionType = TransactionType.Penalty, Status = TransactionStatus.Completed, Amount = 50000, CreatedAt = new DateTime(currentYear, 3, 15) }
            }.AsQueryable().BuildMock();

            _mockTransactionRepo.Setup(r => r.GetAllTransactionsQueryable()).Returns(transactions);
            _mockSystemConfigService.Setup(s => s.GetCancellationRefundRateAsync()).ReturnsAsync(80m);

            var service = CreateService();
            var parameters = new RevenueTransactionQueryParameters 
            { 
                Year = currentYear, TransactionType = "Subscription", 
                PageNumber = 1, PageSize = 10 
            };

            var result = await service.GetRevenueTransactionsAsync(parameters);

            result.Items.Should().HaveCount(1);
            result.Items[0].TransactionType.Should().Be("Subscription");
        }

        // UTC_RVT_05: Throw khi TransactionType không hợp lệ
        [Fact]
        public async Task GetRevenueTransactionsAsync_ShouldThrowArgumentException_WhenTransactionTypeInvalid()
        {
            var currentYear = DateTime.UtcNow.Year;
            var transactions = new List<Transaction>().AsQueryable().BuildMock();
            _mockTransactionRepo.Setup(r => r.GetAllTransactionsQueryable()).Returns(transactions);
            _mockSystemConfigService.Setup(s => s.GetCancellationRefundRateAsync()).ReturnsAsync(80m);

            var service = CreateService();
            var parameters = new RevenueTransactionQueryParameters 
            { 
                Year = currentYear, TransactionType = "InvalidType",
                PageNumber = 1, PageSize = 10 
            };

            var act = () => service.GetRevenueTransactionsAsync(parameters);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*không hợp lệ*");
        }

        // UTC_RVT_06: Tính profit đúng cho Subscription (profit = amount)
        [Fact]
        public async Task GetRevenueTransactionsAsync_ShouldCalculateProfit_ForSubscriptionTransactions()
        {
            var currentYear = DateTime.UtcNow.Year;
            var transactions = new List<Transaction>
            {
                new Transaction { Id = 1, TransactionType = TransactionType.Subscription, Status = TransactionStatus.Completed, Amount = 100000, CreatedAt = new DateTime(currentYear, 3, 1) }
            }.AsQueryable().BuildMock();

            _mockTransactionRepo.Setup(r => r.GetAllTransactionsQueryable()).Returns(transactions);
            _mockSystemConfigService.Setup(s => s.GetCancellationRefundRateAsync()).ReturnsAsync(80m);

            var service = CreateService();
            var parameters = new RevenueTransactionQueryParameters { Year = currentYear, PageNumber = 1, PageSize = 10 };

            var result = await service.GetRevenueTransactionsAsync(parameters);

            result.Items[0].Profit.Should().Be(100000); // Profit = Amount cho subscription
        }

        #endregion
    }
}
