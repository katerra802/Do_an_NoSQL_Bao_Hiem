using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class User : MongoEntity
    {
        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("password")]
        public string Password { get; set; } // TODO: Nên hash password trong thực tế

        [BsonElement("full_name")]
        public string FullName { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        // ✅ Mapping từ "role_id" trong DB sang RoleCode
        [BsonElement("role_id")]
        public string RoleCode { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "active";

        // ✅ Thời gian tạo
        [BsonElement("created_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ✅ Tuỳ chọn: Thời gian cập nhật gần nhất
        [BsonElement("updated_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? UpdatedAt { get; set; }

        // ✅ SỬA: Property helper để check active - PHẢI có get/set hoặc chỉ get với body
        [BsonIgnore]
        public bool IsActive => Status?.ToLower() == "active";

        // ✅ Navigation property (không lưu vào DB)
        [BsonIgnore]
        public Role Role { get; set; }
    }
}