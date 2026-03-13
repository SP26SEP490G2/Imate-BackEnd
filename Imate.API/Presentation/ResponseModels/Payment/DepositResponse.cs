namespace Imate.API.Presentation.ResponseModels.Payment
{
    public class DepositResponse
    {
        public int TransactionId { get; set; } 
        public string CheckoutUrl { get; set; }  
        public string OrderCode { get; set; }
    }
}
