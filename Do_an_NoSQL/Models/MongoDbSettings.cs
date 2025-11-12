namespace Do_an_NoSQL.Models
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "DB_QLBaoHiem";
    }
}
