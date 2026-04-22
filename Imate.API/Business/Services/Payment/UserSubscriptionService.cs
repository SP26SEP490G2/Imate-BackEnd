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
using System.Security.Principal;

namespace Imate.API.Business.Services.Payment
{
    public class UserSubscriptionService : IUserSubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISystemConfigService _systemConfigService;
        private readonly ISystemNotificationService _systemNotificationService;


        public UserSubscriptionService(IUnitOfWork unitOfWork, ISystemConfigService systemConfigService, ISystemNotificationService systemNotificationService)
        {
            _unitOfWork = unitOfWork;
            _systemConfigService = systemConfigService;
            _systemNotificationService = systemNotificationService;
        }

        public async Task ActivateNewSubscriptionAsync(int accountId, int newPackageId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // === 1. LẤY THÔNG TIN GÓI MỚI VÀ TÀI KHOẢN USER ===

                var newPackage = await _unitOfWork.SubscriptionPackages.GetSubscriptionPackageByIdAsync(newPackageId);
                if (newPackage == null)
                {
                    throw new NotFoundException("Không tìm thấy gói đăng ký mới.");
                }

                var userAccount = await _unitOfWork.Accounts.GetByIdAsync(accountId);
                if (userAccount == null)
                {
                    throw new NotFoundException("Không tìm thấy tài khoản người dùng.");
                }

                // === 2. LOGIC NÂNG CẤP: TÍNH TOÁN GIÁ TRỊ CÒN LẠI (PRORATION) ===

                decimal amountToCharge = newPackage.Price; // Mặc định là giá đầy đủ
                decimal remainingValue = 0;
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                // 2a. Tìm gói đang hoạt động (active) CŨ của user
                var existingActiveSub = await _unitOfWork.UserSubscriptions
                    .GetUserSubscriptions()
                    .Where(s => s.CandidateId == accountId && s.IsActive && (s.EndDate == null || s.EndDate >= today))
                    .FirstOrDefaultAsync();

                if (existingActiveSub != null)
                {
                    // 2b. Lấy thông tin gói CŨ
                    var oldPackage = await _unitOfWork.SubscriptionPackages.GetSubscriptionPackageByIdAsync(existingActiveSub.PackageId);
                    if (oldPackage == null)
                    {
                        // Lỗi lạ, nhưng vẫn nên xử lý: coi như không có gói cũ
                        existingActiveSub.IsActive = false; // Vô hiệu hóa nó luôn
                    }
                    else
                    {
                        // 2c. KIỂM TRA RANK: Đảm bảo đây là NÂNG CẤP
                        if (newPackage.Rank <= oldPackage.Rank)
                        {
                            throw new BadRequestException("Không thể hạ cấp hoặc mua lại gói tương đương. Vui lòng chọn gói cao cấp hơn.");
                        }

                        if (existingActiveSub.EndDate > today && oldPackage.DurationDays > 0 && oldPackage.Price > 0)
                        {
                            // Tính giá mỗi ngày của gói cũ
                            decimal oldPricePerDay = oldPackage.Price / (decimal)oldPackage.DurationDays;

                            // Tính số ngày còn lại
                            int remainingDays = existingActiveSub.EndDate.Value.DayNumber - today.DayNumber;
                            remainingValue = Math.Round(oldPricePerDay * remainingDays, 0); // Làm tròn
                        }
                    }
                }

                // 2e. TÍNH TOÁN SỐ TIỀN PHẢI TRẢ CUỐI CÙNG
                amountToCharge = newPackage.Price - remainingValue;
                if (amountToCharge < 0)
                {
                    amountToCharge = 0; // Đảm bảo không bao giờ âm (ví dụ: gói KM)
                }

                // === 3. KIỂM TRA SỐ DƯ ===

                if (userAccount.Balance < amountToCharge)
                {
                    throw new BadRequestException("Số dư không đủ để thanh toán.");
                }

                // === 5. NGHIỆP VỤ USER SUBSCRIPTION (Tạo mới / Vô hiệu hóa cũ) ===

                // 5a. Vô hiệu hóa tất cả các gói cũ (dù chỉ có 1)
                // (Ta query lại để đảm bảo change tracking không bị lỗi)
                var allActiveSubs = await _unitOfWork.UserSubscriptions
                    .GetUserSubscriptions()
                    .Where(s => s.CandidateId == accountId && s.IsActive)
                    .ToListAsync();

                foreach (var oldSub in allActiveSubs)
                {
                    oldSub.IsActive = false;
                }

                // 5b. Xác định MockLimit cho gói MỚI từ package configuration
                // Logic: Chỉ sử dụng TotalInterviewLimit nếu có. 
                // DailyInterviewLimit được kiểm tra riêng mỗi ngày, không nhân với DurationDays
                int initialLimit;
                if (newPackage.TotalInterviewLimit.HasValue)
                {
                    // Nếu có tổng giới hạn, sử dụng nó
                    initialLimit = newPackage.TotalInterviewLimit.Value;
                }
                else
                {
                    // Không có tổng giới hạn - sử dụng số rất lớn
                    // DailyInterviewLimit sẽ được kiểm tra riêng mỗi ngày
                    initialLimit = int.MaxValue;
                }

                // 5c. Tạo đối tượng UserSubscription MỚI
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
                    CreatedAt = now // Đảm bảo CreatedAt được set đúng
                };
                _unitOfWork.UserSubscriptions.AddUserSubscription(newUserSubscription);

