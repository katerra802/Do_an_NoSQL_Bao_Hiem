using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class PolicyApplication : MongoEntity
    {
        [BsonElement("app_no")]
        public string AppNo { get; set; }

        [BsonElement("customer_id")]
        public string CustomerId { get; set; }

        [BsonElement("advisor_id")]
        public string AdvisorId { get; set; }

        [BsonElement("product_code")]
        public string ProductCode { get; set; }

        [BsonElement("sum_assured")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SumAssured { get; set; }

        [BsonElement("premium_mode")]
        public string PremiumMode { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("base_premium")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? BasePremium { get; set; }  

        [BsonElement("approved_premium")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? ApprovedPremium { get; set; }

        [BsonElement("underwriting_result")]
        public string? UnderwritingResult { get; set; }

        [BsonElement("decision")]
        public string? Decision { get; set; }

        [BsonElement("decision_date")]
        public DateTime? DecisionDate { get; set; }

        [BsonElement("is_first_premium_received")]
        public bool IsFirstPremiumReceived { get; set; } = false;

        [BsonElement("first_premium_paid_date")]
        public DateTime? FirstPremiumPaidDate { get; set; }

        [BsonElement("first_premium_amount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? FirstPremiumAmount { get; set; }

        [BsonElement("issued_policy_no")]  // ⭐ Liên kết ngược về Policy
        public string? IssuedPolicyNo { get; set; }

        [BsonElement("notes")]
        public string? Notes { get; set; }

        [BsonElement("submitted_at")]
        public DateTime SubmittedAt { get; set; }

        [BsonElement("documents")]
        public List<string>? Documents { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }

        [BsonIgnore] public Customer? Customer { get; set; }
        [BsonIgnore] public Advisor? Advisor { get; set; }
        [BsonIgnore] public Product? Product { get; set; }
    }
}
