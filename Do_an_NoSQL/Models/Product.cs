using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Do_an_NoSQL.Models
{
    public class Product: Models.MongoEntity
    {
        [BsonElement("product_code")]
        public string ProductCode { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("type")]
        public string Type { get; set; }

        [BsonElement("purpose")]
        public List<string> Purpose { get; set; }

        [BsonElement("term_years")]
        public int TermYears { get; set; }

        [BsonElement("min_age")]
        public int MinAge { get; set; }

        [BsonElement("max_age")]
        public int MaxAge { get; set; }

        [BsonElement("min_sum_assured")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal MinSumAssured { get; set; }

        [BsonElement("max_sum_assured")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal MaxSumAssured { get; set; }

        [BsonElement("riders")]
        public List<Rider> Riders { get; set; }

        [BsonElement("created_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }
    }

    public class Rider
    {
        [BsonElement("code")]
        public string Code { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }
    }
}
