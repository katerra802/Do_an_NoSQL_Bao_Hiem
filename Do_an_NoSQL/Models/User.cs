using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class User: Models.MongoEntity
    {
        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("full_name")]
        public string FullName { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        // --- CẬP NHẬT THAM CHIẾU ---
        [BsonElement("role_id")] // Đổi tên từ role_code
        [BsonRepresentation(BsonType.ObjectId)]
        public string RoleId { get; set; } // Tham chiếu đến Role._id
        // --- KẾT THÚC CẬP NHẬT ---

        [BsonElement("status")]
        public string Status { get; set; }
    }
}
