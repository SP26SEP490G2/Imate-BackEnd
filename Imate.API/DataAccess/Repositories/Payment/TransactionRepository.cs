using Microsoft.EntityFrameworkCore;
using Imate.API.Business.Helper;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.DataAccess.Repositories.Payment
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ImateDbContext _context;

        public TransactionRepository(ImateDbContext context)
        {
            _context = context;
        }

        public async Task<Transaction> AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
            return transaction;
        }

        public async Task<Transaction> UpdateAsync(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
            return transaction;
        }

        public async Task<Transaction> GetByIdAsync(int transactionId)
        {
            return await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);
        }

        public async Task<int> GetTotalAmountAsync(int accountId, TransactionType transactionType, TransactionStatus status, bool isTarget)
        {
            var query = _context.Transactions.AsQueryable();
            query = query.Where(t => t.TransactionType == transactionType && t.Status == status);
            if (isTarget)
            {
                query = query.Where(t => t.TargetAccountId == accountId);
            }
            else
            {
                query = query.Where(t => t.SourceAccountId == accountId);
            }
            var totalAmount = await query.SumAsync(t => (int?)t.Amount) ?? 0;
            return totalAmount;
        }

        public async Task<int> GetSystemTotalAmountAsync(TransactionType transactionType, TransactionStatus status)
        {
            var totalAmount = await _context.Transactions
                .Where(t => t.TransactionType == transactionType && t.Status == status)
                .SumAsync(t => (int?)t.Amount) ?? 0;
            return totalAmount;
        }

        public async Task<PagedList<Transaction>> GetTransactionsForAccountAsync(int accountId, TransactionQueryParameters paginationParams)
        {
            IQueryable<Transaction> query = _context.Transactions
                .Where(t => t.SourceAccountId == accountId || t.TargetAccountId == accountId)
                .Include(t => t.WithdrawalDetail);

            // Bổ sung logic filter
            if (!string.IsNullOrEmpty(paginationParams.SearchTerm))
            {
                query = query.Where(t =>
                    (t.Reason != null && t.Reason.Contains(paginationParams.SearchTerm)) ||
                    (t.ExternalTransactionCode != null && t.ExternalTransactionCode.Contains(paginationParams.SearchTerm))
                );
            }

            // 1. THÊM LỌC THEO LOẠI
            if (!string.IsNullOrEmpty(paginationParams.Type))
            {
                if (Enum.TryParse<TransactionType>(paginationParams.Type, true, out var typeEnum))
                {
                    query = query.Where(t => t.TransactionType == typeEnum);
                }
            }

                // 2. THÊM LỌC THEO TRẠNG THÁI
            if (!string.IsNullOrEmpty(paginationParams.Status))
            {
                // Chuyển string (ví dụ "SUCCESS") thành Enum
                if (Enum.TryParse<TransactionStatus>(paginationParams.Status, true, out var statusEnum))
                {
                    query = query.Where(t => t.Status == statusEnum);
                }
            }

            query = query.OrderByDescending(t => t.CreatedAt);

            var pagedList = await PagedList<Transaction>.CreateAsync(
                query,
                paginationParams.PageNumber,
                paginationParams.PageSize
            );

            return pagedList;
        }

        public async Task<List<Transaction>> GetRecentTransactionsAsync(int accountId, int take = 5)
        {
            return await _context.Transactions
                .Where(t => t.SourceAccountId == accountId || t.TargetAccountId == accountId)
                .OrderByDescending(t => t.CreatedAt) 
                .Take(take) 
                .Include(t => t.WithdrawalDetail) 
                .ToListAsync();
        }

        public async Task<Transaction?> GetTransactionByBookingIdAsync(int bookingId)
        {
            return await _context.Transactions
                .Where(t => t.BookingId == bookingId && t.TransactionType == TransactionType.Deposit)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Transaction?> GetBookingTransactionAsync(int bookingId)
        {
            return await _context.Transactions
                .FirstOrDefaultAsync(t => t.BookingId == bookingId && t.TransactionType == TransactionType.BookingFee);
        }

        public async Task<PagedList<Transaction>> GetAllTransactionsAsync(TransactionQueryParameters paginationParams)
        {
            IQueryable<Transaction> query = _context.Transactions
                .Include(t => t.WithdrawalDetail)
                .Include(t => t.SourceAccount)
                .Include(t => t.TargetAccount);

            // Bổ sung logic filter
            if (!string.IsNullOrEmpty(paginationParams.SearchTerm))
            {
                var searchLower = paginationParams.SearchTerm.ToLower();
                query = query.Where(t =>
                    (t.Reason != null && t.Reason.ToLower().Contains(searchLower)) ||
                    (t.ExternalTransactionCode != null && t.ExternalTransactionCode.ToLower().Contains(searchLower)) ||
                    t.Id.ToString().Contains(searchLower) ||
                    (t.SourceAccount != null && t.SourceAccount.FullName != null && t.SourceAccount.FullName.ToLower().Contains(searchLower)) ||
                    (t.TargetAccount != null && t.TargetAccount.FullName != null && t.TargetAccount.FullName.ToLower().Contains(searchLower))
                );
            }

            // 1. THÊM LỌC THEO LOẠI
            if (!string.IsNullOrEmpty(paginationParams.Type))
            {
                if (Enum.TryParse<TransactionType>(paginationParams.Type, true, out var typeEnum))
                {
                    query = query.Where(t => t.TransactionType == typeEnum);
                }
            }

            // 2. THÊM LỌC THEO TRẠNG THÁI
            if (!string.IsNullOrEmpty(paginationParams.Status))
            {
                if (Enum.TryParse<TransactionStatus>(paginationParams.Status, true, out var statusEnum))
                {
                    query = query.Where(t => t.Status == statusEnum);
                }
            }

            query = query.OrderByDescending(t => t.CreatedAt);

            var pagedList = await PagedList<Transaction>.CreateAsync(
                query,
                paginationParams.PageNumber,
                paginationParams.PageSize
            );

            return pagedList;
        }

        public async Task<PagedList<Transaction>> GetReadyForPayoutBookingsAsync(TransactionQueryParameters paginationParams)
        {
            var now = DateTime.UtcNow;
            
            IQueryable<Transaction> query = _context.Transactions
                .Where(t => t.TransactionType == TransactionType.BookingFee
                         && t.Status == TransactionStatus.Escrow
                         && t.EscrowDeadline.HasValue
                         && t.EscrowDeadline.Value <= now)
                .Include(t => t.SourceAccount)
                .Include(t => t.TargetAccount)
                .Include(t => t.Booking);

            // Filter by search term
            if (!string.IsNullOrEmpty(paginationParams.SearchTerm))
            {
                var searchLower = paginationParams.SearchTerm.ToLower();
                query = query.Where(t =>
                    (t.Reason != null && t.Reason.ToLower().Contains(searchLower)) ||
                    (t.ExternalTransactionCode != null && t.ExternalTransactionCode.ToLower().Contains(searchLower)) ||
                    t.Id.ToString().Contains(searchLower) ||
                    (t.BookingId != null && t.BookingId.ToString().Contains(searchLower)) ||
                    (t.SourceAccount != null && t.SourceAccount.FullName != null && t.SourceAccount.FullName.ToLower().Contains(searchLower)) ||
                    (t.TargetAccount != null && t.TargetAccount.FullName != null && t.TargetAccount.FullName.ToLower().Contains(searchLower))
                );
            }

            query = query.OrderByDescending(t => t.EscrowDeadline);

            var pagedList = await PagedList<Transaction>.CreateAsync(
                query,
                paginationParams.PageNumber,
                paginationParams.PageSize
            );

            return pagedList;
        }

        public IQueryable<Transaction> GetAllTransactionsQueryable()
        {
            return _context.Transactions
                .Include(t => t.SourceAccount)
                .Include(t => t.TargetAccount)
                .Include(t => t.Booking)
                .AsNoTracking();
        }

        public async Task<List<Transaction>> GetPendingTimeoutTransactions(DateTime timeoutTime)
        {
            return await _context.Transactions
                .Where(t => t.Status == TransactionStatus.Pending
                         && t.CreatedAt <= timeoutTime
                         && t.TransactionType == TransactionType.Deposit)
                .ToListAsync();
        }
    }
}
