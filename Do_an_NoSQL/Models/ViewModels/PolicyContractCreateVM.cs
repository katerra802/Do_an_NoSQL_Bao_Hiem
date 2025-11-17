namespace Do_an_NoSQL.Models.ViewModels
{
    public class PolicyContractCreateVM
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string AdvisorId { get; set; }
        public string ProductId { get; set; }
        public string PremiumMode { get; set; }
        public decimal SumAssured { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
    }

}
