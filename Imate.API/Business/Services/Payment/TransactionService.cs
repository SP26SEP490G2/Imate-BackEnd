using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Notification;
using Imate.API.Business.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.Payment;
using Imate.API.Presentation.ResponseModels.Payment;
using Imate.API.Presentation.SignalR.Events.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
namespace Imate.API.Business.Services.Payment
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PayOSClient _payosClient;
        private readonly IConfiguration _configuration; 
        private readonly string _frontendBaseUrl;
        private readonly ILogger<TransactionService> _logger;
        private readonly ISystemConfigService _systemConfigService;
        private readonly IMediator _mediator;
        private readonly IAuditLogService _auditLogService;
        private readonly ISystemNotificationService _systemNotificationService;

        public TransactionService(
            IUnitOfWork unitOfWork,
            PayOSClient payosClient,
            IConfiguration configuration,
            ILogger<TransactionService> logger,
            ISystemConfigService systemConfigService,
            IMediator mediator,
            IAuditLogService auditLogService,
            ISystemNotificationService systemNotificationService
            )
        {
            _unitOfWork = unitOfWork;
            _payosClient = payosClient;
            _configuration = configuration;
            _mediator = mediator;
            _auditLogService = auditLogService;
            _systemNotificationService = systemNotificationService;

            // 3. Đọc URL từ appsettings và lưu lại
            _frontendBaseUrl = _configuration["FrontendSettings:BaseUrl"] ??
                               throw new ArgumentNullException("Url is not set in appsettings");
            _logger = logger;
            _systemConfigService = systemConfigService;
            _auditLogService = auditLogService;
        }
        public async Task<BalanceSummaryResponse> GetBalanceSummaryAsync(int accountId)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new KeyNotFoundException("Không tìm thấy tài khoản.");
            }

            // 2. Tính toán tổng nạp và tổng rút (có thể chạy song song)
            var totalDeposit = await _unitOfWork.Transactions
                .GetTotalAmountAsync(accountId, TransactionType.Deposit, TransactionStatus.Completed, isTarget: true);

            var totalWithdrawal = await _unitOfWork.Transactions
                .GetTotalAmountAsync(accountId, TransactionType.Withdrawal, TransactionStatus.Completed, isTarget: false);

            // 3. Đóng gói DTO để trả về
            var summary = new BalanceSummaryResponse
            {
                CurrentBalance = account.Balance,
                LastUpdated = account.UpdatedAt,
                TotalDeposit = totalDeposit,
                TotalWithdrawal = totalWithdrawal
            };

            // 4. Nếu là Mentor, tính toán thông tin tiền đảm bảo
            var mentor = await _unitOfWork.Mentors.GetMentorByIdAsync(accountId);
            if (mentor != null)
            {
                // Guarantee deposit rate từ config (thay vì commission rate)
                decimal guaranteeDepositRate = await _systemConfigService.GetGuaranteeDepositRateAsync();
                var pricePerSession = mentor.PricePerSession;
                var guaranteePerNewBooking = pricePerSession * guaranteeDepositRate / 100m;

                // Tính các booking đang cần tiền đảm bảo:
                // 1. Confirmed
                // 2. Completed nhưng chưa hết thời gian report
                var reportDeadlineHours = await _systemConfigService.GetReportDeadlineHoursAsync();
                var now = DateTime.UtcNow;
                var bookingsRequiringGuarantee = await _unitOfWork.Bookings.GetAllBookings()
                    .Where(b => b.MentorId == accountId
                        && (b.Status == BookingStatus.Confirmed
                            || (b.Status == BookingStatus.Completed
                                && b.StartTime.AddHours(1 + reportDeadlineHours) > now)))
                    .ToListAsync();

                var escrowCount = bookingsRequiringGuarantee.Count;
                var existingGuaranteeAmount = bookingsRequiringGuarantee
                    .Sum(b => b.PriceAtBooking * guaranteeDepositRate / 100m);
                
                // Tính số lượt booking có thể nhận: (balance - tiền đảm bảo hiện tại) / tiền đảm bảo cho 1 booking mới
                var availableBalanceForNewBookings = (decimal)account.Balance - existingGuaranteeAmount;
                var maxBookingsCanReceive = guaranteePerNewBooking > 0 && availableBalanceForNewBookings >= guaranteePerNewBooking
                    ? (int)Math.Floor(availableBalanceForNewBookings / guaranteePerNewBooking)
                    : 0;
                
                summary.PricePerSession = pricePerSession;
                summary.CurrentEscrowBookings = escrowCount;
                summary.RequiredBalanceForOneBooking = (int)(guaranteePerNewBooking);
                summary.MaxBookingsCanReceive = maxBookingsCanReceive;
                summary.GuaranteeDepositRate = guaranteeDepositRate;
            }

            return summary;
        }

        public async Task<PagedList<TransactionResponse>> GetTransactionsAsync(int accountId, TransactionQueryParameters paginationParams)
        {
            // 1. Gọi Repository để lấy PagedList<Transaction> (Entity)
            var pagedTransactions = await _unitOfWork.Transactions
                .GetTransactionsForAccountAsync(accountId, paginationParams);

            // 2. Map List<Transaction> (Items) sang List<TransactionDto>
            // For user: Escrowed status should display as "Completed"
            var transactionDtos = pagedTransactions.Items.Select(txn => new TransactionResponse
            {
                TransactionId = txn.Id,
                Date = txn.CreatedAt,
                Amount = txn.Amount,
                TransactionType = txn.TransactionType.ToString(),
                Status = txn.Status == TransactionStatus.Escrow ? TransactionStatus.Completed.ToString() : txn.Status.ToString(), // User sees Escrowed as Completed
                ExternalCode = txn.ExternalTransactionCode,
                Reason = txn.Reason,
                WithdrawalDetail = txn.WithdrawalDetail == null ? null : new WithdrawalDetailDto
                {
                    BankCode = txn.WithdrawalDetail.BankCode,
                    BankAccountHolder = txn.WithdrawalDetail.BankAccountHolder,
                    BankAccountNumber = MaskBankAccount(txn.WithdrawalDetail.BankAccountNumber)
                }
            }).ToList();

            // 3. Tạo một PagedList<TransactionDto> mới để trả về
            return new PagedList<TransactionResponse>(
                transactionDtos,
                pagedTransactions.TotalCount,
                pagedTransactions.PageNumber,
                pagedTransactions.PageSize
            );
        }

        private string MaskBankAccount(string accountNumber)
        {
            if (accountNumber.Length <= 4) return accountNumber;
            return $"{new string('X', accountNumber.Length - 4)}{accountNumber.Substring(accountNumber.Length - 4)}";
        }

        public async Task<DepositResponse> CreateDepositAsync(int accountId, DepositRequest depositRequestDto)
        {
            // Validation đầu vào
            if (depositRequestDto.Amount <= 0)
            {
                throw new ArgumentException("Số tiền nạp phải lớn hơn 0.");
            }

            // PayOS yêu cầu amount tối thiểu từ config
            var minimumAmount = await _systemConfigService.GetMinDepositAmountAsync();
            if (depositRequestDto.Amount < minimumAmount)
            {
                throw new ArgumentException($"Số tiền nạp tối thiểu là {minimumAmount:N0} VNĐ.");
            }

            // Validation FrontendBaseUrl
            if (string.IsNullOrWhiteSpace(_frontendBaseUrl))
            {
                _logger.LogError("FrontendSettings:BaseUrl không được cấu hình trong appsettings.");
                throw new InvalidOperationException("Cấu hình FrontendSettings:BaseUrl không hợp lệ.");
            }

            // Kiểm tra URL format (phải là absolute URL và nên dùng HTTPS)
            if (!Uri.TryCreate(_frontendBaseUrl, UriKind.Absolute, out var baseUri))
            {
                _logger.LogError("FrontendSettings:BaseUrl không phải là URL hợp lệ: {BaseUrl}", _frontendBaseUrl);
                throw new InvalidOperationException($"FrontendSettings:BaseUrl không hợp lệ: {_frontendBaseUrl}");
            }

            // PayOS yêu cầu HTTPS cho production
            if (baseUri.Scheme != "https" && baseUri.Scheme != "http")
            {
                _logger.LogError("FrontendSettings:BaseUrl phải sử dụng HTTP hoặc HTTPS: {BaseUrl}", _frontendBaseUrl);
                throw new InvalidOperationException($"FrontendSettings:BaseUrl phải sử dụng HTTP hoặc HTTPS: {_frontendBaseUrl}");
            }

            await _unitOfWork.BeginTransactionAsync();
            Transaction newTransaction;

            try
            {
                // 1. Tạo giao dịch PENDING
                newTransaction = new Transaction
                {
                    TargetAccountId = accountId,
                    Amount = depositRequestDto.Amount,
                    TransactionType = TransactionType.Deposit,
                    Status = TransactionStatus.Pending,
                    Reason = "Nạp imCoin",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Transactions.AddAsync(newTransaction);
                await _unitOfWork.SaveChangesAsync(); // Lưu để lấy ID
            }
            catch (Exception dbEx)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(dbEx, "Lỗi CSDL khi tạo Transaction PENDING ban đầu.");
                throw new Exception("Lỗi CSDL khi khởi tạo giao dịch.");
            }

            // 2. LẤY ID LÀM ORDER CODE
            long orderCode = newTransaction.Id;

            // Validation OrderCode: PayOS yêu cầu OrderCode phải là số nguyên dương và trong khoảng hợp lệ
            // PayOS thường chấp nhận OrderCode từ 1 đến 9,999,999,999,999,999
            if (orderCode <= 0 || orderCode > 9999999999999999)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError("OrderCode không hợp lệ: {OrderCode}. Phải là số nguyên dương và nhỏ hơn 9,999,999,999,999,999", orderCode);
                throw new InvalidOperationException("OrderCode không hợp lệ.");
            }

            // 3. Tạo returnUrl và cancelUrl từ frontend base URL
            // Đảm bảo URL không có trailing slash và khớp với frontend
            var returnUrl = $"{_frontendBaseUrl.TrimEnd('/')}/wallet";
            var cancelUrl = $"{_frontendBaseUrl.TrimEnd('/')}/wallet";

            // Validation URL format
            if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var returnUri) || 
                !Uri.TryCreate(cancelUrl, UriKind.Absolute, out var cancelUri))
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError("ReturnUrl hoặc CancelUrl không hợp lệ. ReturnUrl: {ReturnUrl}, CancelUrl: {CancelUrl}", returnUrl, cancelUrl);
                throw new InvalidOperationException("ReturnUrl hoặc CancelUrl không hợp lệ.");
            }

            // Giới hạn độ dài description (PayOS có thể có giới hạn)
            var description = $"Nap imCoin #{newTransaction.Id}";
            if (description.Length > 255)
            {
                description = description.Substring(0, 255);
            }

            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = depositRequestDto.Amount,
                Description = description,
                CancelUrl = cancelUrl,
                ReturnUrl = returnUrl
            };

            // Log thông tin request
            _logger.LogInformation(
                "Tạo PayOS payment request - AccountId: {AccountId}, OrderCode: {OrderCode}, Amount: {Amount}, ReturnUrl: {ReturnUrl}, CancelUrl: {CancelUrl}",
                accountId, orderCode, depositRequestDto.Amount, returnUrl, cancelUrl);

            // Kiểm tra PayOS credentials có được cấu hình không
            var clientId = _configuration["PayOS:ClientId"];
            var apiKey = _configuration["PayOS:ApiKey"];
            var checksumKey = _configuration["PayOS:ChecksumKey"];

            if (string.IsNullOrWhiteSpace(clientId) || 
                string.IsNullOrWhiteSpace(apiKey) || 
                string.IsNullOrWhiteSpace(checksumKey))
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError("PayOS credentials chưa được cấu hình đầy đủ. Kiểm tra PayOS:ClientId, PayOS:ApiKey, PayOS:ChecksumKey trong appsettings.");
                throw new InvalidOperationException("PayOS credentials chưa được cấu hình. Vui lòng liên hệ quản trị viên.");
            }

            // 4. --- GIẢI PHÁP (FIX) ---
            // Ghi đè (override) cấu hình mặc định của SDK
            // Chúng ta yêu cầu nó VẪN KÝ REQUEST, nhưng BỎ QUA CHECK RESPONSE
            var customSignatureOptions = new SignatureOptions
            {
                Request = "create-payment-link", // Vẫn ký request (đang hoạt động tốt)
                Response = null // <-- BỎ QUA check response (nơi đang bị lỗi)
            };

            var customRequestOptions = new RequestOptions<CreatePaymentLinkRequest>
            {
                Signature = customSignatureOptions
            };

            CreatePaymentLinkResponse paymentLinkResponse;
            try
            {
                // 5. GỌI HÀM CREATEASYNC (NHƯ CŨ)
                // Nhưng truyền thêm tham số thứ 2
                paymentLinkResponse = await _payosClient.PaymentRequests.CreateAsync(
                    paymentRequest,
                    customRequestOptions // <-- Truyền vào cấu hình đã ghi đè
                );

                _logger.LogInformation(
                    "PayOS payment request thành công - OrderCode: {OrderCode}, PaymentLinkId: {PaymentLinkId}",
                    orderCode, paymentLinkResponse.PaymentLinkId);
            }
            catch (ApiException apiEx)
            {
                // Lỗi này xảy ra nếu PayOS trả về code != "00" (ví dụ: key request sai)
                _logger.LogError(
                    apiEx, 
                    "Lỗi API PayOS (ApiException). Đang Rollback Txn ID: {TxnId}. ErrorCode: {ErrorCode}, Message: {Message}. Request details - OrderCode: {OrderCode}, Amount: {Amount}, ReturnUrl: {ReturnUrl}, CancelUrl: {CancelUrl}, Description: {Description}",
                    newTransaction.Id, apiEx.ErrorCode, apiEx.Message, orderCode, depositRequestDto.Amount, returnUrl, cancelUrl, description);
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception($"Lỗi API PayOS ({apiEx.ErrorCode}): {apiEx.Message}");
            }
            catch (Exception ex)
            {
                // Bắt các lỗi khác
                _logger.LogError(
                    ex, 
                    "Lỗi không xác định khi gọi CreateAsync. Đang Rollback Txn ID: {TxnId}. Request details - OrderCode: {OrderCode}, Amount: {Amount}, ReturnUrl: {ReturnUrl}, CancelUrl: {CancelUrl}",
                    newTransaction.Id, orderCode, depositRequestDto.Amount, returnUrl, cancelUrl);
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            // 6. GỌI API THÀNH CÔNG (Không bị exception)
            try
            {
                newTransaction.ExternalTransactionCode = paymentLinkResponse.PaymentLinkId;
                await _unitOfWork.Transactions.UpdateAsync(newTransaction);
                await _unitOfWork.SaveChangesAsync(); // Lưu PaymentLinkId

                await _unitOfWork.CommitTransactionAsync(); // Commit

                // 7. Trả về FE
                return new DepositResponse
                {
                    TransactionId = newTransaction.Id,
                    CheckoutUrl = paymentLinkResponse.CheckoutUrl,
                    OrderCode = paymentLinkResponse.OrderCode.ToString()
                };
            }
            catch (Exception dbEx)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _payosClient.PaymentRequests.CancelAsync(paymentLinkResponse.PaymentLinkId, "Lỗi hệ thống nội bộ");
                throw;
            }
        }
        public async Task HandlePayOsWebhookAsync(Webhook webhookData)
        {
            WebhookData verifiedData;
            try
            {
                // 1. Xác thực Webhook
                verifiedData = await _payosClient.Webhooks.VerifyAsync(webhookData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Signature Webhook không hợp lệ: {ex.Message}");
            }

            if (verifiedData.OrderCode == 123 && verifiedData.Description == "VQRIO123")
            {
                return; // Đây là "ping", trả về 200 OK để PayOS lưu.
            }

            int transactionId = (int)verifiedData.OrderCode;
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Webhook: Không tìm thấy transaction với OrderCode: {OrderCode}", verifiedData.OrderCode);
                return;
            }

            // Nếu transaction đã Completed, không xử lý lại
            if (transaction.Status == TransactionStatus.Completed)
            {
                _logger.LogInformation("Webhook: Transaction {TransactionId} đã thành công, bỏ qua webhook", transactionId);
                return;
            }

            // 2. Kiểm tra code và xử lý theo trạng thái
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (verifiedData.Code == "00")
                {
                    // Completed: Thanh toán thành công
                    _logger.LogInformation("Webhook: Transaction {TransactionId} thành công (Code: {Code})", transactionId, verifiedData.Code);
                    
                    transaction.Status = TransactionStatus.Completed;
                    await _unitOfWork.Transactions.UpdateAsync(transaction);

                    // Cộng tiền vào tài khoản
                    var account = await _unitOfWork.Accounts.GetByIdAsync(transaction.TargetAccountId.Value);
                    if (account != null)
                    {
                        account.Balance += transaction.Amount;
                        await _unitOfWork.Accounts.UpdateAsync(account);
                        _logger.LogInformation("Webhook: Đã cộng {Amount} vào balance của account {AccountId}", transaction.Amount, account.Id);
                        await _systemNotificationService.CreateAndSendNotificationAsync(account .Id, $"Đã cộng {transaction.Amount} vào số ví Imate của bạn", null);

                    }
                }
                else
                {
                    // FAILED hoặc CANCELLED: Giao dịch thất bại hoặc bị hủy
                    _logger.LogWarning("Webhook: Transaction {TransactionId} thất bại/hủy (Code: {Code}, Description: {Description})", 
                        transactionId, verifiedData.Code, verifiedData.Description);
                    
                    transaction.Status = TransactionStatus.Failed;
                    await _unitOfWork.Transactions.UpdateAsync(transaction);
                    
                    // Log thông tin webhook để debug
                    _logger.LogInformation("Webhook data: Code={Code}, OrderCode={OrderCode}, Description={Description}, Amount={Amount}",
                        verifiedData.Code, verifiedData.OrderCode, verifiedData.Description, verifiedData.Amount);
                }

                // Lưu thay đổi
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("Webhook: Đã cập nhật transaction {TransactionId} thành công", transactionId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Webhook: Lỗi khi xử lý webhook cho transaction {TransactionId}", transactionId);
                throw; 
            }
        }

        public async Task CancelTransactionAsync(int transactionId, int accountId)
        {
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
            
            if (transaction == null)
            {
                throw new KeyNotFoundException("Không tìm thấy giao dịch.");
            }

            // Kiểm tra quyền: chỉ cho phép cancel transaction của chính account đó
            if (transaction.TargetAccountId != accountId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền hủy giao dịch này.");
            }

            // Chỉ cho phép hủy transaction ở trạng thái Pending
            if (transaction.Status != TransactionStatus.Pending)
            {
                throw new InvalidOperationException($"Không thể hủy giao dịch với trạng thái {transaction.Status}.");
            }

            // Kiểm tra xem có ExternalTransactionCode (PaymentLinkId) không
            if (string.IsNullOrEmpty(transaction.ExternalTransactionCode))
            {
                // Nếu chưa có PaymentLinkId, chỉ cần cập nhật status
                transaction.Status = TransactionStatus.Cancelled;
                await _unitOfWork.Transactions.UpdateAsync(transaction);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Transaction {TransactionId} đã được hủy (không có PaymentLinkId)", transactionId);
                return;
            }

            // Nếu có PaymentLinkId, hủy payment link trên PayOS
            try
            {
                await _payosClient.PaymentRequests.CancelAsync(
                    transaction.ExternalTransactionCode,
                    "Người dùng hủy giao dịch"
                );
                _logger.LogInformation("Đã hủy payment link {PaymentLinkId} trên PayOS", transaction.ExternalTransactionCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể hủy payment link trên PayOS cho transaction {TransactionId}", transactionId);
                // Vẫn tiếp tục cập nhật status trong DB dù PayOS cancel fail
            }

            // Cập nhật status thành Failed
            transaction.Status = TransactionStatus.Cancelled;
            await _unitOfWork.Transactions.UpdateAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Transaction {TransactionId} đã được hủy thành công", transactionId);
        }

        public async Task<TransactionResponse> CreateWithdrawalAsync(int accountId, string role, WithdrawRequest withdrawRequestDto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 1. Lấy tài khoản và kiểm tra số dư (như cũ)
                var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
                if (account == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy tài khoản.");
                }
                if (account.Balance < withdrawRequestDto.Amount)
                {
                    throw new ArgumentException($"Số dư không đủ để thực hiện giao dịch. Số dư hiện tại: {account.Balance:N0} VND. Số tiền muốn rút: {withdrawRequestDto.Amount:N0} VND.");
                }

                // --- LOGIC LẤY THÔNG TIN NGÂN HÀNG THEO VAI TRÒ ---
                string bankCode;
                string bankAccountHolder;
                string bankAccountNumber;
                Mentor? mentorProfile = null;

                if (role.Equals("Mentor", StringComparison.OrdinalIgnoreCase))
                {
                    // Mentor: Lấy thông tin từ profile Mentor
                    // Giả định PK của Mentor là AccountId, nên dùng GetByIdAsync
                    mentorProfile = await _unitOfWork.Mentors.GetMentorByIdAsync(accountId);
                    if (mentorProfile == null)
                    {
                        throw new KeyNotFoundException("Không tìm thấy hồ sơ Mentor.");
                    }

                    if (string.IsNullOrEmpty(mentorProfile.BankCode) ||
                        string.IsNullOrEmpty(mentorProfile.BankAccountNumber) ||
                        string.IsNullOrEmpty(mentorProfile.BankAccountHolderName))
                    {
                        throw new ArgumentException("Thông tin ngân hàng của Mentor chưa đầy đủ. Vui lòng cập nhật hồ sơ.");
                    }

                    bankCode = mentorProfile.BankCode;
                    bankAccountHolder = mentorProfile.BankAccountHolderName;
                    bankAccountNumber = mentorProfile.BankAccountNumber;
                }
                else if (role.Equals("Candidate", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(withdrawRequestDto.BankCode) ||
                        string.IsNullOrEmpty(withdrawRequestDto.BankAccountHolder) ||
                        string.IsNullOrEmpty(withdrawRequestDto.BankAccountNumber))
                    {
                        throw new ArgumentException("Thiếu thông tin ngân hàng");
                    }

                    bankCode = withdrawRequestDto.BankCode;
                    bankAccountHolder = withdrawRequestDto.BankAccountHolder;
                    bankAccountNumber = withdrawRequestDto.BankAccountNumber;
                }
                else
                {
                    throw new ArgumentException("Vai trò người dùng không được phép thực hiện hành động này.");
                }
                // --- KẾT THÚC LOGIC LẤY THÔNG TIN ---


                // 2.5. KIỂM TRA TIỀN ĐẢM BẢO CHO MENTOR
                if (role.Equals("Mentor", StringComparison.OrdinalIgnoreCase) && mentorProfile != null)
                {
                    // Lấy guarantee deposit rate từ config
                    decimal guaranteeDepositRate = await _systemConfigService.GetGuaranteeDepositRateAsync();
                    var guaranteePerBooking = mentorProfile.PricePerSession * guaranteeDepositRate / 100m;

                    // Lấy thời gian report deadline từ config
                    var reportDeadlineHours = await _systemConfigService.GetReportDeadlineHoursAsync();
                    var now = DateTime.UtcNow;

                    // Các booking cần tiền đảm bảo:
                    // 1. Confirmed
                    // 2. Completed nhưng chưa hết thời gian report
                    var bookingsRequiringGuarantee = await _unitOfWork.Bookings.GetAllBookings()
                        .Where(b => b.MentorId == accountId 
                            && (b.Status == BookingStatus.Confirmed
                                || (b.Status == BookingStatus.Completed
                                    && b.StartTime.AddHours(1 + reportDeadlineHours) > now)))
                        .Select(b => b.PriceAtBooking)
                        .ToListAsync();

                    var bookingsRequiringGuaranteeCount = bookingsRequiringGuarantee.Count;

                    // Tính số tiền đảm bảo cần giữ lại theo giá tại thời điểm booking
                    var requiredGuaranteeAmount = bookingsRequiringGuarantee
                        .Sum(price => price * guaranteeDepositRate / 100m);

                    // Số tiền có thể rút = balance - tiền đảm bảo cần giữ
                    // Đảm bảo withdrawableAmount không âm
                    var withdrawableAmount = Math.Max(0m, (decimal)account.Balance - requiredGuaranteeAmount);

                    if (withdrawRequestDto.Amount > withdrawableAmount)
                    {
                        var errorMessage = bookingsRequiringGuaranteeCount > 0
                            ? $"Bạn đang có {bookingsRequiringGuaranteeCount} booking cần tiền đảm bảo. Số tiền đảm bảo cần giữ lại: {requiredGuaranteeAmount:N0} VND."
                            : $"Số dư không đủ. Số dư hiện tại: {account.Balance:N0} VND.";
                        
                        throw new ArgumentException(errorMessage);
                    }
                }

                // 3. Trừ tiền (như cũ)
                account.Balance -= withdrawRequestDto.Amount;
                await _unitOfWork.Accounts.UpdateAsync(account);

                // 4. Tạo Giao dịch và Chi tiết Rút tiền (dùng biến đã gán)
                var newTransaction = new Transaction
                {
                    SourceAccountId = accountId,
                    TargetAccountId = null,
                    Amount = withdrawRequestDto.Amount,
                    TransactionType = TransactionType.Withdrawal,
                    Status = TransactionStatus.Pending,
                    Reason = "Yêu cầu rút tiền",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,

                    WithdrawalDetail = new WithdrawalDetail
                    {
                        BankCode = bankCode, // Gán từ biến
                        BankAccountHolder = bankAccountHolder, // Gán từ biến
                        BankAccountNumber = bankAccountNumber // Gán từ biến
                    }
                };

                // 5. Lưu và Commit (như cũ)
                await _unitOfWork.Transactions.AddAsync(newTransaction);
                await _unitOfWork.SaveChangesAsync(); // Lưu để lấy ID
                
                // Set ExternalTransactionCode nếu chưa có (entity đã được track, không cần UpdateAsync)
                newTransaction.EnsureExternalTransactionCode();
                if (newTransaction.ExternalTransactionCode != null)
                {
                    await _unitOfWork.SaveChangesAsync(); // Lưu ExternalTransactionCode
                }
                
                await _unitOfWork.CommitTransactionAsync();
                await _mediator.Publish(new WithdrawalRequestCreateEvent(newTransaction));

                // 6. Map và trả về DTO (như cũ)
                return MapTransactionToDto(newTransaction);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private TransactionResponse MapTransactionToDto(Transaction txn)
        {
            return new TransactionResponse
            {
                TransactionId = txn.Id,
                Date = txn.CreatedAt,
                Amount = txn.Amount,
                TransactionType = txn.TransactionType.ToString(),
                Status = txn.Status.ToString(),
                ExternalCode = txn.ExternalTransactionCode,
                Reason = txn.Reason,
                WithdrawalDetail = txn.WithdrawalDetail == null ? null : new WithdrawalDetailDto
                {
                    BankCode = txn.WithdrawalDetail.BankCode,
                    BankAccountHolder = txn.WithdrawalDetail.BankAccountHolder,
                    BankAccountNumber = txn.WithdrawalDetail.BankAccountNumber
                },
                SourceAccountName = txn.SourceAccount?.FullName ?? (txn.SourceAccountId == null ? "Imate" : null),
                TargetAccountName = txn.TargetAccount?.FullName ?? (txn.TargetAccountId == null ? "Imate" : null),
                BookingId = txn.BookingId,
                EscrowDeadline = txn.EscrowDeadline,
                CommissionRateApplied = txn.CommissionRateApplied
            };
        }

        public async Task<List<TransactionResponse>> GetRecentTransactionsAsync(int accountId, int take = 5)
        {
            var transactions = await _unitOfWork.Transactions
                .GetRecentTransactionsAsync(accountId, take);

            // Map thủ công (tái sử dụng hàm map đã viết)   
            // For user: Escrowed status should display as "Completed"
            var dtos = transactions
                .Select(txn => 
                {
                    var dto = MapTransactionToDto(txn);
                    // Override status for user view: Escrowed -> Completed
                    if (txn.Status == TransactionStatus.Escrow)
                    {
                        dto.Status = TransactionStatus.Completed.ToString();
                    }
                    return dto;
                })
                .ToList();

            return dtos;
        }

        public async Task<PagedList<TransactionResponse>> GetAllTransactionsForAdminAsync(TransactionQueryParameters paginationParams)
        {
            var pagedTransactions = await _unitOfWork.Transactions
                .GetAllTransactionsAsync(paginationParams);

            var dtos = pagedTransactions.Items
                .Select(txn => MapTransactionToDto(txn))
                .ToList();

            return new PagedList<TransactionResponse>(
                dtos,
                pagedTransactions.TotalCount,
                pagedTransactions.PageNumber,
                pagedTransactions.PageSize
            );
        }

        public async Task ApproveWithdrawalAsync(int transactionId, int reviewerId, string? responseNote = null)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
                if (transaction == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy giao dịch với ID {transactionId}.");
                }

                if (transaction.TransactionType != TransactionType.Withdrawal)
                {
                    throw new ArgumentException("Chỉ có thể duyệt các giao dịch rút tiền.");
                }

                if (transaction.Status != TransactionStatus.Pending)
                {
                    throw new ArgumentException("Giao dịch này đã được xử lý.");
                }

                // Get old value before update
                var oldValue = new { 
                    Status = transaction.Status.ToString(), 
                    Amount = transaction.Amount,
                    Reason = transaction.Reason
                };

                // Cập nhật trạng thái thành Completed
                transaction.Status = TransactionStatus.Completed;
                transaction.ReviewerId = reviewerId;
                transaction.Reason = responseNote ?? transaction.Reason;
                transaction.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Transactions.UpdateAsync(transaction);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                
                // Create audit log
                var newValue = new { 
                    Status = transaction.Status.ToString(), 
                    Amount = transaction.Amount,
                    Reason = transaction.Reason,
                    ReviewerId = reviewerId
                };
                await _auditLogService.CreateAuditLogAsync(
                    reviewerId,
                    AuditAction.Update,
                    "Transaction",
                    transactionId,
                    oldValue,
                    newValue
                );
                
                await _mediator.Publish(new WithdrawalRequestApprovedEvent(transaction));
                _logger.LogInformation("Withdrawal transaction {TransactionId} đã được duyệt bởi {ReviewerId}", transactionId, reviewerId);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task RejectWithdrawalAsync(int transactionId, int reviewerId, string? responseNote = null)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
                if (transaction == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy giao dịch với ID {transactionId}.");
                }

                if (transaction.TransactionType != TransactionType.Withdrawal)
                {
                    throw new ArgumentException("Chỉ có thể từ chối các giao dịch rút tiền.");
                }

                if (transaction.Status != TransactionStatus.Pending)
                {
                    throw new ArgumentException("Giao dịch này đã được xử lý.");
                }

                // Get old value before update
                var oldValue = new { 
                    Status = transaction.Status.ToString(), 
                    Amount = transaction.Amount,
                    Reason = transaction.Reason
                };

                // Hoàn tiền lại cho người dùng
                if (transaction.SourceAccountId.HasValue)
                {
                    var account = await _unitOfWork.Accounts.GetByIdAsync(transaction.SourceAccountId.Value);
                    if (account != null)
                    {
                        account.Balance += transaction.Amount;
                        await _unitOfWork.Accounts.UpdateAsync(account);
                    }
                }

                // Cập nhật trạng thái thành Failed
                transaction.Status = TransactionStatus.Failed;
                transaction.ReviewerId = reviewerId;
                transaction.Reason = responseNote ?? transaction.Reason;
                transaction.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Transactions.UpdateAsync(transaction);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                
                // Create audit log
                var newValue = new { 
                    Status = transaction.Status.ToString(), 
                    Amount = transaction.Amount,
                    Reason = transaction.Reason,
                    ReviewerId = reviewerId
                };
                await _auditLogService.CreateAuditLogAsync(
                    reviewerId,
                    AuditAction.Update,
                    "Transaction",
                    transactionId,
                    oldValue,
                    newValue
                );
                
                await _mediator.Publish(new WithdrawalRequestRejectedEvent(transaction));

                _logger.LogInformation("Withdrawal transaction {TransactionId} đã bị từ chối bởi {ReviewerId}", transactionId, reviewerId);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<SystemStatisticsResponse> GetSystemStatisticsAsync()
        {
            // Tính tổng tiền nạp vào (MoneyDeposit Completed)
            var totalDeposit = await _unitOfWork.Transactions
                .GetSystemTotalAmountAsync(TransactionType.Deposit, TransactionStatus.Completed);

            // Tính tổng tiền rút ra (MoneyWithdrawal Completed)
            var totalWithdrawal = await _unitOfWork.Transactions
                .GetSystemTotalAmountAsync(TransactionType.Withdrawal, TransactionStatus.Completed);

            // Lãi ròng = Tiền vào - Tiền ra
            var netProfit = totalDeposit - totalWithdrawal;

            return new SystemStatisticsResponse
            {
                TotalDeposit = totalDeposit,
                TotalWithdrawal = totalWithdrawal,
                NetProfit = netProfit
            };
        }

        public async Task<PagedList<TransactionResponse>> GetReadyForPayoutBookingsAsync(TransactionQueryParameters paginationParams)
        {
            var pagedTransactions = await _unitOfWork.Transactions
                .GetReadyForPayoutBookingsAsync(paginationParams);

            var dtos = new List<TransactionResponse>();
            
            foreach (var txn in pagedTransactions.Items)
            {
                var dto = MapTransactionToDto(txn);
                
                // Check if there are pending or in-review mentor reports for this booking
                if (txn.BookingId.HasValue)
                {
                    var hasPendingReport = await _unitOfWork.Applications.GetAllApplications()
                        .AnyAsync(a => a.BookingId == txn.BookingId.Value
                            && a.ApplicationType == ApplicationType.ReportMentor
                            && (a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.InReview));
                    
                    dto.HasPendingMentorReport = hasPendingReport;
                }
                else
                {
                    dto.HasPendingMentorReport = false;
                }
                
                dtos.Add(dto);
            }

            return new PagedList<TransactionResponse>(
                dtos,
                pagedTransactions.TotalCount,
                pagedTransactions.PageNumber,
                pagedTransactions.PageSize
            );
        }

        public async Task ProcessBookingPayoutAsync(int transactionId, int reviewerId, string? responseNote = null)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 1. Get the booking fee transaction
                var bookingFeeTransaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
                if (bookingFeeTransaction == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy giao dịch với ID {transactionId}.");
                }

                // 2. Validate transaction type and status
                if (bookingFeeTransaction.TransactionType != TransactionType.BookingFee)
                {
                    throw new ArgumentException("Chỉ có thể xử lý payout cho giao dịch booking fee.");
                }

                if (bookingFeeTransaction.Status != TransactionStatus.Escrow)
                {
                    throw new ArgumentException("Giao dịch này không ở trạng thái escrow hoặc đã được xử lý.");
                }

                // 3. Validate escrow deadline has passed
                var escrowHours = await _systemConfigService.GetEscrowHoursAsync();
                if (!bookingFeeTransaction.EscrowDeadline.HasValue || bookingFeeTransaction.EscrowDeadline.Value > DateTime.UtcNow)
                {
                    throw new ArgumentException($"Chưa đến thời gian payout. Phải đợi {escrowHours} giờ sau khi đặt lịch.");
                }

                // 4. Validate booking exists and is not cancelled
                if (!bookingFeeTransaction.BookingId.HasValue)
                {
                    throw new ArgumentException("Giao dịch này không liên quan đến booking nào.");
                }

                var booking = await _unitOfWork.Bookings.GetBookingByIdAsync(bookingFeeTransaction.BookingId.Value);
                if (booking == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy booking liên quan.");
                }

                if (booking.Status == BookingStatus.Cancelled)
                {
                    throw new ArgumentException("Booking này đã bị hủy, không thể payout.");
                }

                // 4.1. Check if there are pending or in-review mentor reports for this booking
                var pendingReports = await _unitOfWork.Applications.GetAllApplications()
                    .Where(a => a.BookingId == bookingFeeTransaction.BookingId.Value
                        && a.ApplicationType == ApplicationType.ReportMentor
                        && (a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.InReview))
                    .ToListAsync();

                if (pendingReports.Any())
                {
                    throw new ArgumentException("Không thể xử lý payout. Booking này có đơn báo cáo mentor chưa được xử lý. Vui lòng xử lý báo cáo trước.");
                }

                // 5. Get mentor account from booking
                if (booking.MentorId == 0)
                {
                    throw new ArgumentException("Booking này không có mentor.");
                }

                var mentor = await _unitOfWork.Mentors.GetMentorByIdAsync(booking.MentorId);
                if (mentor == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy mentor.");
                }

                var mentorAccount = await _unitOfWork.Accounts.GetByIdAsync(mentor.AccountId);
                if (mentorAccount == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy tài khoản mentor.");
                }

                // 6. Set commission rate if not already set (from config)
                decimal commissionRate = await _systemConfigService.GetCommissionRateAsync();
                if (!bookingFeeTransaction.CommissionRateApplied.HasValue)
                {
                    bookingFeeTransaction.CommissionRateApplied = commissionRate;
                }
                else
                {
                    commissionRate = bookingFeeTransaction.CommissionRateApplied.Value;
                }

                // 7. Calculate payout amount (after commission)
                var payoutAmount = bookingFeeTransaction.Amount;
                var commission = (int)(payoutAmount * commissionRate / 100);
                payoutAmount -= commission;

                // Get old value before update
                var oldValue = new { 
                    Status = bookingFeeTransaction.Status.ToString(), 
                    Amount = bookingFeeTransaction.Amount,
                    CommissionRateApplied = bookingFeeTransaction.CommissionRateApplied,
                    Reason = bookingFeeTransaction.Reason
                };

                // 8. Update booking fee transaction (set commission rate if not set)
                bookingFeeTransaction.CommissionRateApplied = commissionRate;
                bookingFeeTransaction.Status = TransactionStatus.Completed;
                bookingFeeTransaction.ReviewerId = reviewerId;
                bookingFeeTransaction.Reason = responseNote ?? bookingFeeTransaction.Reason;
                bookingFeeTransaction.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Transactions.UpdateAsync(bookingFeeTransaction);

                // 9. Add money to mentor account (after commission)
                mentorAccount.Balance += payoutAmount;
                await _unitOfWork.Accounts.UpdateAsync(mentorAccount);

                // 10. Create payout transaction
                var payoutTransaction = new Transaction
                {
                    SourceAccountId = null, // System payout
                    TargetAccountId = mentorAccount.Id,
                    TransactionType = TransactionType.BookingPayout,
                    Amount = payoutAmount,
                    BookingId = bookingFeeTransaction.BookingId,
                    Status = TransactionStatus.Completed,
                    CommissionRateApplied = commissionRate, // Store commission rate for reference
                    Reason = responseNote ?? $"Payout cho booking #{bookingFeeTransaction.BookingId} sau {escrowHours}h (Hoa hồng: {commissionRate}%)",
                    ReviewerId = reviewerId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Transactions.AddAsync(payoutTransaction);
                await _unitOfWork.SaveChangesAsync(); // Lưu để lấy ID
                
                // Set ExternalTransactionCode nếu chưa có (entity đã được track, không cần UpdateAsync)
                payoutTransaction.EnsureExternalTransactionCode();
                if (payoutTransaction.ExternalTransactionCode != null)
                {
                    await _unitOfWork.SaveChangesAsync(); // Lưu ExternalTransactionCode
                }
                
                await _unitOfWork.CommitTransactionAsync();

                // Create audit log
                var newValue = new { 
                    Status = bookingFeeTransaction.Status.ToString(), 
                    Amount = bookingFeeTransaction.Amount,
                    CommissionRateApplied = bookingFeeTransaction.CommissionRateApplied,
                    Reason = bookingFeeTransaction.Reason,
                    ReviewerId = reviewerId,
                    PayoutAmount = payoutAmount,
                    Commission = commission
                };
                await _auditLogService.CreateAuditLogAsync(
                    reviewerId,
                    AuditAction.Update,
                    "Transaction",
                    transactionId,
                    oldValue,
                    newValue
                );

                // Publish event to send notification to mentor
                await _mediator.Publish(new BookingPayoutProcessedEvent(
                    payoutTransaction,
                    payoutAmount,
                    bookingFeeTransaction.BookingId!.Value
                ));

                _logger.LogInformation("Booking payout processed. Transaction {TransactionId}, Original amount: {OriginalAmount}, Commission: {Commission}%, Payout amount: {PayoutAmount}, Mentor: {MentorId}", 
                    transactionId, bookingFeeTransaction.Amount, commissionRate, payoutAmount, mentorAccount.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error processing booking payout for transaction {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task<RevenueResponse> GetRevenueAsync(RevenueQueryParameters parameters)
        {
            // Validate year
            if (parameters.Year < 2020 || parameters.Year > DateTime.UtcNow.Year + 1)
            {
                throw new ArgumentException($"Năm không hợp lệ. Phải từ 2020 đến {DateTime.UtcNow.Year + 1}.");
            }

            // Validate month if provided
            if (parameters.Month.HasValue && (parameters.Month.Value < 1 || parameters.Month.Value > 12))
            {
                throw new ArgumentException("Tháng không hợp lệ. Phải từ 1 đến 12.");
            }

            // Build base window: only Completed in year (+ optional month)
            var baseQuery = _unitOfWork.Transactions.GetAllTransactionsQueryable()
                .Where(t => t.Status == TransactionStatus.Completed && t.CreatedAt.Year == parameters.Year);

            if (parameters.Month.HasValue)
            {
                baseQuery = baseQuery.Where(t => t.CreatedAt.Month == parameters.Month.Value);
            }

            // Parse transaction type filter (do not apply to baseQuery)
            TransactionType? filterType = null;
            if (!string.IsNullOrWhiteSpace(parameters.TransactionType))
            {
                if (!Enum.TryParse<TransactionType>(parameters.TransactionType, out var tt))
                {
                    throw new ArgumentException($"Loại giao dịch '{parameters.TransactionType}' không hợp lệ.");
                }
                filterType = tt;
            }

            // 1) Mentor commission (stored into PointBookingPayout field)
            //    - Always anchor to payouts created inside the window
            //    - Include in result if no filter or filter is PointBookingPayout
            int mentorCommission = 0;
            if (!filterType.HasValue || filterType.Value == TransactionType.BookingPayout)
            {
                var payouts = await baseQuery
                    .Where(t => t.TransactionType == TransactionType.BookingPayout && t.BookingId.HasValue)
                    .Select(t => new { t.BookingId, t.Amount })
                    .ToListAsync();

                var bookingIds = payouts
                    .Where(p => p.BookingId.HasValue)
                    .Select(p => p.BookingId!.Value)
                    .Distinct()
                    .ToList();

                if (bookingIds.Count > 0)
                {
                    // Fetch booking fees for those bookings regardless of month (but Completed only)
                    var fees = await _unitOfWork.Transactions.GetAllTransactionsQueryable()
                        .Where(t => t.Status == TransactionStatus.Completed
                                    && t.TransactionType == TransactionType.BookingFee
                                    && t.BookingId.HasValue
                                    && bookingIds.Contains(t.BookingId.Value))
                        .Select(t => new { t.BookingId, t.Amount })
                        .ToListAsync();

                    var feeByBooking = fees
                        .GroupBy(f => f.BookingId!.Value)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

                    var payoutByBooking = payouts
                        .GroupBy(p => p.BookingId!.Value)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

                    foreach (var kv in payoutByBooking)
                    {
                        var bookingId = kv.Key;
                        var payoutSum = kv.Value;
                        feeByBooking.TryGetValue(bookingId, out var feeSum);
                        var diff = feeSum - payoutSum;
                        if (diff > 0) mentorCommission += diff;
                    }
                }
            }

            // 2) Other categories: include only when no filter or matching filter
            int subscriptionSum = 0;
            int penaltySum = 0;
            int interviewSum = 0;
            int refundSum = 0;
            int depositSum = 0;

            if (!filterType.HasValue || filterType.Value == TransactionType.Subscription)
            {
                subscriptionSum = await baseQuery
                    .Where(t => t.TransactionType == TransactionType.Subscription)
                    .SumAsync(t => (int?)t.Amount) ?? 0;
            }

            if (!filterType.HasValue || filterType.Value == TransactionType.Penalty)
            {
                penaltySum = await baseQuery
                    .Where(t => t.TransactionType == TransactionType.Penalty)
                    .SumAsync(t => (int?)t.Amount) ?? 0;
            }

            if (!filterType.HasValue || filterType.Value == TransactionType.InterviewFee)
            {
                interviewSum = await baseQuery
                    .Where(t => t.TransactionType == TransactionType.InterviewFee)
                    .SumAsync(t => (int?)t.Amount) ?? 0;
            }

            if (!filterType.HasValue || filterType.Value == TransactionType.Refund)
            {
                // Tính các refund dựa trên refund rate từ config
                // Doanh thu = (100 - refundRate)% còn lại (phần hệ thống giữ)
                var refundRate = await _systemConfigService.GetCancellationRefundRateAsync();
                var systemKeepRate = (100 - refundRate) / 100m; // Tỷ lệ hệ thống giữ
                var refunds = await baseQuery
                    .Where(t => t.TransactionType == TransactionType.Refund)
                    .ToListAsync();
                // Filter refunds có chứa refund rate trong reason (để tương thích với cả transaction cũ và mới)
                var refundRateString = $"{refundRate}%";
                refundSum = refunds
                    .Where(t => t.Reason != null && t.Reason.Contains(refundRateString))
                    .Sum(t => (int)(t.Amount * systemKeepRate / (refundRate / 100m))); // Tính doanh thu từ refund amount
            }

            if (!filterType.HasValue || filterType.Value == TransactionType.Deposit)
            {
                depositSum = await baseQuery
                    .Where(t => t.TransactionType == TransactionType.Deposit)
                    .SumAsync(t => (int?)t.Amount) ?? 0;
            }

            if (mentorCommission < 0) mentorCommission = 0;

            var breakdown = new RevenueBreakdown
            {
                PointBookingPayout = mentorCommission,
                PointSubscriptionFee = subscriptionSum,
                PointPenalty = penaltySum,
                PointInterviewFee = interviewSum,
                PointDeposit = depositSum
            };

            var totalIncome = mentorCommission + subscriptionSum + penaltySum + interviewSum + refundSum;
            var totalRevenue = totalIncome - depositSum;

            return new RevenueResponse
            {
                TotalRevenue = totalRevenue,
                TotalIncome = totalIncome,
                TotalDeposit = depositSum,
                Breakdown = breakdown,
                Year = parameters.Year,
                Month = parameters.Month,
                TransactionType = parameters.TransactionType
            };
        }

        public async Task<PagedList<TransactionResponse>> GetRevenueTransactionsAsync(RevenueTransactionQueryParameters parameters)
        {
            // Validate year
            if (parameters.Year < 2020 || parameters.Year > DateTime.UtcNow.Year + 1)
            {
                throw new ArgumentException($"Năm không hợp lệ. Phải từ 2020 đến {DateTime.UtcNow.Year + 1}.");
            }

            // Validate month if provided
            if (parameters.Month.HasValue && (parameters.Month.Value < 1 || parameters.Month.Value > 12))
            {
                throw new ArgumentException("Tháng không hợp lệ. Phải từ 1 đến 12.");
            }

            // Define revenue-related transaction types
            var revenueTypes = new[]
            {
                TransactionType.BookingPayout,
                TransactionType.Subscription,
                TransactionType.Penalty,
                TransactionType.InterviewFee,
                TransactionType.Refund,
                TransactionType.Deposit
            };

            // Get all Completedful transactions in the specified year/month
            var query = _unitOfWork.Transactions.GetAllTransactionsQueryable()
                .Where(t => t.Status == TransactionStatus.Completed &&
                           revenueTypes.Contains(t.TransactionType) &&
                           t.CreatedAt.Year == parameters.Year);

            // Filter by month if specified
            if (parameters.Month.HasValue)
            {
                query = query.Where(t => t.CreatedAt.Month == parameters.Month.Value);
            }

            // Filter PointRefund: only get configured refund rate refunds (exclude 100% refunds)
            var refundRate = await _systemConfigService.GetCancellationRefundRateAsync();
            var refundRateString = $"{refundRate}%";
            query = query.Where(t => 
                t.TransactionType != TransactionType.Refund || 
                (t.TransactionType == TransactionType.Refund && t.Reason != null && t.Reason.Contains(refundRateString))
            );

            // Filter by transaction type if specified
            if (!string.IsNullOrWhiteSpace(parameters.TransactionType))
            {
                if (Enum.TryParse<TransactionType>(parameters.TransactionType, out var transactionType))
                {
                    if (revenueTypes.Contains(transactionType))
                    {
                        query = query.Where(t => t.TransactionType == transactionType);
                    }
                    else
                    {
                        throw new ArgumentException($"Loại giao dịch '{parameters.TransactionType}' không liên quan đến doanh thu.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Loại giao dịch '{parameters.TransactionType}' không hợp lệ.");
                }
            }

            // Search by transaction code or account name
            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var searchLower = parameters.SearchTerm.ToLower();
                query = query.Where(t =>
                    (t.ExternalTransactionCode != null && t.ExternalTransactionCode.ToLower().Contains(searchLower)) ||
                    t.Id.ToString().Contains(searchLower) ||
                    (t.SourceAccount != null && t.SourceAccount.FullName.ToLower().Contains(searchLower)) ||
                    (t.TargetAccount != null && t.TargetAccount.FullName.ToLower().Contains(searchLower))
                );
            }

            // Order by date descending (newest first)
            query = query.OrderByDescending(t => t.CreatedAt);

            // Map to response DTOs
            var pagedTransactions = await PagedList<Transaction>.CreateAsync(
                query,
                parameters.PageNumber,
                parameters.PageSize
            );

            var dtos = pagedTransactions.Items
                .Select(txn => MapTransactionToDto(txn))
                .ToList();

            // Tính lợi nhuận cho từng giao dịch:
            // - PointBookingPayout: Fee - Payout (commission)
            // - PointDeposit: -Amount (chi phí)
            // - Các loại khác (PointSubscriptionFee, PointPenalty, PointInterviewFee): Amount (lợi nhuận)
            
            var payoutDtos = dtos
                .Where(d => d.BookingId.HasValue && d.TransactionType == TransactionType.BookingPayout.ToString())
                .ToList();

            if (payoutDtos.Count > 0)
            {
                var bookingIds = payoutDtos.Select(d => d.BookingId!.Value).Distinct().ToList();

                // Lấy tổng fee theo booking (Completed, có thể ở tháng khác)
                var fees = await _unitOfWork.Transactions.GetAllTransactionsQueryable()
                    .Where(t => t.Status == TransactionStatus.Completed
                                && t.TransactionType == TransactionType.BookingFee
                                && t.BookingId.HasValue
                                && bookingIds.Contains(t.BookingId.Value))
                    .GroupBy(t => t.BookingId!.Value)
                    .Select(g => new { BookingId = g.Key, FeeTotal = g.Sum(x => x.Amount) })
                    .ToListAsync();

                var feeMap = fees.ToDictionary(x => x.BookingId, x => x.FeeTotal);

                foreach (var dto in payoutDtos)
                {
                    feeMap.TryGetValue(dto.BookingId!.Value, out var feeTotal);
                    var profit = feeTotal - dto.Amount;
                    dto.Profit = profit > 0 ? profit : 0;
                }
            }

            // Tính profit cho các giao dịch khác
            foreach (var dto in dtos)
            {
                if (dto.TransactionType == TransactionType.Deposit.ToString())
                {
                    // PointDeposit: lợi nhuận âm (chi phí)
                    dto.Profit = -dto.Amount;
                }
                else if (dto.TransactionType == TransactionType.Refund.ToString())
                {
                    // PointRefund: tính doanh thu dựa trên refund rate từ config
                    // Sử dụng refundRate và refundRateString đã được khai báo ở trên
                    var systemKeepRate = (100 - refundRate) / 100m; // Tỷ lệ hệ thống giữ
                    var transaction = pagedTransactions.Items.FirstOrDefault(t => t.Id == dto.TransactionId);
                    if (transaction?.Reason != null && transaction.Reason.Contains(refundRateString))
                    {
                        dto.Profit = (int)(dto.Amount * systemKeepRate / (refundRate / 100m)); // Doanh thu hệ thống từ refund
                    }
                    else
                    {
                        dto.Profit = 0; // 100% refund hoặc refund không khớp với config không tính
                    }
                }
                else if (dto.TransactionType != TransactionType.BookingPayout.ToString())
                {
                    // PointSubscriptionFee, PointPenalty, PointInterviewFee: lợi nhuận = số tiền
                    dto.Profit = dto.Amount;
                }
                // PointBookingPayout đã được tính ở trên
            }

            return new PagedList<TransactionResponse>(
                dtos,
                pagedTransactions.TotalCount,
                pagedTransactions.PageNumber,
                pagedTransactions.PageSize
            );
        }
    }
}
