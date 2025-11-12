using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Do_an_NoSQL.Models
{
    public class Customer: Models.MongoEntity
    {
        [BsonElement("customer_code")]
        public string CustomerCode { get; set; }

        [BsonElement("full_name")]
        public string FullName { get; set; }

        [BsonElement("dob")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Dob { get; set; }

        [BsonElement("gender")]
        public string Gender { get; set; }

        [BsonElement("national_id")]
        public string NationalId { get; set; }

        [BsonElement("occupation")]
        public string Occupation { get; set; }

        [BsonElement("income")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Income { get; set; }

        [BsonElement("address")]
        public string Address { get; set; }

        [BsonElement("phone")]
        public string Phone { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("health_info")]
        public string HealthInfo { get; set; }

        [BsonElement("source")]
        public string Source { get; set; }

        [BsonElement("created_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }
    }
}
