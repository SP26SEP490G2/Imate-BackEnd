

using Imate.API.Business.Helper;
using Imate.API.Models.Enums;
using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Payment
{
    public interface ITransactionRepository
    {
        Task<Transaction> AddAsync(Transaction transaction);
        Task<Transaction> UpdateAsync(Transaction transaction);
        Task<Transaction> GetByIdAsync(int transactionId);
        Task<int> GetTotalAmountAsync(int accountId, TransactionType transactionType, TransactionStatus status, bool isTarget);
        Task<int> GetSystemTotalAmountAsync(TransactionType transactionType, TransactionStatus status);
        Task<PagedList<Transaction>> GetTransactionsForAccountAsync(int accountId, TransactionQueryParameters paginationParams);
        Task<List<Transaction>> GetRecentTransactionsAsync(int accountId, int take = 5);
        Task<PagedList<Transaction>> GetAllTransactionsAsync(TransactionQueryParameters paginationParams);
        Task<Transaction?> GetTransactionByBookingIdAsync(int bookingId);
        Task<Transaction?> GetBookingTransactionAsync(int bookingId);
        Task<PagedList<Transaction>> GetReadyForPayoutBookingsAsync(TransactionQueryParameters paginationParams);
        IQueryable<Transaction> GetAllTransactionsQueryable();
        Task<List<Transaction>> GetPendingTimeoutTransactions(DateTime timeoutTime);
    }
}

