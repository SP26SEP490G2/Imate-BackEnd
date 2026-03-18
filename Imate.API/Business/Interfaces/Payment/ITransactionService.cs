using PayOS.Models.Webhooks;
using Imate.API.Business.Helper;
using Imate.API.Presentation.RequestModels.Payment;
using Imate.API.Presentation.ResponseModels.Payment;

namespace Imate.API.Business.Interfaces.Payment
{
    public interface ITransactionService
    {
        Task<BalanceSummaryResponse> GetBalanceSummaryAsync(int accountId);
        Task<PagedList<TransactionResponse>> GetTransactionsAsync(int accountId, TransactionQueryParameters paginationParams);
        Task<DepositResponse> CreateDepositAsync(int accountId, DepositRequest depositRequestDto);
        Task<TransactionResponse> CreateWithdrawalAsync(int accountId, string role, WithdrawRequest withdrawRequestDto);
        Task<List<TransactionResponse>> GetRecentTransactionsAsync(int accountId, int take = 5);
        Task HandlePayOsWebhookAsync(Webhook webhookData);
        Task CancelTransactionAsync(int transactionId, int accountId);
        Task<PagedList<TransactionResponse>> GetAllTransactionsForAdminAsync(TransactionQueryParameters paginationParams);
        Task ApproveWithdrawalAsync(int transactionId, int reviewerId, string? responseNote = null);
        Task RejectWithdrawalAsync(int transactionId, int reviewerId, string? responseNote = null);
        Task<SystemStatisticsResponse> GetSystemStatisticsAsync();
        Task<PagedList<TransactionResponse>> GetReadyForPayoutBookingsAsync(TransactionQueryParameters paginationParams);
        Task ProcessBookingPayoutAsync(int transactionId, int reviewerId, string? responseNote = null);
        Task<RevenueResponse> GetRevenueAsync(RevenueQueryParameters parameters);
        Task<PagedList<TransactionResponse>> GetRevenueTransactionsAsync(RevenueTransactionQueryParameters parameters);
    }
}
