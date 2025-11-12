using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Do_an_NoSQL.Models;

namespace Do_an_NoSQL.Database
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }
        public IMongoCollection<Customer> Customers
            => _database.GetCollection<Customer>("Customers");

        public IMongoCollection<Advisor> Advisors
            => _database.GetCollection<Advisor>("Advisors");

        public IMongoCollection<Product> Products
            => _database.GetCollection<Product>("Products");
    }
}
