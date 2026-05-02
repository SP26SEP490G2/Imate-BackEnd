using Moq;
using FluentAssertions;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Notification;
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
using MediatR;
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
        private readonly Mock<IMediator> _mockMediator;
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
            _mockMediator = new Mock<IMediator>();

            _mockUnitOfWork.Setup(u => u.Accounts).Returns(_mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.UserSubscriptions).Returns(_mockUserSubRepo.Object);
            _mockUnitOfWork.Setup(u => u.SubscriptionPackages).Returns(_mockPackageRepo.Object);
            _mockUnitOfWork.Setup(u => u.Transactions).Returns(_mockTransactionRepo.Object);

            _service = new UserSubscriptionService(
                _mockUnitOfWork.Object,
                _mockSystemConfigService.Object,
                _mockSystemNotificationService.Object,
                _mockMediator.Object
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
                TotalInterviewLimit = 50,
                IsActive = true
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

        // UTC_SUB_02: Nâng cấp gói thành công → trừ full giá gói mới (không còn proration)
        [Fact]
        public async Task ActivateNewSubscriptionAsync_ShouldChargeFullPrice_WhenUpgrading()
        {
            // Arrange
            var accountId = 1;
            var newPackageId = 3;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var oldPackage = new SubscriptionPackage
            {
                Id = 2, Name = "Basic", Price = 60000, Rank = 1, DurationDays = 30, IsActive = true
            };
            var newPackage = new SubscriptionPackage
            {
                Id = newPackageId, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30,
                TotalInterviewLimit = 100, IsActive = true
            };

            var existingSub = new UserSubscription
            {
                Id = 1, CandidateId = accountId, PackageId = 2, IsActive = true,
                StartDate = today.AddDays(-15),
                EndDate = today.AddDays(15),
                Package = oldPackage
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

            // Assert - Luôn charge full price: 200000 - 100000 = 100000
            account.Balance.Should().Be(100000);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        // UTC_SUB_03: Nâng cấp gói thành công → trừ full giá gói mới (kể cả khi gói cũ còn giá trị)
        [Fact]
        public async Task ActivateNewSubscriptionAsync_ShouldChargeFullPrice_WhenUpgradingEvenIfRemainingValueExists()
        {
            // Arrange
            var accountId = 1;
            var newPackageId = 3;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Gói cũ rất đắt, còn nhiều ngày
            var oldPackage = new SubscriptionPackage
            {
                Id = 2, Name = "Enterprise", Price = 300000, Rank = 1, DurationDays = 30, IsActive = true
            };
            // Gói mới rẻ hơn nhưng rank cao hơn
            var newPackage = new SubscriptionPackage
            {
                Id = newPackageId, Name = "Premium Plus", Price = 50000, Rank = 2, DurationDays = 30, IsActive = true
            };

            var existingSub = new UserSubscription
            {
                Id = 1, CandidateId = accountId, PackageId = 2, IsActive = true,
                StartDate = today.AddDays(-5),
                EndDate = today.AddDays(25), // Còn 25 ngày → remainingValue = 250k
                Package = oldPackage
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

            // Assert - Luôn charge full price: 200000 - 50000 = 150000
            account.Balance.Should().Be(150000);
            _mockTransactionRepo.Verify(r => r.AddAsync(It.Is<Transaction>(
                t => t.Amount == 50000
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
                .WithMessage("Không tìm thấy gói đăng ký.");
        }

        // UTC_SUB_05: Throw NotFoundException khi accountId không tồn tại
        [Fact]
        public async Task ActivateNewSubscriptionAsync_ShouldThrowNotFoundException_WhenAccountNotFound()
        {
            // Arrange
            var package = new SubscriptionPackage { Id = 2, Name = "Premium", Price = 100000, Rank = 2, IsActive = true };
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

            var oldPackage = new SubscriptionPackage { Id = 2, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30, IsActive = true };
            var newPackage = new SubscriptionPackage { Id = 1, Name = "Basic", Price = 50000, Rank = 1, DurationDays = 30, IsActive = true };

            var existingSub = new UserSubscription
            {
                Id = 1, CandidateId = accountId, PackageId = 2, IsActive = true,
                StartDate = today.AddDays(-10), EndDate = today.AddDays(20),
                Package = oldPackage
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
                .WithMessage("*Vui lòng chọn gói cao cấp hơn.*");
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
                Id = packageId, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30, IsActive = true
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
                Id = packageId, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30, IsActive = true
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
                TotalInterviewLimit = 50, IsActive = true
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
                TotalInterviewLimit = null, IsActive = true
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
                Id = packageId, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30, IsActive = true
            };

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(packageId)).ReturnsAsync(newPackage);

            var emptySubs = new List<UserSubscription>().AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(emptySubs);

            // Act
            var result = await _service.GetUpgradePreviewAsync(accountId, packageId);

            // Assert
            result.NewPackageName.Should().Be("Premium");
            result.NewPackagePrice.Should().Be(100000);
            result.IsEligible.Should().BeTrue();
        }

        // UTC_SUB_12: Preview nâng cấp (không còn proration)
        [Fact]
        public async Task GetUpgradePreviewAsync_ShouldReturnFullPrice_WhenUpgrading()
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
                StartDate = today.AddDays(-15), EndDate = today.AddDays(15),
                Package = oldPackage
            };

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(newPackageId)).ReturnsAsync(newPackage);
            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(2)).ReturnsAsync(oldPackage);

            var activeSubs = new List<UserSubscription> { existingSub }.AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(activeSubs);

            // Act
            var result = await _service.GetUpgradePreviewAsync(accountId, newPackageId);

            // Assert - Luôn trả về full price
            result.NewPackageName.Should().Be("Premium");
            result.NewPackagePrice.Should().Be(100000);
            result.IsEligible.Should().BeTrue();
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
                .WithMessage("Không tìm thấy gói đăng ký.");
        }

        // UTC_SUB_14: Throw BadRequestException khi hạ cấp
        [Fact]
        public async Task GetUpgradePreviewAsync_ShouldThrowBadRequest_WhenDowngrading()
        {
            // Arrange
            var accountId = 1;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var oldPackage = new SubscriptionPackage { Id = 2, Name = "Premium", Price = 100000, Rank = 2, DurationDays = 30, IsActive = true };
            var newPackage = new SubscriptionPackage { Id = 1, Name = "Basic", Price = 50000, Rank = 1, DurationDays = 30, IsActive = true };

            var existingSub = new UserSubscription
            {
                Id = 1, CandidateId = accountId, PackageId = 2, IsActive = true,
                StartDate = today.AddDays(-10), EndDate = today.AddDays(20),
                Package = oldPackage
            };

            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(1)).ReturnsAsync(newPackage);
            _mockPackageRepo.Setup(r => r.GetSubscriptionPackageByIdAsync(2)).ReturnsAsync(oldPackage);

            var activeSubs = new List<UserSubscription> { existingSub }.AsQueryable().BuildMock();
            _mockUserSubRepo.Setup(r => r.GetUserSubscriptions()).Returns(activeSubs);

            // Act
            var act = () => _service.GetUpgradePreviewAsync(accountId, 1);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*Vui lòng chọn gói cao cấp hơn.*");
        }

        #endregion


    }
}
