using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class Policy: Models.MongoEntity
    {
        [BsonElement("policy_no")]
        public string PolicyNo { get; set; } // Mã nghiệp vụ

        [BsonElement("app_no")]
        public string AppNo { get; set; } // Mã nghiệp vụ

        // --- CẬP NHẬT THAM CHIẾU ---
        [BsonElement("customer_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string CustomerId { get; set; } // Tham chiếu đến Customer._id

        [BsonElement("advisor_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string AdvisorId { get; set; } // Tham chiếu đến Advisor._id

        [BsonElement("product_id")] // Đổi tên từ product_code
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProductId { get; set; } // Tham chiếu đến Product._id
        // --- KẾT THÚC CẬP NHẬT ---

        [BsonElement("issue_date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime IssueDate { get; set; }

        [BsonElement("effective_date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime EffectiveDate { get; set; }

        [BsonElement("maturity_date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime MaturityDate { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("annual_premium")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal AnnualPremium { get; set; }

        [BsonElement("premium_mode")]
        public string PremiumMode { get; set; }

        [BsonElement("term_years")]
        public int TermYears { get; set; }

        [BsonElement("sum_assured")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SumAssured { get; set; }

        [BsonElement("created_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }
    }
}
