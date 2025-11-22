
namespace Do_an_NoSQL.Models.ViewModels
{
    public class CustomerCreateVM
    {
        public string? Id { get; set; }
        public string FullName { get; set; }
        public DateTime Dob { get; set; }
        public string Gender { get; set; }
        public string NationalId { get; set; }
        public string Occupation { get; set; }
        public decimal Income { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string HealthInfo { get; set; }
        public string Source { get; set; }
    }

}
