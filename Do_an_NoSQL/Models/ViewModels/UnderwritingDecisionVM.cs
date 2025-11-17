namespace Do_an_NoSQL.Models.ViewModels
{
    public class UnderwritingDecisionVM
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public decimal BasePremium { get; set; }        // ⭐ THÊM
        public decimal ExtraPremium { get; set; }
        public decimal ApprovedPremium { get; set; }
        public string Decision { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
