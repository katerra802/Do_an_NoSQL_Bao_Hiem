using System.Diagnostics;
using Do_an_NoSQL.Models;
using Do_an_NoSQL.Database;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Do_an_NoSQL.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MongoDbContext _context;

        public HomeController(ILogger<HomeController> logger, MongoDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var today = DateTime.Today;
                var startOfDay = today;
                var endOfDay = today.AddDays(1);

                // Lấy thống kê tổng quan
                var totalPolicies = await _context.Policies.CountDocumentsAsync(FilterDefinition<Policy>.Empty);
                var totalCustomers = await _context.Customers.CountDocumentsAsync(FilterDefinition<Customer>.Empty);
                var pendingApplications = await _context.PolicyApplications
                    .CountDocumentsAsync(p => p.Status == "under_review");

                // Lấy các policies mới nhất (giả lập giao dịch hôm nay)
                var recentPolicies = await _context.Policies
                    .Find(FilterDefinition<Policy>.Empty)
                    .SortByDescending(p => p.CreatedAt)
                    .Limit(10)
                    .ToListAsync();

                // Lấy các payment gần đây
                var recentPayments = await _context.PremiumPayments
                    .Find(FilterDefinition<PremiumPayment>.Empty)
                    .SortByDescending(p => p.PaidDate)
                    .Limit(6)
                    .ToListAsync();

                // Lấy các claims đang chờ xử lý
                var pendingClaims = await _context.Claims
                    .Find(c => c.Status == "submitted")
                    .SortByDescending(c => c.SubmittedAt)
                    .Limit(5)
                    .ToListAsync();

                // Lấy các applications gần đây
                var recentApplications = await _context.PolicyApplications
                    .Find(FilterDefinition<PolicyApplication>.Empty)
                    .SortByDescending(p => p.SubmittedAt)
                    .Limit(5)
                    .ToListAsync();

                // Tính tổng giá trị hợp đồng
                var allPolicies = await _context.Policies
                    .Find(FilterDefinition<Policy>.Empty)
                    .ToListAsync();
                var totalValue = allPolicies.Sum(p => (decimal)p.SumAssured);

                // Tính tổng số tiền đã thanh toán
                var allPayments = await _context.PremiumPayments
                    .Find(FilterDefinition<PremiumPayment>.Empty)
                    .ToListAsync();
                var totalPayments = allPayments.Sum(p => (decimal)p.Amount);

                // Tạo ViewModel
                var viewModel = new DashboardViewModel
                {
                    TotalPolicies = (int)totalPolicies,
                    TotalCustomers = (int)totalCustomers,
                    PendingApplications = (int)pendingApplications,
                    TotalValue = totalValue,
                    TotalPaymentsToday = totalPayments,
                    NewPoliciesToday = recentPolicies.Count,
                    RecentPolicies = recentPolicies,
                    RecentPayments = recentPayments,
                    PendingClaims = pendingClaims,
                    RecentApplications = recentApplications
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                return View(new DashboardViewModel());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    // ViewModel cho Dashboard
    public class DashboardViewModel
    {
        public int TotalPolicies { get; set; }
        public int TotalCustomers { get; set; }
        public int PendingApplications { get; set; }
        public decimal TotalValue { get; set; }
        public decimal TotalPaymentsToday { get; set; }
        public int NewPoliciesToday { get; set; }
        public List<Policy> RecentPolicies { get; set; } = new();
        public List<PremiumPayment> RecentPayments { get; set; } = new();
        public List<Claim> PendingClaims { get; set; } = new();
        public List<PolicyApplication> RecentApplications { get; set; } = new();
    }
}