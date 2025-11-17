using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class Beneficiary : MongoEntity  
    {
        [BsonElement("app_no")]
        public string AppNo { get; set; } = string.Empty;  

        [BsonElement("policy_no")]
        public string? PolicyNo { get; set; }  

        [BsonElement("full_name")]
        public string FullName { get; set; } = string.Empty;

        [BsonElement("relation")]
        public string Relation { get; set; } = string.Empty;

        [BsonElement("share_percent")]
        public int SharePercent { get; set; }

        [BsonElement("dob")]
        public DateTime Dob { get; set; }

        [BsonElement("national_id")]
        public string NationalId { get; set; } = string.Empty;

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}