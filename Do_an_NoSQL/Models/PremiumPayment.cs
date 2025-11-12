using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class PremiumPayment: Models.MongoEntity
    {
        [BsonElement("policy_no")]
        public string PolicyNo { get; set; }

        [BsonElement("due_date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime DueDate { get; set; }

        [BsonElement("paid_date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? PaidDate { get; set; } // Nullable

        [BsonElement("amount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Amount { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("channel")]
        public string Channel { get; set; }

        [BsonElement("reference")]
        public string Reference { get; set; }
    }
}
