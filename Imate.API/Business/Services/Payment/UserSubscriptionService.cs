using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Notification;
using Imate.API.Business.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.ResponseModels.Payment;
using Microsoft.EntityFrameworkCore;
using Imate.API.Presentation.SignalR.Events.Transactions;
using MediatR;

namespace Imate.API.Business.Services.Payment
{
    public class UserSubscriptionService : IUserSubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISystemConfigService _systemConfigService;
        private readonly ISystemNotificationService _systemNotificationService;
        private readonly IMediator _mediator;

        public UserSubscriptionService(
            IUnitOfWork unitOfWork,
            ISystemConfigService systemConfigService,
            ISystemNotificationService systemNotificationService,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _systemConfigService = systemConfigService;
            _systemNotificationService = systemNotificationService;
            _mediator = mediator;
        }

        public async Task ActivateNewSubscriptionAsync(int accountId, int newPackageId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Lấy thông tin gói mới và tài khoản
                var newPackage = await _unitOfWork.SubscriptionPackages.GetSubscriptionPackageByIdAsync(newPackageId);
                if (newPackage == null)
                    throw new NotFoundException("Không tìm thấy gói đăng ký.");

                if (!newPackage.IsActive)
                    throw new BadRequestException("Gói đăng ký này hiện không khả dụng.");

                var userAccount = await _unitOfWork.Accounts.GetByIdAsync(accountId);
                if (userAccount == null)
                    throw new NotFoundException("Không tìm thấy tài khoản người dùng.");

                // 2. Kiểm tra gói hiện tại
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var existingActiveSub = await _unitOfWork.UserSubscriptions
                    .GetUserSubscriptions()
                    .Include(s => s.Package)
                    .Where(s => s.CandidateId == accountId
                             && s.IsActive
                             && (s.EndDate == null || s.EndDate >= today))
                    .FirstOrDefaultAsync();

                if (existingActiveSub != null)
                {
                    // Không cho phép mua gói rank thấp hơn hoặc bằng gói hiện tại
                    if (newPackage.Rank <= existingActiveSub.Package.Rank)
                        throw new BadRequestException(
                            $"Bạn đang sử dụng gói {existingActiveSub.Package.Name}. " +
                            $"Vui lòng chọn gói cao cấp hơn.");
                }

                // 3. Kiểm tra số dư — luôn charge full giá gói mới
                if (userAccount.Balance < newPackage.Price)
                    throw new BadRequestException(
                        $"Số dư không đủ. Cần {newPackage.Price:N0}đ, hiện có {userAccount.Balance:N0}đ.");

                // 4. Hủy toàn bộ subscription cũ (không hoàn tiền)
                var allActiveSubs = await _unitOfWork.UserSubscriptions
                    .GetUserSubscriptions()
                    .Where(s => s.CandidateId == accountId && s.IsActive)
                    .ToListAsync();

                foreach (var oldSub in allActiveSubs)
                {
                    oldSub.IsActive = false;
                    oldSub.UpdatedAt = DateTimeOffset.UtcNow;
                }

                // 5. Tính InitialMockLimit cho gói mới
                int initialLimit = newPackage.TotalInterviewLimit ?? int.MaxValue;

                // 6. Tạo UserSubscription mới
                var now = DateTime.UtcNow;
                var newUserSubscription = new UserSubscription
                {
                    CandidateId = accountId,
                    PackageId = newPackageId,
                    StartDate = DateOnly.FromDateTime(now),
                    EndDate = newPackage.DurationDays.HasValue && newPackage.DurationDays.Value > 0
                        ? DateOnly.FromDateTime(now.AddDays(newPackage.DurationDays.Value))
                        : null,
                    InitialMockLimit = initialLimit,
                    MockInterviewUsed = 0,
                    IsActive = true,
                    CreatedAt = now
                };
                _unitOfWork.UserSubscriptions.AddUserSubscription(newUserSubscription);

                // 7. Trừ tiền và tạo transaction
                userAccount.Balance -= (int)newPackage.Price;

                var newTransaction = new Transaction
                {
                    SourceAccountId = accountId,
                    TargetAccountId = null,
                    Amount = (int)newPackage.Price,
                    TransactionType = TransactionType.Subscription,
                    Status = TransactionStatus.Completed,
                    Reason = existingActiveSub != null
                        ? $"Nâng cấp lên gói {newPackage.Name}"
                        : $"Thanh toán gói {newPackage.Name}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UserSubscription = newUserSubscription
                };
                await _unitOfWork.Transactions.AddAsync(newTransaction);
                await _unitOfWork.SaveChangesAsync();

                newTransaction.EnsureExternalTransactionCode();
                if (newTransaction.ExternalTransactionCode != null)
                    await _unitOfWork.SaveChangesAsync();

                await _systemNotificationService.CreateAndSendNotificationAsync(
                    accountId,
                    existingActiveSub != null
                        ? $"Bạn đã nâng cấp thành công lên gói {newPackage.Name}."
                        : $"Chúc mừng bạn đã đăng ký gói {newPackage.Name} thành công.",
                    null);

                await _unitOfWork.CommitTransactionAsync();

                // Phát event SignalR để cập nhật balance và AI Credit
                int remainingAiCredit = Math.Max(
                    newUserSubscription.InitialMockLimit - newUserSubscription.MockInterviewUsed,
                    0);
                var balanceEvent = new BalanceUpdatedEvent(accountId, userAccount.Balance, remainingAiCredit);
                await _mediator.Publish(balanceEvent);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Preview thông tin gói muốn mua: giá full, điều kiện rank.
        /// Không còn tính proration — luôn trả về full price.
        /// </summary>
        public async Task<UpgradePreviewResponse> GetUpgradePreviewAsync(int accountId, int newPackageId)
        {
            var newPackage = await _unitOfWork.SubscriptionPackages.GetSubscriptionPackageByIdAsync(newPackageId);
            if (newPackage == null)
                throw new NotFoundException("Không tìm thấy gói đăng ký.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var existingActiveSub = await _unitOfWork.UserSubscriptions
                .GetUserSubscriptions()
                .Include(s => s.Package)
                .Where(s => s.CandidateId == accountId
                         && s.IsActive
                         && (s.EndDate == null || s.EndDate >= today))
                .FirstOrDefaultAsync();

            if (existingActiveSub != null && newPackage.Rank <= existingActiveSub.Package.Rank)
                throw new BadRequestException(
                    $"Bạn đang sử dụng gói {existingActiveSub.Package.Name}. " +
                    $"Vui lòng chọn gói cao cấp hơn.");

            return new UpgradePreviewResponse
            {
                NewPackageName = newPackage.Name,
                NewPackagePrice = newPackage.Price,
                IsEligible = true,
                Message = existingActiveSub != null
                    ? $"Nâng cấp từ gói {existingActiveSub.Package.Name} lên gói {newPackage.Name}. Gói cũ sẽ bị hủy ngay lập tức."
                    : $"Đăng ký mới gói {newPackage.Name}."
            };
        }

        public async Task<UserSubscriptionHistoryResponse> GetUserSubscriptionHistoryAsync(int accountId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var allSubscriptions = await _unitOfWork.UserSubscriptions
                .GetUserSubscriptions()
                .Include(us => us.Package)
                .Include(us => us.Transaction)
                .Where(us => us.CandidateId == accountId)
                .OrderByDescending(us => us.StartDate)
                .ToListAsync();

            var response = new UserSubscriptionHistoryResponse();

            var currentSub = allSubscriptions
                .FirstOrDefault(us => us.IsActive &&
                                      us.Package != null &&
                                      (us.EndDate == null || us.EndDate >= today));

            if (currentSub?.Package != null)
            {
                DateTime startDateTime = currentSub.CreatedAt.DateTime;
                if (startDateTime == default || startDateTime == DateTime.MinValue)
                    startDateTime = currentSub.StartDate.ToDateTime(TimeOnly.FromDateTime(DateTime.UtcNow));

                DateTime? endDateTime = null;
                if (currentSub.Package.DurationDays.HasValue && currentSub.Package.DurationDays.Value > 0)
                    endDateTime = startDateTime.AddDays(currentSub.Package.DurationDays.Value);
                else if (currentSub.EndDate.HasValue)
                {
                    var daysDiff = currentSub.EndDate.Value.DayNumber - currentSub.StartDate.DayNumber;
                    if (daysDiff > 0) endDateTime = startDateTime.AddDays(daysDiff);
                }

                response.CurrentSubscription = new CurrentSubscriptionResponse
                {
                    SubscriptionId = currentSub.Id,
                    PackageName = currentSub.Package.Name,
                    StartDate = currentSub.StartDate,
                    EndDate = currentSub.EndDate,
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime,
                    InitialMockLimit = currentSub.InitialMockLimit,
                    MockInterviewUsed = currentSub.MockInterviewUsed,
                    IsActive = currentSub.IsActive
                };
            }

            response.History = allSubscriptions
                .Where(us => us.Package != null)
                .Select(us =>
                {
                    DateTime startDateTime = us.CreatedAt.DateTime;
                    if (startDateTime == default || startDateTime == DateTime.MinValue)
                    {
                        var transactionTime = us.Transaction?.CreatedAt.UtcDateTime ?? DateTime.UtcNow;
                        startDateTime = us.StartDate.ToDateTime(TimeOnly.FromDateTime(transactionTime));
                    }

                    DateTime? endDateTime = null;
                    if (us.Package!.DurationDays.HasValue && us.Package.DurationDays.Value > 0)
                        endDateTime = startDateTime.AddDays(us.Package.DurationDays.Value);
                    else if (us.EndDate.HasValue)
                    {
                        var daysDiff = us.EndDate.Value.DayNumber - us.StartDate.DayNumber;
                        if (daysDiff > 0) endDateTime = startDateTime.AddDays(daysDiff);
                    }

                    return new SubscriptionHistoryItem
                    {
                        SubscriptionId = us.Id,
                        PackageName = us.Package!.Name,
                        StartDate = us.StartDate,
                        EndDate = us.EndDate,
                        StartDateTime = startDateTime,
                        EndDateTime = endDateTime,
                        AmountPaid = us.Transaction?.Amount ?? 0,
                        TransactionDate = us.Transaction?.CreatedAt.DateTime,
                        IsActive = us.IsActive
                    };
                })
                .ToList();

            bool isUsingFreePackage = currentSub == null ||
                                      (currentSub.Package != null && currentSub.Package.Rank == 0);

            if (isUsingFreePackage)
            {
                var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
                if (account != null)
                {
                    var freeLimit = await _systemConfigService.GetFreeInterviewLimitAsync();
                    response.FreeInterviewInfo = new FreeInterviewInfo
                    {
                        FreeUsedMock = account.FreeUsedMock,
                        FreeLimit = freeLimit
                    };
                }
            }

            return response;
        }

        public async Task<CurrentPackageResponse> GetCurrentPackageAsync(int accountId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var activeSub = await _unitOfWork.UserSubscriptions
                .GetUserSubscriptions()
                .Include(us => us.Package)
                .Where(us => us.CandidateId == accountId
                          && us.IsActive
                          && (us.EndDate == null || us.EndDate >= today))
                .OrderByDescending(us => us.StartDate)
                .FirstOrDefaultAsync();

            SubscriptionPackage package = activeSub != null
                ? activeSub.Package
                : await _unitOfWork.SubscriptionPackages.GetLowestRankPackageAsync();

            return new CurrentPackageResponse
            {
                PackageId = package.Id,
                PackageName = package.Name,
                Rank = package.Rank,
                Price = package.Price
            };
        }

        public async Task<CurrentSubscriptionDetailResponse> GetCurrentSubscriptionDetailAsync(int accountId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var now = DateTime.UtcNow;

            var activeSub = await _unitOfWork.UserSubscriptions
                .GetUserSubscriptions()
                .Include(us => us.Package)
                .Where(us => us.CandidateId == accountId
                          && us.IsActive
                          && (us.EndDate == null || us.EndDate >= today))
                .OrderByDescending(us => us.StartDate)
                .FirstOrDefaultAsync();

            // Người dùng không có gói trả phí → trả về thông tin Free
            if (activeSub == null || activeSub.Package.Rank == 0)
            {
                var freePackage = await _unitOfWork.SubscriptionPackages.GetLowestRankPackageAsync();
                return new CurrentSubscriptionDetailResponse
                {
                    PackageName = freePackage?.Name ?? "Free",
                    Rank = 0,
                    StartedAt = null,
                    ExpiresAt = null,
                    RemainingDays = null,
                    IsExpired = false,
                    MockInterviewUsed = 0,
                    InitialMockLimit = 0,
                };
            }

            // Tính StartedAt từ CreatedAt
            DateTime startedAt = activeSub.CreatedAt.DateTime;
            if (startedAt == default || startedAt == DateTime.MinValue)
                startedAt = activeSub.StartDate.ToDateTime(TimeOnly.MinValue);

            // Tính ExpiresAt và RemainingDays
            DateTime? expiresAt = null;
            int? remainingDays = null;

            if (activeSub.EndDate.HasValue)
            {
                // Tính ExpiresAt dựa trên StartedAt + DurationDays để giữ chính xác giờ
                if (activeSub.Package.DurationDays.HasValue && activeSub.Package.DurationDays.Value > 0)
                    expiresAt = startedAt.AddDays(activeSub.Package.DurationDays.Value);
                else
                    expiresAt = activeSub.EndDate.Value.ToDateTime(TimeOnly.MaxValue);

                remainingDays = (int)Math.Max(0, Math.Ceiling((expiresAt.Value - now).TotalDays));
            }

            int remaining = activeSub.InitialMockLimit == int.MaxValue
                ? int.MaxValue
                : Math.Max(0, activeSub.InitialMockLimit - activeSub.MockInterviewUsed);

            return new CurrentSubscriptionDetailResponse
            {
                PackageName = activeSub.Package.Name,
                Rank = activeSub.Package.Rank,
                StartedAt = startedAt,
                ExpiresAt = expiresAt,
                RemainingDays = remainingDays,
                IsExpired = false,
                MockInterviewUsed = activeSub.MockInterviewUsed,
                InitialMockLimit = activeSub.InitialMockLimit,
            };
        }
    }
}
