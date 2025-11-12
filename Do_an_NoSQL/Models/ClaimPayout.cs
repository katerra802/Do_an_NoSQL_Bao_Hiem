using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class ClaimPayout: Models.MongoEntity
    {
        [BsonElement("claim_no")]
        public string ClaimNo { get; set; }

        [BsonElement("approved_amount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ApprovedAmount { get; set; }

        [BsonElement("paid_amount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PaidAmount { get; set; }

        [BsonElement("pay_method")]
        public string PayMethod { get; set; }

        [BsonElement("paid_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? PaidAt { get; set; } // Nullable

        [BsonElement("reference")]
        public string Reference { get; set; }
    }
}
