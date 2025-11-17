using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class UnderwritingDecision : MongoEntity
    {
        [BsonElement("app_no")]
        public string AppNo { get; set; }

        [BsonElement("underwriter_id")]
        public string UnderwriterId { get; set; }

        [BsonElement("risk_level")]
        public string RiskLevel { get; set; }

        [BsonElement("base_premium")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BasePremium { get; set; }   // ⭐ Thêm mới

        [BsonElement("extra_premium")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ExtraPremium { get; set; }

        [BsonElement("approved_premium")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ApprovedPremium { get; set; }

        [BsonElement("medical_required")]
        public bool MedicalRequired { get; set; }

        [BsonElement("decision")]
        public string Decision { get; set; }

        [BsonElement("notes")]
        public string Notes { get; set; }

        [BsonElement("decided_at")]
        public DateTime DecidedAt { get; set; }
    }
}
