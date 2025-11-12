using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class Beneficiary: Models.MongoEntity
    {
        [BsonElement("policy_no")]
        public string PolicyNo { get; set; }

        [BsonElement("full_name")]
        public string FullName { get; set; }

        [BsonElement("relation")]
        public string Relation { get; set; }

        [BsonElement("share_percent")]
        public int SharePercent { get; set; }

        [BsonElement("dob")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Dob { get; set; }

        [BsonElement("national_id")]
        public string NationalId { get; set; }
    }
}
