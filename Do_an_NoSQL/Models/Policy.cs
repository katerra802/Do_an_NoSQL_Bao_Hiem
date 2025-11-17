using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class Policy : MongoEntity
    {
        [BsonElement("policy_no")]
        public string PolicyNo { get; set; }

        [BsonElement("app_no")]
        public string AppNo { get; set; }

        [BsonElement("customer_id")]
        public string CustomerId { get; set; }

        [BsonElement("advisor_id")]
        public string AdvisorId { get; set; }

        [BsonElement("product_code")]
        public string ProductCode { get; set; }

        [BsonElement("issue_date")]
        public DateTime IssueDate { get; set; }

        [BsonElement("effective_date")]
        public DateTime EffectiveDate { get; set; }

        [BsonElement("maturity_date")]
        public DateTime MaturityDate { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("approved_premium")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ApprovedPremium { get; set; }   // ⭐ Phí chính thức / năm

        [BsonElement("annual_premium")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal AnnualPremium { get; set; }      // ⭐ = approved_premium

        [BsonElement("premium_mode")]
        public string PremiumMode { get; set; }

        [BsonElement("term_years")]
        public int TermYears { get; set; }

        [BsonElement("sum_assured")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SumAssured { get; set; }

        [BsonElement("first_premium_paid")]
        public bool FirstPremiumPaid { get; set; } = false;

        [BsonElement("issue_channel")]
        public string? IssueChannel { get; set; }

        [BsonElement("policy_pdf")]
        public string? PolicyPdf { get; set; }

        [BsonElement("remark")]
        public string? Remark { get; set; }

        [BsonElement("notes")]
        public string? Notes { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("last_modified")]
        public DateTime? LastModified { get; set; }

        [BsonElement("modified_by")]
        public string? ModifiedBy { get; set; }

        [BsonElement("is_locked")]
        public bool IsLocked { get; set; } = false;

        [BsonElement("lock_reason")]
        public string? LockReason { get; set; }

        [BsonIgnore] public Customer? Customer { get; set; }
        [BsonIgnore] public Product? Product { get; set; }
        [BsonIgnore] public Advisor? Advisor { get; set; }

        public List<Beneficiary> Beneficiaries { get; set; }
    }
}