                // === 6. NGHIỆP VỤ THANH TOÁN (Tạo Giao dịch) ===

                // 6a. Cập nhật số dư
                userAccount.Balance -= (int)amountToCharge;

                // 6b. Tạo đối tượng Giao dịch (Transaction)
                var newTransaction = new Transaction
                {
                    SourceAccountId = accountId,
                    TargetAccountId = null,
                    Amount = (int)amountToCharge,
                    TransactionType = TransactionType.Subscription,
                    Status = TransactionStatus.Completed,
                    Reason = $"Thanh toán phí gói {newPackage.Name}" + (remainingValue > 0 ? $" (nâng cấp, đã trừ {remainingValue}đ)" : ""),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UserSubscription = newUserSubscription // Liên kết giao dịch với gói mới
                };
                await _unitOfWork.Transactions.AddAsync(newTransaction);
                await _unitOfWork.SaveChangesAsync(); // Lưu để lấy ID
                
                // Set ExternalTransactionCode nếu chưa có (entity đã được track, không cần UpdateAsync)
                newTransaction.EnsureExternalTransactionCode();
                if (newTransaction.ExternalTransactionCode != null)
                {
                    await _unitOfWork.SaveChangesAsync(); // Lưu ExternalTransactionCode
                }
                await _systemNotificationService.CreateAndSendNotificationAsync(accountId, "Chúc mừng bạn đã mua gói thành công", null);

