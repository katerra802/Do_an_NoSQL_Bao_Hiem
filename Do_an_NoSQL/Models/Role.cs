using MongoDB.Bson.Serialization.Attributes;

namespace Do_an_NoSQL.Models
{
    public class Role : Models.MongoEntity
    {
        [BsonElement("code")]
        public string Code { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }
    }
}
