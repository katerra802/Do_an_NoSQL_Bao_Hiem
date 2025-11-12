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
            => _database.GetCollection<Customer>("customers");

        public IMongoCollection<Advisor> Advisors
            => _database.GetCollection<Advisor>("advisors");

        public IMongoCollection<Product> Products
            => _database.GetCollection<Product>("products");

        public IMongoCollection<PolicyApplication> PolicyApplications
            => _database.GetCollection<PolicyApplication>("policy_applications");

        public IMongoCollection<Policy> Policies
            => _database.GetCollection<Policy>("policies");

        public IMongoCollection<UnderwritingDecision> UnderwritingDecisions
            => _database.GetCollection<UnderwritingDecision>("underwriting_decisions");

        public IMongoCollection<Beneficiary> Beneficiaries
            => _database.GetCollection<Beneficiary>("beneficiaries");

        public IMongoCollection<PaymentSchedule> PaymentSchedules
            => _database.GetCollection<PaymentSchedule>("payment_schedules");

        public IMongoCollection<PremiumPayment> PremiumPayments
            => _database.GetCollection<PremiumPayment>("premium_payments");

        public IMongoCollection<Claim> Claims
            => _database.GetCollection<Claim>("claims");

        public IMongoCollection<ClaimPayout> ClaimPayouts
            => _database.GetCollection<ClaimPayout>("claim_payouts");

        public IMongoCollection<Role> Roles
            => _database.GetCollection<Role>("roles");

        public IMongoCollection<User> Users
            => _database.GetCollection<User>("users");
    }
}