                // === 7. LƯU TẤT CẢ VÀ COMMIT ===
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<UpgradePreviewResponse> GetUpgradePreviewAsync(int accountId, int newPackageId)
        {
            // === 1. LẤY THÔNG TIN GÓI MỚI ===
            var newPackage = await _unitOfWork.SubscriptionPackages.GetSubscriptionPackageByIdAsync(newPackageId);
            if (newPackage == null)
            {
                throw new NotFoundException("Không tìm thấy gói đăng ký mới.");
            }

            var response = new UpgradePreviewResponse
            {
                NewPackageName = newPackage.Name,
                NewPackagePrice = newPackage.Price,
                AmountToCharge = newPackage.Price, // Mặc định là giá đầy đủ
                IsEligible = true,
                HasActiveSubscription = false
            };
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            // === 2. LOGIC TÍNH TOÁN (Giống hệt hàm Activate...) ===
            var existingActiveSub = await _unitOfWork.UserSubscriptions
                .GetUserSubscriptions()
                .Where(s => s.CandidateId == accountId
                     && s.IsActive
                     && (s.EndDate == null || s.EndDate >= today))
                .FirstOrDefaultAsync();

            if (existingActiveSub != null)
            {
                response.HasActiveSubscription = true;
                var oldPackage = await _unitOfWork.SubscriptionPackages.GetSubscriptionPackageByIdAsync(existingActiveSub.PackageId);

                if (oldPackage != null)
                {
                    response.OldPackageName = oldPackage.Name;

                    // 2c. KIỂM TRA RANK
                    if (newPackage.Rank <= oldPackage.Rank)
                    {
                        // Ném Exception, middleware của bạn sẽ bắt lỗi này
                        throw new BadRequestException("Không thể hạ cấp hoặc mua lại gói tương đương. Vui lòng chọn gói cao cấp hơn hoặc hủy gói hiện tại để chọn gói cấp thấp hơn.");
                    }

                    // 2d. TÍNH GIÁ TRỊ CÒN LẠI
                    if (existingActiveSub.EndDate.HasValue && existingActiveSub.EndDate.Value > today && oldPackage.DurationDays > 0 && oldPackage.Price > 0)
                    {
                        decimal oldPricePerDay = oldPackage.Price / (decimal)oldPackage.DurationDays;
                        int remainingDays = existingActiveSub.EndDate.Value.DayNumber - today.DayNumber;
                        response.RemainingValue = Math.Round(oldPricePerDay * remainingDays, 0);
                    }
                }
            }

            // 2e. TÍNH TOÁN SỐ TIỀN PHẢI TRẢ
            response.AmountToCharge = newPackage.Price - response.RemainingValue;
            if (response.AmountToCharge < 0)
            {
                response.AmountToCharge = 0;
            }

            response.Message = response.HasActiveSubscription ? "Chi phí nâng cấp" : "Chi phí đăng ký mới";
            return response;
        }

        public async Task CancelSubscriptionAsync(int accountId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);


                var currentPackage = await GetCurrentPackageAsync(accountId);
                var lowestPackage = await _unitOfWork.SubscriptionPackages.GetLowestRankPackageAsync();

                if (currentPackage.Rank == lowestPackage.Rank)
                {
                    throw new BadRequestException($"Bạn đang sử dụng {currentPackage.PackageName}, không thể hủy.");
                }
                var activeSub = await _unitOfWork.UserSubscriptions
                    .GetUserSubscriptions()
                    .Include(s => s.Package)
                    .Where(s => s.CandidateId == accountId
                         && s.IsActive
                         && (s.EndDate == null || s.EndDate >= today))
                    .FirstOrDefaultAsync();

                // 3. Tính toán số tiền hoàn lại (Prorated Refund)
                decimal refundAmount = 0;
                if (activeSub.EndDate.HasValue && activeSub.EndDate.Value > today && activeSub.Package.DurationDays > 0 && activeSub.Package.Price > 0)
                {
                    decimal pricePerDay = activeSub.Package.Price / (decimal)activeSub.Package.DurationDays;
                    int remainingDays = activeSub.EndDate.Value.DayNumber - today.DayNumber;
                    refundAmount = Math.Round(pricePerDay * remainingDays, 0);
                }

                // 4. Lấy tài khoản User và Hệ thống
                var userAccount = await _unitOfWork.Accounts.GetByIdAsync(accountId);

                if (userAccount == null)
                {
                    throw new NotFoundException("Lỗi hệ thống: Không tìm thấy tài khoản.");
                }

