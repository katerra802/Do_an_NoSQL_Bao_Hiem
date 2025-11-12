using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class PaymentSchedule: Models.MongoEntity
    {
        [BsonElement("policy_no")]
        public string PolicyNo { get; set; }

        [BsonElement("period_no")]
        public int PeriodNo { get; set; }

        [BsonElement("due_date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime DueDate { get; set; }

        [BsonElement("premium_due")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PremiumDue { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }
    }
}
