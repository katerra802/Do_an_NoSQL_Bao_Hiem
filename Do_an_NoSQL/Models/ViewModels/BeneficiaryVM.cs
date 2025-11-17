using MongoDB.Bson;

namespace Do_an_NoSQL.Models.ViewModels
{
    public class BeneficiaryVM
    {
        public ObjectId? Id { get; set; }
        public string FullName { get; set; }
        public string Relation { get; set; }
        public int SharePercent { get; set; }
        public DateTime Dob { get; set; }
        public string NationalId { get; set; }
    }
}
