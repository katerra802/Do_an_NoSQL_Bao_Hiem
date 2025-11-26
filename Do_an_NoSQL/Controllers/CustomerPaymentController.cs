using Do_an_NoSQL.Database;
using Do_an_NoSQL.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Do_an_NoSQL.Controllers
{
    public class CustomerPaymentController : Controller
    {
        private readonly MongoDbContext _context;

        public CustomerPaymentController(MongoDbContext context)
        {
            _context = context;
        }

        // ✅ THÊM: Trang chủ hiển thị danh sách lịch thanh toán theo PolicyNo
        [HttpGet]
        public IActionResult Index(string policyNo = "")
        {
            ViewBag.PolicyNo = policyNo;

            if (string.IsNullOrEmpty(policyNo))
            {
                return View(new List<PremiumPayment>());
            }

            try
            {
                // Lấy danh sách thanh toán theo PolicyNo
                var payments = _context.PremiumPayments
                    .Find(p => p.PolicyNo == policyNo)
                    .SortBy(p => p.DueDate)
                    .ToList();

                // Lấy thông tin policy
                var policy = _context.Policies
                    .Find(p => p.PolicyNo == policyNo)
                    .FirstOrDefault();

                if (policy != null)
                {
                    policy.Customer = _context.Customers
                        .Find(c => c.CustomerCode == policy.CustomerId)
                        .FirstOrDefault();

                    policy.Product = _context.Products
                        .Find(p => p.ProductCode == policy.ProductCode)
                        .FirstOrDefault();

                    ViewBag.Policy = policy;
                }

                // Tính penalty cho các khoản quá hạn
                foreach (var payment in payments)
                {
                    if (payment.Status != "paid" && payment.DueDate < DateTime.UtcNow)
                    {
                        var product = policy?.Product;
                        if (product != null)
                        {
                            var daysLate = (DateTime.UtcNow - payment.DueDate.AddDays(product.GracePeriodDays)).Days;
                            if (daysLate > 0)
                            {
                                payment.PenaltyAmount = payment.Amount * (product.LatePenaltyRate / 100) * daysLate;
                                payment.PaymentType = "penalty";
                            }
                        }
                    }
                }

                return View(payments);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tải dữ liệu: " + ex.Message;
                return View(new List<PremiumPayment>());
            }
        }

        // GET: CustomerPayment/Pay/{id}
        // Hiển thị form thanh toán cho 1 khoản phí pending
        [HttpGet]
        public IActionResult Pay(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin thanh toán!";
                return RedirectToAction("Index");
            }

            // Tìm premium payment theo ID
            var payment = _context.PremiumPayments
                .Find(p => p.Id == id && p.Status == "pending")
                .FirstOrDefault();

            if (payment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khoản phí cần thanh toán hoặc đã được thanh toán!";
                return RedirectToAction("Index");
            }

            // Tính penalty nếu quá hạn
            if (payment.DueDate < DateTime.UtcNow)
            {
                var pol = _context.Policies.Find(p => p.PolicyNo == payment.PolicyNo).FirstOrDefault();
                var product = _context.Products.Find(p => p.ProductCode == pol.ProductCode).FirstOrDefault();

                if (product != null)
                {
                    var daysLate = (DateTime.UtcNow - payment.DueDate.AddDays(product.GracePeriodDays)).Days;
                    if (daysLate > 0)
                    {
                        payment.PenaltyAmount = payment.Amount * (product.LatePenaltyRate / 100) * daysLate;
                        payment.PaymentType = "penalty";
                    }
                }
            }

            // Lấy thông tin policy liên quan
            var policy = _context.Policies
                .Find(p => p.PolicyNo == payment.PolicyNo)
                .FirstOrDefault();

            if (policy != null)
            {
                policy.Product = _context.Products
                    .Find(p => p.ProductCode == policy.ProductCode)
                    .FirstOrDefault();
            }

            ViewBag.Policy = policy;

            return View(payment);
        }

        // POST: CustomerPayment/ProcessPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessPayment(string id, string channel, string payMethod, string reference)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin thanh toán!" });
            }

            try
            {
                // Tìm premium payment
                var payment = _context.PremiumPayments
                    .Find(p => p.Id == id)
                    .FirstOrDefault();

                if (payment == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khoản phí!" });
                }

                if (payment.Status == "paid")
                {
                    return Json(new { success = false, message = "Khoản phí này đã được thanh toán!" });
                }

                // Tính lại penalty (nếu có)
                var penalty = 0m;
                if (payment.DueDate < DateTime.UtcNow)
                {
                    var pol = _context.Policies.Find(p => p.PolicyNo == payment.PolicyNo).FirstOrDefault();
                    var product = _context.Products.Find(p => p.ProductCode == pol.ProductCode).FirstOrDefault();

                    if (product != null)
                    {
                        var daysLate = (DateTime.UtcNow - payment.DueDate.AddDays(product.GracePeriodDays)).Days;
                        if (daysLate > 0)
                        {
                            penalty = payment.Amount * (product.LatePenaltyRate / 100) * daysLate;
                        }
                    }
                }

                // Cập nhật thông tin thanh toán
                var updateDef = Builders<PremiumPayment>.Update
                    .Set(p => p.Status, "paid")
                    .Set(p => p.PaidDate, DateTime.UtcNow)
                    .Set(p => p.Channel, channel ?? "customer")
                    .Set(p => p.PayMethod, payMethod ?? "online")
                    .Set(p => p.PaymentType, penalty > 0 ? "penalty" : "normal")
                    .Set(p => p.PenaltyAmount, penalty)
                    .Set(p => p.Reference, reference ?? $"CUST-{DateTime.UtcNow:yyyyMMddHHmmss}");

                var result = _context.PremiumPayments.UpdateOne(
                    p => p.Id == id,
                    updateDef
                );

                if (result.ModifiedCount > 0)
                {
                    // Cập nhật PaymentSchedule tương ứng
                    if (!string.IsNullOrEmpty(payment.RelatedScheduleId))
                    {
                        var scheduleUpdate = Builders<PaymentSchedule>.Update
                            .Set(s => s.Status, "paid");

                        _context.PaymentSchedules.UpdateOne(
                            s => s.Id == payment.RelatedScheduleId,
                            scheduleUpdate
                        );
                    }

                    return Json(new
                    {
                        success = true,
                        message = "Thanh toán thành công!",
                        policyNo = payment.PolicyNo,
                        amount = payment.Amount,
                        penalty = penalty,
                        total = payment.Amount + penalty,
                        paidDate = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể cập nhật trạng thái thanh toán!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // GET: CustomerPayment/Success
        // Trang xác nhận thanh toán thành công
        [HttpGet]
        public IActionResult Success(string policyNo, string amount, string penalty, string total, string paidDate)
        {
            ViewBag.PolicyNo = policyNo;
            ViewBag.Amount = amount;
            ViewBag.Penalty = penalty;
            ViewBag.Total = total;
            ViewBag.PaidDate = paidDate;
            return View();
        }
    }
}