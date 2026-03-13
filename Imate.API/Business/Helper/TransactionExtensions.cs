using Imate.API.Models.Entities;

namespace Imate.API.Business.Helper
{
    /// <summary>
    /// Extension methods cho Transaction entity
    /// </summary>
    public static class TransactionExtensions
    {
        /// <summary>
        /// Set ExternalTransactionCode nếu chưa có, sử dụng format PEPPAY-{ID}
        /// </summary>
        public static void EnsureExternalTransactionCode(this Transaction transaction)
        {
            if (string.IsNullOrWhiteSpace(transaction.ExternalTransactionCode) && transaction.Id > 0)
            {
                transaction.ExternalTransactionCode = $"PEPPAY-{transaction.Id}";
            }
        }
    }
}

