using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class Claim: Models.MongoEntity
    {
        [BsonElement("claim_no")]
        public string ClaimNo { get; set; } // Mã nghiệp vụ

        // Tham chiếu bằng mã nghiệp vụ
        [BsonElement("policy_no")]
        public string PolicyNo { get; set; }

        [BsonElement("beneficiary_name")]
        public string BeneficiaryName { get; set; }

        [BsonElement("benefit_type")]
        public string BenefitType { get; set; }

        [BsonElement("event_date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime EventDate { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("submitted_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime SubmittedAt { get; set; }

        [BsonElement("documents")]
        public List<string> Documents { get; set; }
    }
}
