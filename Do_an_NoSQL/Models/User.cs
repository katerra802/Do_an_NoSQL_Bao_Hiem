using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class User : Models.MongoEntity
    {
        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("full_name")]
        public string FullName { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        // Dữ liệu cũ trong DB vẫn là "role_id", 
        // nên giữ nguyên annotation này nhưng mapping sang RoleCode.
        [BsonElement("role_id")]
        public string RoleCode { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "active";

        // ✅ Thêm mới: Thời gian tạo
        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ✅ Tuỳ chọn: Thời gian cập nhật gần nhất
        [BsonElement("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
