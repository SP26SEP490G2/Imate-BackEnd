using System.ComponentModel.DataAnnotations;

namespace Imate.API.Business.Helper
{
    public class RevenueTransactionQueryParameters : QueryParameters
    {
        [Required]
        public int Year { get; set; }

        public int? Month { get; set; }

        public string? TransactionType { get; set; }

        public string? SearchTerm { get; set; }
    }
}
