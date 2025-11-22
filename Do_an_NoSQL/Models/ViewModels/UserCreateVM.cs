namespace Do_an_NoSQL.Models.ViewModels
{
    public class UserCreateVM
    {
        public string? Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string RoleCode { get; set; } = string.Empty;

        public string Status { get; set; } = "active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
