using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Do_an_NoSQL.Models
{
    public class RolePermission : MongoEntity
    {
        [BsonElement("role_code")]
        public string RoleCode { get; set; }

        [BsonElement("permissions")]
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
