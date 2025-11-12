using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class UnderwritingDecision: Models.MongoEntity
    {
        [BsonElement("app_no")]
        public string AppNo { get; set; } // Tham chiếu đến PolicyApplication.app_no

        // --- CẬP NHẬT THAM CHIẾU ---
        [BsonElement("underwriter_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UnderwriterId { get; set; } // Tham chiếu đến User._id
        // --- KẾT THÚC CẬP NHẬT ---

        [BsonElement("risk_level")]
        public string RiskLevel { get; set; }

        [BsonElement("extra_premium")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ExtraPremium { get; set; }

        [BsonElement("medical_required")]
        public bool MedicalRequired { get; set; }

        [BsonElement("decision")]
        public string Decision { get; set; }

        [BsonElement("notes")]
        public string Notes { get; set; }

        [BsonElement("decided_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime DecidedAt { get; set; }
    }
}
