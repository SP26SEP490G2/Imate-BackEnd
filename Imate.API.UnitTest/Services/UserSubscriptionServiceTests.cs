using Moq;
using FluentAssertions;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Services.Payment;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using MockQueryable;
using MockQueryable.Moq;
using Xunit;
using Imate.API.Business.Interfaces.Notification;

namespace Imate.API.UnitTest.Services
{
    public class UserSubscriptionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<IUserSubscriptionRepository> _mockUserSubRepo;
        private readonly Mock<ISubscriptionPackageRepository> _mockPackageRepo;
        private readonly Mock<ITransactionRepository> _mockTransactionRepo;
        private readonly Mock<ISystemConfigService> _mockSystemConfigService;
        private readonly Mock<ISystemNotificationService> _mockSystemNotificationService;
        private readonly UserSubscriptionService _service;

        public UserSubscriptionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockUserSubRepo = new Mock<IUserSubscriptionRepository>();
            _mockPackageRepo = new Mock<ISubscriptionPackageRepository>();
            _mockTransactionRepo = new Mock<ITransactionRepository>();
            _mockSystemConfigService = new Mock<ISystemConfigService>();
            _mockSystemNotificationService = new Mock<ISystemNotificationService>();

            _mockUnitOfWork.Setup(u => u.Accounts).Returns(_mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.UserSubscriptions).Returns(_mockUserSubRepo.Object);
            _mockUnitOfWork.Setup(u => u.SubscriptionPackages).Returns(_mockPackageRepo.Object);
            _mockUnitOfWork.Setup(u => u.Transactions).Returns(_mockTransactionRepo.Object);

