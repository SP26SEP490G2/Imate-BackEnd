using Imate.API.Business.Helper;
using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Payment
{
    public class RevenueQueryParameters
    {
        [Required]
        public int Year { get; set; }

        public int? Month { get; set; } // Null = all months of the year

        public string? TransactionType { get; set; } // "PointBookingPayout", "PointSubscriptionFee", etc.
    }
}
