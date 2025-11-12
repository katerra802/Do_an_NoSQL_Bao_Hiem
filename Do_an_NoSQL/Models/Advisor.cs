using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class Advisor: Models.MongoEntity
    {
        [BsonElement("code")]
        public string Code { get; set; }

        [BsonElement("full_name")]
        public string FullName { get; set; }

        [BsonElement("branch")]
        public string Branch { get; set; }

        [BsonElement("phone")]
        public string Phone { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }
    }
}
