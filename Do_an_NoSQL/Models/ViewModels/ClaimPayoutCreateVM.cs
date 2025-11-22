namespace Do_an_NoSQL.Models.ViewModels
{
    public class ClaimPayoutCreateVM
    {
        public string ClaimId { get; set; }
        public decimal PaidAmount { get; set; }
        public string PayMethod { get; set; }
        public DateTime PaidAt { get; set; }
        public string Reference { get; set; }
    }
}
