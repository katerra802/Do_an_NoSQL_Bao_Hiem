namespace Do_an_NoSQL.Models.ViewModels
{
    public class PolicyStatusUpdate
    {
        public string? Id { get; set; }
        public List<string>? Ids { get; set; }
        public List<string>? ExcludeIds { get; set; }

        public string NewStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool? IsLocked { get; set; }
        public string? LockReason { get; set; }
    }
}
