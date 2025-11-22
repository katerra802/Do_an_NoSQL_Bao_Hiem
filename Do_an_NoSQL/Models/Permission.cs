using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class Permission : MongoEntity
    {
        [BsonElement("code")]
        public string Code { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("module")]
        public string Module { get; set; }
    }
}
