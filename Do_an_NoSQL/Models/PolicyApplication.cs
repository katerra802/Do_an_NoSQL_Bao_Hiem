using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class PolicyApplication: Models.MongoEntity
    {
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

        [BsonElement("sum_assured")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SumAssured { get; set; }

        [BsonElement("premium_mode")]
        public string PremiumMode { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("notes")]
        public string Notes { get; set; }

        [BsonElement("submitted_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime SubmittedAt { get; set; }

        [BsonElement("documents")]
        public List<string> Documents { get; set; }
    }
}
