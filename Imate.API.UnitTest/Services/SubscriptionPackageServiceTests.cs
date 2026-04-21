using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using Imate.API.Business.Services.Payment;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Xunit;

namespace Imate.API.UnitTest.Services
{
    public class SubscriptionPackageServiceTests
    {
        private readonly Mock<ISubscriptionPackageRepository> _mockPackageRepo;
        private readonly ImateDbContext _context;
        private readonly SubscriptionPackageService _service;

        public SubscriptionPackageServiceTests()
        {
            _mockPackageRepo = new Mock<ISubscriptionPackageRepository>();

            var options = new DbContextOptionsBuilder<ImateDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ImateDbContext(options);
            _service = new SubscriptionPackageService(_mockPackageRepo.Object, _context);
        }

        #region View Subscription
        [Fact]
        public async Task GetPublicSubscriptionPackagesAsync_ShouldReturnActivePackages()
        {
            var packages = new List<SubscriptionPackage>
            {
                new SubscriptionPackage { Id = 1, Name = "Premium", Price = 100, IsActive = true, Rank = 1, Benefits = "{\"features\":[\"Feature 1\"]}", DurationDays = 30 },
                new SubscriptionPackage { Id = 2, Name = "Enterprise", Price = 200, IsActive = true, Rank = 2, Benefits = "{\"features\":[\"Feature 2\"]}", DurationDays = 30 }
            };

            _mockPackageRepo.Setup(r => r.GetActivePackagesOrderedByPriceAsync()).ReturnsAsync(packages);

            var result = await _service.GetPublicSubscriptionPackagesAsync();

            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Premium");
        }

        [Fact]
        public async Task GetPublicSubscriptionPackagesAsync_ShouldReturnEmpty_WhenNoActivePackages()
        {
            _mockPackageRepo.Setup(r => r.GetActivePackagesOrderedByPriceAsync()).ReturnsAsync(new List<SubscriptionPackage>());
            var result = await _service.GetPublicSubscriptionPackagesAsync();
            result.Should().BeEmpty();
        }
        [Fact]
        public async Task GetPublicSubscriptionPackagesAsync_ShouldMapDurationAndBenefitsCorrectly()
        {
            var packages = new List<SubscriptionPackage>
            {
                new SubscriptionPackage
                {
                    Id = 1,
                    Name = "Premium",
                    Price = 100,
                    IsActive = true,
                    Rank = 1,
                    Benefits = "{\"features\":[\"Feature 1\",\"Feature 2\"]}",
                    DurationDays = 30
                }
            };

            _mockPackageRepo.Setup(r => r.GetActivePackagesOrderedByPriceAsync()).ReturnsAsync(packages);

            var result = await _service.GetPublicSubscriptionPackagesAsync();

            var item = result.First();
            item.Name.Should().Be("Premium");
            item.Duration.Should().Be("1 tháng");
            item.Benefits.Should().HaveCount(2);
        }
        #endregion

        #region View Subscriptions for Admin
        [Fact]
        public async Task GetSubscriptionOverviewAsync_ShouldCalculateCorrectTotals()
        {
            var currentYear = DateTime.UtcNow.Year;
            var package = new SubscriptionPackage { Id = 1, Name = "Premium", IsActive = true, IsRecommended = true };
            _context.SubscriptionPackages.Add(package);
            
            _context.UserSubscriptions.AddRange(new List<UserSubscription>
            {
                new UserSubscription { Id = 1, PackageId = 1, Package = package, CreatedAt = new DateTime(currentYear, 1, 15, 0, 0, 0, DateTimeKind.Utc) },
                new UserSubscription { Id = 2, PackageId = 1, Package = package, CreatedAt = new DateTime(currentYear, 1, 20, 0, 0, 0, DateTimeKind.Utc) }
            });

            _context.Transactions.AddRange(new List<Transaction>
            {
                new Transaction { Id = 1, TransactionType = TransactionType.Subscription, Status = TransactionStatus.Completed, Amount = 1000 },
                new Transaction { Id = 2, TransactionType = TransactionType.Subscription, Status = TransactionStatus.Completed, Amount = 2000 }
            });

            await _context.SaveChangesAsync();
            var result = await _service.GetSubscriptionOverviewAsync();

            result.TotalSold.Should().Be(2);
            result.TotalRevenue.Should().Be(3000);
            result.FeaturedPackageName.Should().Be("Premium");
            result.MonthlySales.Should().HaveCount(12);
            result.MonthlySales.First(m => m.Month == 1).PackageSales["Premium"].Should().Be(2);
        }

        [Fact]
        public async Task GetSubscriptionOverviewAsync_ShouldHandleNullRevenue()
        {
            var result = await _service.GetSubscriptionOverviewAsync();
            result.TotalRevenue.Should().Be(0);
        }

        [Fact]
        public async Task GetSubscriptionOverviewAsync_ShouldFilterOutSalesFromOtherYears()
        {
            var currentYear = DateTime.UtcNow.Year;
            var lastYear = currentYear - 1;
            var package = new SubscriptionPackage { Id = 1, Name = "Premium", IsActive = true };
            _context.SubscriptionPackages.Add(package);

            _context.UserSubscriptions.AddRange(new List<UserSubscription>
            {
                new UserSubscription { Id = 1, PackageId = 1, Package = package, CreatedAt = new DateTime(currentYear, 5, 1, 0, 0, 0, DateTimeKind.Utc) },
                new UserSubscription { Id = 2, PackageId = 1, Package = package, CreatedAt = new DateTime(lastYear, 5, 1, 0, 0, 0, DateTimeKind.Utc) }
            });

            await _context.SaveChangesAsync();

            var result = await _service.GetSubscriptionOverviewAsync();

            result.TotalSold.Should().Be(2); 
            result.MonthlySales.First(m => m.Month == 5).PackageSales["Premium"].Should().Be(1);
        }

        [Fact]
        public async Task GetSubscriptionOverviewAsync_ShouldReturnEmptyOverview_WhenNoData()
        {
            var result = await _service.GetSubscriptionOverviewAsync();

            result.TotalSold.Should().Be(0);
            result.TotalRevenue.Should().Be(0);
            result.FeaturedPackageName.Should().BeNull();
            result.MonthlySales.Should().HaveCount(12);
            result.MonthlySales.All(m => m.PackageSales.Count == 0).Should().BeTrue();
        }
        #endregion
    }
}