            _service = new UserSubscriptionService(
                _mockUnitOfWork.Object,
                _mockSystemConfigService.Object,
                _mockSystemNotificationService.Object
            );
        }

        #region UC-4: ActivateNewSubscriptionAsync

        // UTC_SUB_01: Đăng ký gói mới thành công khi chưa có gói active → tạo UserSubscription, trừ balance, tạo Transaction
        [Fact]
        public async Task ActivateNewSubscriptionAsync_ShouldSucceed_WhenNoExistingSubscription()
        {
            // Arrange
            var accountId = 1;
            var packageId = 2;
            var newPackage = new SubscriptionPackage
            {
                Id = packageId,
                Name = "Premium",
                Price = 100000,
                Rank = 2,
                DurationDays = 30,
                TotalInterviewLimit = 50
            };
            var account = new Account { Id = accountId, Balance = 200000 };

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(packageId)).ReturnsAsync(newPackage);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Không có gói active
            var emptySubs = new List<UserSubscription>().AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(emptySubs);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockTransactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = 100; return t; });

            // Act
            await _service.ActivateNewSubscriptionAsync(accountId, packageId);

            // Assert
            account.Balance.Should().Be(100000); // 200000 - 100000
            _mockUserSubRepo.Verify(r => r.AddUserSubscription(It.Is<UserSubscription>(
                s => s.CandidateId == accountId &&
                     s.PackageId == packageId &&
                     s.IsActive &&
                     s.InitialMockLimit == 50 &&
                     s.MockInterviewUsed == 0
            )), Times.Once);
            _mockTransactionRepo.Verify(r => r.AddAsync(It.Is<Transaction>(
                t => t.Amount == 100000 &&
                     t.TransactionType == TransactionType.Subscription &&
                     t.Status == TransactionStatus.Completed
            )), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        // UTC_SUB_02: Nâng cấp gói thành công với proration → trừ chênh lệch giá
        [Fact]
        public async Task ActivateNewSubscriptionAsync_ShouldApplyProration_WhenUpgrading()
        {
            // Arrange
            var accountId = 1;
            var newPackageId = 3;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var oldPackage = new SubscriptionPackage
            {
                Id = 2, Name = "Basic", Price = 60000, Rank = 1, DurationDays = 30
            };
            var newPackage = new SubscriptionPackage
            {
                Id = newPackageId, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30,
                TotalInterviewLimit = 100
            };

            var existingSub = new UserSubscription
            {
                Id = 1, CandidateId = accountId, PackageId = 2, IsActive = true,
                StartDate = today.AddDays(-15),
                EndDate = today.AddDays(15) // Còn 15 ngày
            };

            var account = new Account { Id = accountId, Balance = 200000 };

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(newPackageId)).ReturnsAsync(newPackage);
            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(2)).ReturnsAsync(oldPackage);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Có gói active
            var activeSubs = new List<UserSubscription> { existingSub }.AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(activeSubs);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockTransactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = 100; return t; });

            // Act
            await _service.ActivateNewSubscriptionAsync(accountId, newPackageId);

            // Assert - Proration: remainingValue = (60000/30) * 15 = 30000, charge = 100000 - 30000 = 70000
            account.Balance.Should().Be(130000); // 200000 - 70000
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        // UTC_SUB_03: Nâng cấp khi remainingValue > newPrice → charge = 0
        [Fact]
        public async Task ActivateNewSubscriptionAsync_ShouldChargeZero_WhenRemainingValueExceedsNewPrice()
        {
            // Arrange
            var accountId = 1;
            var newPackageId = 3;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Gói cũ rất đắt, còn nhiều ngày
            var oldPackage = new SubscriptionPackage
            {
                Id = 2, Name = "Enterprise", Price = 300000, Rank = 1, DurationDays = 30
            };
            // Gói mới rẻ hơn nhưng rank cao hơn
            var newPackage = new SubscriptionPackage
            {
                Id = newPackageId, Name = "Premium Plus", Price = 50000, Rank = 2, DurationDays = 30
            };

            var existingSub = new UserSubscription
            {
                Id = 1, CandidateId = accountId, PackageId = 2, IsActive = true,
                StartDate = today.AddDays(-5),
                EndDate = today.AddDays(25) // Còn 25 ngày → remainingValue = 250k > 50k
            };

            var account = new Account { Id = accountId, Balance = 200000 };

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(newPackageId)).ReturnsAsync(newPackage);
            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(2)).ReturnsAsync(oldPackage);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var activeSubs = new List<UserSubscription> { existingSub }.AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(activeSubs);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockTransactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = 100; return t; });

            // Act
            await _service.ActivateNewSubscriptionAsync(accountId, newPackageId);

            // Assert - amountToCharge = max(0, 50000 - 250000) = 0
            account.Balance.Should().Be(200000); // Không bị trừ
            _mockTransactionRepo.Verify(r => r.AddAsync(It.Is<Transaction>(
                t => t.Amount == 0
            )), Times.Once);
        }

        // UTC_SUB_04: Throw NotFoundException khi packageId không tồn tại
        [Fact]
        public async Task ActivateNewSubscriptionAsync_ShouldThrowNotFoundException_WhenPackageNotFound()
        {
            // Arrange
            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(999)).ReturnsAsync((SubscriptionPackage?)null);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var act = () => _service.ActivateNewSubscriptionAsync(1, 999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Không tìm thấy gói đăng ký mới.");
        }

        // UTC_SUB_05: Throw NotFoundException khi accountId không tồn tại
        [Fact]
        public async Task ActivateNewSubscriptionAsync_ShouldThrowNotFoundException_WhenAccountNotFound()
        {
            // Arrange
            var package = new SubscriptionPackage { Id = 2, Name = "Premium", Price = 100000, Rank = 2 };
            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(2)).ReturnsAsync(package);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Account?)null);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var act = () => _service.ActivateNewSubscriptionAsync(999, 2);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Không tìm thấy tài khoản người dùng.");
        }

        // UTC_SUB_06: Throw BadRequestException khi hạ cấp gói (newRank <= oldRank)
        [Fact]
        public async Task ActivateNewSubscriptionAsync_ShouldThrowBadRequest_WhenDowngrading()
        {
            // Arrange
            var accountId = 1;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var oldPackage = new SubscriptionPackage { Id = 2, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30 };
            var newPackage = new SubscriptionPackage { Id = 1, Name = "Basic", Price = 50000, Rank = 1, DurationDays = 30 };

            var existingSub = new UserSubscription
            {
                Id = 1, CandidateId = accountId, PackageId = 2, IsActive = true,
                StartDate = today.AddDays(-10), EndDate = today.AddDays(20)
            };

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(1)).ReturnsAsync(newPackage);
            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(2)).ReturnsAsync(oldPackage);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(new Account { Id = accountId, Balance = 200000 });

            var activeSubs = new List<UserSubscription> { existingSub }.AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(activeSubs);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var act = () => _service.ActivateNewSubscriptionAsync(accountId, 1);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*Không thể hạ cấp*");
        }

        // UTC_SUB_07: Throw BadRequestException khi số dư không đủ
        [Fact]
        public async Task ActivateNewSubscriptionAsync_ShouldThrowBadRequest_WhenInsufficientBalance()
        {
            // Arrange
            var accountId = 1;
            var packageId = 2;
            var newPackage = new SubscriptionPackage
            {
                Id = packageId, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30
            };
            var account = new Account { Id = accountId, Balance = 10000 }; // Không đủ

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(packageId)).ReturnsAsync(newPackage);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var emptySubs = new List<UserSubscription>().AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(emptySubs);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var act = () => _service.ActivateNewSubscriptionAsync(accountId, packageId);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*Số dư không đủ*");
        }

        // UTC_SUB_08: Rollback khi exception xảy ra
        [Fact]
        public async Task ActivateNewSubscriptionAsync_ShouldRollback_WhenExceptionOccurs()
        {
            // Arrange
            var accountId = 1;
            var packageId = 2;
            var newPackage = new SubscriptionPackage
            {
                Id = packageId, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30
            };
            var account = new Account { Id = accountId, Balance = 200000 };

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(packageId)).ReturnsAsync(newPackage);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var emptySubs = new List<UserSubscription>().AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(emptySubs);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new Exception("DB connection lost"));
            _mockTransactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = 100; return t; });

            // Act
            var act = () => _service.ActivateNewSubscriptionAsync(accountId, packageId);

            // Assert
            await act.Should().ThrowAsync<Exception>();
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        // UTC_SUB_09: InitialMockLimit = TotalInterviewLimit khi có giá trị
        [Fact]
        public async Task ActivateNewSubscriptionAsync_ShouldSetInitialMockLimit_FromTotalInterviewLimit()
        {
            // Arrange
            var accountId = 1;
            var packageId = 2;
            var newPackage = new SubscriptionPackage
            {
                Id = packageId, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30,
                TotalInterviewLimit = 50
            };
            var account = new Account { Id = accountId, Balance = 200000 };

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(packageId)).ReturnsAsync(newPackage);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var emptySubs = new List<UserSubscription>().AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(emptySubs);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockTransactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = 100; return t; });

            // Act
            await _service.ActivateNewSubscriptionAsync(accountId, packageId);

            // Assert
            _mockUserSubRepo.Verify(r => r.AddUserSubscription(It.Is<UserSubscription>(
                s => s.InitialMockLimit == 50
            )), Times.Once);
        }

        // UTC_SUB_10: InitialMockLimit = int.MaxValue khi không có TotalInterviewLimit
        [Fact]
        public async Task ActivateNewSubscriptionAsync_ShouldSetMaxLimit_WhenNoTotalInterviewLimit()
        {
            // Arrange
            var accountId = 1;
            var packageId = 2;
            var newPackage = new SubscriptionPackage
            {
                Id = packageId, Name = "Unlimited", Price = 200000, Rank = 3, DurationDays = 30,
                TotalInterviewLimit = null
            };
            var account = new Account { Id = accountId, Balance = 300000 };

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(packageId)).ReturnsAsync(newPackage);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var emptySubs = new List<UserSubscription>().AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(emptySubs);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockTransactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = 100; return t; });

            // Act
            await _service.ActivateNewSubscriptionAsync(accountId, packageId);

            // Assert
            _mockUserSubRepo.Verify(r => r.AddUserSubscription(It.Is<UserSubscription>(
                s => s.InitialMockLimit == int.MaxValue
            )), Times.Once);
        }

        #endregion

        #region UC-4: GetUpgradePreviewAsync

        // UTC_SUB_11: Preview đăng ký mới khi chưa có gói → full price
        [Fact]
        public async Task GetUpgradePreviewAsync_ShouldReturnFullPrice_WhenNoActiveSubscription()
        {
            // Arrange
            var accountId = 1;
            var packageId = 2;
            var newPackage = new SubscriptionPackage
            {
                Id = packageId, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30
            };

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(packageId)).ReturnsAsync(newPackage);

            var emptySubs = new List<UserSubscription>().AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(emptySubs);

            // Act
            var result = await _service.GetUpgradePreviewAsync(accountId, packageId);

            // Assert
            result.NewPackageName.Should().Be("Premium");
            result.NewPackagePrice.Should().Be(100000);
            result.AmountToCharge.Should().Be(100000);
            result.HasActiveSubscription.Should().BeFalse();
            result.IsEligible.Should().BeTrue();
            result.RemainingValue.Should().Be(0);
        }

        // UTC_SUB_12: Preview nâng cấp với proration
        [Fact]
        public async Task GetUpgradePreviewAsync_ShouldCalculateProration_WhenUpgrading()
        {
            // Arrange
            var accountId = 1;
            var newPackageId = 3;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var oldPackage = new SubscriptionPackage
            {
                Id = 2, Name = "Basic", Price = 60000, Rank = 1, DurationDays = 30
            };
            var newPackage = new SubscriptionPackage
            {
                Id = newPackageId, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30
            };

            var existingSub = new UserSubscription
            {
                Id = 1, CandidateId = accountId, PackageId = 2, IsActive = true,
                StartDate = today.AddDays(-15), EndDate = today.AddDays(15)
            };

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(newPackageId)).ReturnsAsync(newPackage);
            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(2)).ReturnsAsync(oldPackage);

            var activeSubs = new List<UserSubscription> { existingSub }.AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(activeSubs);

            // Act
            var result = await _service.GetUpgradePreviewAsync(accountId, newPackageId);

            // Assert - remainingValue = (60000/30)*15 = 30000
            result.HasActiveSubscription.Should().BeTrue();
            result.OldPackageName.Should().Be("Basic");
            result.RemainingValue.Should().Be(30000);
            result.AmountToCharge.Should().Be(70000); // 100000 - 30000
        }

        // UTC_SUB_13: Throw NotFoundException khi package không tồn tại
        [Fact]
        public async Task GetUpgradePreviewAsync_ShouldThrowNotFoundException_WhenPackageNotFound()
        {
            // Arrange
            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(999)).ReturnsAsync((SubscriptionPackage?)null);

            // Act
            var act = () => _service.GetUpgradePreviewAsync(1, 999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Không tìm thấy gói đăng ký mới.");
        }

        // UTC_SUB_14: Throw BadRequestException khi hạ cấp
        [Fact]
        public async Task GetUpgradePreviewAsync_ShouldThrowBadRequest_WhenDowngrading()
        {
            // Arrange
            var accountId = 1;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var oldPackage = new SubscriptionPackage { Id = 2, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30 };
            var newPackage = new SubscriptionPackage { Id = 1, Name = "Basic", Price = 50000, Rank = 1, DurationDays = 30 };

            var existingSub = new UserSubscription
            {
                Id = 1, CandidateId = accountId, PackageId = 2, IsActive = true,
                StartDate = today.AddDays(-10), EndDate = today.AddDays(20)
            };

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(1)).ReturnsAsync(newPackage);
            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(2)).ReturnsAsync(oldPackage);

            var activeSubs = new List<UserSubscription> { existingSub }.AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(activeSubs);

            // Act
            var act = () => _service.GetUpgradePreviewAsync(accountId, 1);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*Không thể hạ cấp*");
        }

        #endregion

        #region UC-4: CancelSubscriptionAsync

        // UTC_SUB_15: Hủy gói thành công với hoàn tiền prorated
        [Fact]
        public async Task CancelSubscriptionAsync_ShouldRefundAndDeactivate_WhenValidSubscription()
        {
            // Arrange
            var accountId = 1;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var package = new SubscriptionPackage
            {
                Id = 2, Name = "Premium", Price = 60000, Rank = 2, DurationDays = 30
            };
            var lowestPackage = new SubscriptionPackage { Id = 1, Name = "Gói Thường", Rank = 0, Price = 0 };

            var activeSub = new UserSubscription
            {
                Id = 1, CandidateId = accountId, PackageId = 2, IsActive = true,
                StartDate = today.AddDays(-15), EndDate = today.AddDays(15),
                Package = package
            };
            var account = new Account { Id = accountId, Balance = 50000 };

            // Mock GetCurrentPackageAsync dependencies
            var activeSubsForCurrent = new List<UserSubscription> { activeSub }.AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(activeSubsForCurrent);
            _mockPackageRepo.Setup(r => r.GetLowestRankPackageAsync()).ReturnsAsync(lowestPackage);
            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(2)).ReturnsAsync(package);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockTransactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => { t.Id = 200; return t; });

            // Act
            await _service.CancelSubscriptionAsync(accountId);

            // Assert - refundAmount = (60000/30)*15 = 30000
            activeSub.IsActive.Should().BeFalse();
            account.Balance.Should().Be(80000); // 50000 + 30000
            _mockTransactionRepo.Verify(r => r.AddAsync(It.Is<Transaction>(
                t => t.TransactionType == TransactionType.Refund &&
                     t.Status == TransactionStatus.Completed &&
                     t.Amount == 30000
            )), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        // UTC_SUB_16: Throw BadRequestException khi đang dùng gói thấp nhất
        [Fact]
        public async Task CancelSubscriptionAsync_ShouldThrowBadRequest_WhenUsingLowestPackage()
        {
            // Arrange
            var accountId = 1;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var lowestPackage = new SubscriptionPackage { Id = 1, Name = "Gói Thường", Rank = 0, Price = 0 };

            var activeSub = new UserSubscription
            {
                Id = 1, CandidateId = accountId, PackageId = 1, IsActive = true,
                StartDate = today, EndDate = null,
                Package = lowestPackage
            };

            var activeSubsQuery = new List<UserSubscription> { activeSub }.AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(activeSubsQuery);
            _mockPackageRepo.Setup(r => r.GetLowestRankPackageAsync()).ReturnsAsync(lowestPackage);
            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(1)).ReturnsAsync(lowestPackage);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var act = () => _service.CancelSubscriptionAsync(accountId);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*Gói Thường*không thể hủy*");
        }

        // UTC_SUB_17: Rollback khi exception xảy ra trong CancelSubscription
        [Fact]
        public async Task CancelSubscriptionAsync_ShouldRollback_WhenExceptionOccurs()
        {
            // Arrange
            var accountId = 1;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var package = new SubscriptionPackage { Id = 2, Name = "Premium", Price = 60000, Rank = 2, DurationDays = 30 };
            var lowestPackage = new SubscriptionPackage { Id = 1, Name = "Gói Thường", Rank = 0 };

            var activeSub = new UserSubscription
            {
                Id = 1, CandidateId = accountId, PackageId = 2, IsActive = true,
                StartDate = today.AddDays(-15), EndDate = today.AddDays(15),
                Package = package
            };

            var activeSubsQuery = new List<UserSubscription> { activeSub }.AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(activeSubsQuery);
            _mockPackageRepo.Setup(r => r.GetLowestRankPackageAsync()).ReturnsAsync(lowestPackage);
            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(2)).ReturnsAsync(package);
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null); // Sẽ throw

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var act = () => _service.CancelSubscriptionAsync(accountId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        #endregion

        #region UC-4: GetCancelPreviewAsync

        // UTC_SUB_18: Preview hủy gói với refund prorated
        [Fact]
        public async Task GetCancelPreviewAsync_ShouldReturnRefundPreview_WhenActiveSubscriptionExists()
        {
            // Arrange
            var accountId = 1;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var package = new SubscriptionPackage
            {
                Id = 2, Name = "Premium", Price = 60000, DurationDays = 30, Rank = 2
            };

            var activeSub = new UserSubscription
            {
                Id = 1, CandidateId = accountId, PackageId = 2, IsActive = true,
                StartDate = today.AddDays(-15), EndDate = today.AddDays(15),
                Package = package
            };

            var activeSubsQuery = new List<UserSubscription> { activeSub }.AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(activeSubsQuery);

            // Act
            var result = await _service.GetCancelPreviewAsync(accountId);

            // Assert
            result.PackageToCancel.Should().Be("Premium");
            result.RemainingDays.Should().Be(15);
            result.RefundAmount.Should().Be(30000); // (60000/30)*15
        }

        // UTC_SUB_19: Throw BadRequestException khi không có gói trả phí active
        [Fact]
        public async Task GetCancelPreviewAsync_ShouldThrowBadRequest_WhenNoActiveSubscription()
        {
            // Arrange
            var accountId = 1;

            var emptySubs = new List<UserSubscription>().AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(emptySubs);

            // Act
            var act = () => _service.GetCancelPreviewAsync(accountId);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*không có gói trả phí nào*");
        }

        #endregion
    }
}