                // 5. Xử lý hoàn tiền (nếu có)
                if (refundAmount > 0)
                {
                    userAccount.Balance += (int)refundAmount;

                    // Tạo giao dịch hoàn tiền
                    var refundTransaction = new Transaction
                    {
                        SourceAccountId = null,
                        TargetAccountId = accountId,
                        Amount = (int)refundAmount,
                        TransactionType = TransactionType.Refund, // Bạn cần thêm Enum này
                        Status = TransactionStatus.Completed,
                        Reason = $"Hoàn tiền hủy gói {activeSub.Package.Name}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    await _unitOfWork.Transactions.AddAsync(refundTransaction);
                    await _unitOfWork.SaveChangesAsync(); // Lưu để lấy ID
                    
                    // Set ExternalTransactionCode nếu chưa có (entity đã được track, không cần UpdateAsync)
                    refundTransaction.EnsureExternalTransactionCode();
                    if (refundTransaction.ExternalTransactionCode != null)
                    {
                        await _unitOfWork.SaveChangesAsync(); // Lưu ExternalTransactionCode
                    }
                }

                // 6. Vô hiệu hóa gói cũ
                activeSub.IsActive = false;

                // 7. KÍCH HOẠT GÓI THƯỜNG (Rất quan trọng)
                // (Nếu không làm bước này, user sẽ không có gói active nào
                // và không thể dùng 3 lượt free)               
                await _systemNotificationService.CreateAndSendNotificationAsync(accountId, "Bạn vừa hủy gói đăng ký", null);

                // 8. Lưu tất cả thay đổi
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<CancelPreviewResponse> GetCancelPreviewAsync(int accountId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // 1. Tìm gói active và thông tin gói
            var activeSub = await _unitOfWork.UserSubscriptions
                .GetUserSubscriptions()
                .Include(s => s.Package)
                .Where(s => s.CandidateId == accountId && s.IsActive)
                .FirstOrDefaultAsync();

            // 2. Kiểm tra
            if (activeSub == null || activeSub.Package.Name == "Gói Thường")
            {
                throw new BadRequestException("Bạn không có gói trả phí nào đang hoạt động để hủy.");
            }

            // 3. Tính toán
            decimal refundAmount = 0;
            int remainingDays = 0;

            if (activeSub.EndDate.HasValue && activeSub.EndDate.Value > today && activeSub.Package.DurationDays > 0 && activeSub.Package.Price > 0)
            {
                decimal pricePerDay = activeSub.Package.Price / (decimal)activeSub.Package.DurationDays;
                remainingDays = activeSub.EndDate.Value.DayNumber - today.DayNumber;
                refundAmount = Math.Round(pricePerDay * remainingDays, 0);
            }

            // 4. Trả về kết quả (không lưu, không commit)
            return new CancelPreviewResponse
            {
                PackageToCancel = activeSub.Package.Name,
                RemainingDays = remainingDays,
                RefundAmount = refundAmount
            };
        }
        public async Task<UserSubscriptionHistoryResponse> GetUserSubscriptionHistoryAsync(int accountId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
            // Lấy tất cả subscriptions của user, bao gồm Transaction
            var allSubscriptions = await _unitOfWork.UserSubscriptions
                .GetUserSubscriptions()
                .Include(us => us.Package)
                .Include(us => us.Transaction)
                .Where(us => us.CandidateId == accountId)
                .OrderByDescending(us => us.StartDate)
                .ToListAsync();

            var response = new UserSubscriptionHistoryResponse();

            // Tìm gói hiện tại (active và chưa hết hạn)
            var currentSub = allSubscriptions
                .FirstOrDefault(us => us.IsActive && 
                                      us.Package != null && 
                                      (us.EndDate == null || us.EndDate >= today));

            if (currentSub != null && currentSub.Package != null)
            {
                // Tính thời gian bắt đầu từ CreatedAt (thời gian đăng ký thực tế)
                // Nếu CreatedAt chưa được set (default), sử dụng StartDate với giờ hiện tại
                DateTime startDateTime = currentSub.CreatedAt.DateTime;
                if (startDateTime == default(DateTime) || startDateTime == DateTime.MinValue)
                {
                    // Fallback: sử dụng StartDate với giờ hiện tại
                    startDateTime = currentSub.StartDate.ToDateTime(TimeOnly.FromDateTime(DateTime.UtcNow));
                }
                
                // Tính thời gian kết thúc: StartDateTime + DurationDays
                DateTime? endDateTime = null;
                if (currentSub.Package.DurationDays.HasValue && currentSub.Package.DurationDays.Value > 0)
                {
                    endDateTime = startDateTime.AddDays(currentSub.Package.DurationDays.Value);
                }
                else if (currentSub.EndDate.HasValue)
                {
                    // Nếu không có DurationDays nhưng có EndDate, tính từ StartDateTime đến EndDate
                    var daysDiff = currentSub.EndDate.Value.DayNumber - currentSub.StartDate.DayNumber;
                    if (daysDiff > 0)
                    {
                        endDateTime = startDateTime.AddDays(daysDiff);
                    }
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

            // Lịch sử tất cả các gói (bao gồm cả gói hiện tại)
            response.History = allSubscriptions
                .Where(us => us.Package != null)
                .Select(us => 
                {
                    // Tính thời gian bắt đầu từ CreatedAt (thời gian đăng ký thực tế)
                    // Nếu CreatedAt chưa được set (default), sử dụng StartDate với giờ hiện tại
                    DateTime startDateTime = us.CreatedAt.DateTime;
                    if (startDateTime == default(DateTime) || startDateTime == DateTime.MinValue)
                    {
                        // Fallback: sử dụng StartDate với giờ từ Transaction hoặc giờ hiện tại
                        var transactionTime = us.Transaction?.CreatedAt.UtcDateTime ?? DateTime.UtcNow;
                        startDateTime = us.StartDate.ToDateTime(TimeOnly.FromDateTime(transactionTime));
                    }
                    
                    // Tính thời gian kết thúc: StartDateTime + DurationDays
                    DateTime? endDateTime = null;
                    if (us.Package!.DurationDays.HasValue && us.Package.DurationDays.Value > 0)
                    {
                        endDateTime = startDateTime.AddDays(us.Package.DurationDays.Value);
                    }
                    else if (us.EndDate.HasValue)
                    {
                        // Nếu không có DurationDays nhưng có EndDate, tính từ StartDateTime đến EndDate
                        var daysDiff = us.EndDate.Value.DayNumber - us.StartDate.DayNumber;
                        if (daysDiff > 0)
                        {
                            endDateTime = startDateTime.AddDays(daysDiff);
                        }
                    }
                    
                    return new SubscriptionHistoryItem
                    {
                        SubscriptionId = us.Id,
                        PackageName = us.Package!.Name,
                        StartDate = us.StartDate,
                        EndDate = us.EndDate,
                        StartDateTime = startDateTime,
                        EndDateTime = endDateTime,
                        AmountPaid = us.Transaction != null ? us.Transaction.Amount : 0,
                        TransactionDate = us.Transaction?.CreatedAt.DateTime,
                        IsActive = us.IsActive
                    };
                })
                .ToList();

            // Kiểm tra nếu người dùng đang dùng gói thường hoặc không có gói -> thêm thông tin lượt phỏng vấn miễn phí
            bool isUsingRegularPackage = currentSub == null || 
                                         (currentSub.Package != null && (currentSub.Package.Id == 1 || currentSub.Package.Name == "Gói Thường"));
            
            if (isUsingRegularPackage)
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

            SubscriptionPackage package;

            if (activeSub != null)
            {
                package = activeSub.Package;
            }
            else
            {
                package = await _unitOfWork.SubscriptionPackages.GetLowestRankPackageAsync();
            }

            return new CurrentPackageResponse
            {
                PackageId = package.Id,
                PackageName = package.Name,
                Rank = package.Rank,
                Price = package.Price
            };
        }
    }
}
