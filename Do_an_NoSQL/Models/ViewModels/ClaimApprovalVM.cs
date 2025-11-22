namespace Do_an_NoSQL.Models.ViewModels
{
    public class ClaimApprovalVM
    {
        public string ClaimId { get; set; }
        public string Decision { get; set; } // approved / rejected
        public decimal ApprovedAmount { get; set; }
        public string PayMethod { get; set; }
        public string Notes { get; set; }
        public decimal RequestedAmount { get; set; }
    }
}
