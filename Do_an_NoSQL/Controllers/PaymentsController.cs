using Do_an_NoSQL.Database;
using Do_an_NoSQL.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;

namespace Do_an_NoSQL.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly MongoDbContext _context;

        public PaymentsController(MongoDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(
    string tab = "premium",
    string search = "",
    string status = "",
    string channel = "",
    string pay_method = "",
    DateTime? from_date = null,
    DateTime? to_date = null,
    int page = 1,
    [FromQuery(Name = "per_page")] int pageSize = 10)
        {
            ViewBag.ActiveTab = tab ?? "premium";
            ViewBag.Search = search ?? "";
            ViewBag.Status = status ?? "";
            ViewBag.Channel = channel ?? "";
            ViewBag.PayMethod = pay_method ?? "";
            ViewBag.FromDate = from_date;
            ViewBag.ToDate = to_date;

            return tab == "payout"
                ? GetClaimPayoutsView(search, from_date, to_date, page, pageSize, pay_method)
                : GetPremiumPaymentsView(search, status, from_date, to_date, page, pageSize, channel);
        }


        private IActionResult GetPremiumPaymentsView(
     string search,
     string status,
     DateTime? from_date,
     DateTime? to_date,
     int page,
     int pageSize,
     string channel = "")
        {
            try
            {
                var query = _context.PremiumPayments.AsQueryable();


                // 🔍 Tìm kiếm theo mã hợp đồng hoặc mã tham chiếu
                if (!string.IsNullOrEmpty(search))
                {
                    var keyword = search.Trim().ToLower();
                    query = query.Where(x =>
                        x.PolicyNo.ToLower().Contains(keyword) ||
                        (x.Reference != null && x.Reference.ToLower().Contains(keyword))
                    );
                }

                // Trạng thái
                if (!string.IsNullOrEmpty(status))
                    query = query.Where(x => x.Status == status);

                // Kênh thanh toán
                if (!string.IsNullOrEmpty(channel))
                    query = query.Where(x => x.Channel == channel);

                // Ngày đến hạn
                if (from_date.HasValue)
                {
                    var fromDateOnly = from_date.Value.Date;
                    query = query.Where(x => x.DueDate >= fromDateOnly);
                }

                if (to_date.HasValue)
                {
                    var toDateOnly = to_date.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => x.DueDate <= toDateOnly);
                }


                query = query.OrderByDescending(x => x.DueDate);

                var totalItems = query.Count();
                var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var pagedResult = new PagedResult<dynamic>
                {
                    Items = items.Cast<dynamic>().ToList(),
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                Console.WriteLine($"Filtered items: {totalItems}");
                Console.WriteLine($"Items retrieved: {items.Count}");

                ViewBag.RouteValues = new Dictionary<string, string>
        {
            { "tab", "premium" },
            { "search", search ?? "" },
            { "status", status ?? "" },
            { "channel", channel ?? "" },
            { "from_date", from_date?.ToString("yyyy-MM-dd") ?? "" },
            { "to_date", to_date?.ToString("yyyy-MM-dd") ?? "" }
        };

                return View(pagedResult);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi tải dữ liệu thanh toán: " + ex.Message;
                return View(new PagedResult<dynamic>());
            }
        }

        private IActionResult GetClaimPayoutsView(
    string search,
    DateTime? from_date,
    DateTime? to_date,
    int page,
    int pageSize,
    string pay_method = "")
        {
            try
            {
                var query = _context.ClaimPayouts.AsQueryable();

                // 🔍 Tìm kiếm theo mã yêu cầu, mã hợp đồng (join claim), hoặc mã tham chiếu
                if (!string.IsNullOrEmpty(search))
                {
                    var keyword = search.Trim().ToLower();

                    // Lấy danh sách ClaimNo có PolicyNo khớp
                    var relatedClaims = _context.Claims.AsQueryable()
                        .Where(c => c.PolicyNo.ToLower().Contains(keyword))
                        .Select(c => c.ClaimNo)
                        .ToList();

                    query = query.Where(x =>
                        x.ClaimNo.ToLower().Contains(keyword) ||
                        (x.Reference != null && x.Reference.ToLower().Contains(keyword)) ||
                        relatedClaims.Contains(x.ClaimNo)
                    );
                }

                // Lọc theo phương thức thanh toán
                if (!string.IsNullOrEmpty(pay_method))
                    query = query.Where(x => x.PayMethod == pay_method);

                // Lọc thời gian chi trả
                if (from_date.HasValue)
                {
                    var fromDateOnly = from_date.Value.Date;
                    query = query.Where(x => x.PaidAt.HasValue && x.PaidAt.Value >= fromDateOnly);
                }

                if (to_date.HasValue)
                {
                    var toDateOnly = to_date.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => x.PaidAt.HasValue && x.PaidAt.Value <= toDateOnly);
                }

                query = query.OrderByDescending(x => x.PaidAt);

                var totalItems = query.Count();
                var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // Debug log để kiểm tra
                Console.WriteLine($"Claim payouts total before paging: {totalItems}");
                Console.WriteLine($"Displayed items: {items.Count}");

                var pagedResult = new PagedResult<dynamic>
                {
                    Items = items.Cast<dynamic>().ToList(),
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                ViewBag.RouteValues = new Dictionary<string, string>
        {
            { "tab", "payout" },
            { "search", search ?? "" },
            { "pay_method", pay_method ?? "" },
            { "from_date", from_date?.ToString("yyyy-MM-dd") ?? "" },
            { "to_date", to_date?.ToString("yyyy-MM-dd") ?? "" }
        };

                return View(pagedResult);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi tải dữ liệu chi trả: " + ex.Message;
                return View(new PagedResult<dynamic>());
            }
        }

        // API endpoints
        [HttpGet]
        public IActionResult GetPremiumPayments()
        {
            try
            {
                var payments = _context.PremiumPayments
                    .Find(_ => true)
                    .SortByDescending(x => x.DueDate)
                    .ToList();

                var data = payments.Select(x => new
                {
                    x.PolicyNo,
                    DueDate = x.DueDate.ToString("dd/MM/yyyy"),
                    PaidDate = x.PaidDate?.ToString("dd/MM/yyyy"),
                    Amount = x.Amount,
                    x.Status,
                    x.Channel,
                    x.Reference
                }).ToList();

                return Json(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải dữ liệu thanh toán: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetClaimPayouts()
        {
            try
            {
                var payouts = _context.ClaimPayouts
                    .Find(_ => true)
                    .SortByDescending(x => x.PaidAt)
                    .ToList();

                var data = payouts.Select(x => new
                {
                    x.ClaimNo,
                    RequestedAmount = x.RequestedAmount,
                    ApprovedAmount = x.ApprovedAmount,
                    PaidAmount = x.PaidAmount,
                    x.PayMethod,
                    PaidAt = x.PaidAt?.ToString("dd/MM/yyyy"),
                    x.Reference
                }).ToList();

                return Json(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải dữ liệu chi trả: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetPaymentDetail(string id)
        {
            var payment = _context.PremiumPayments
                .Find(p => p.Id == id)
                .FirstOrDefault();

            if (payment == null)
                return NotFound(new { success = false, message = "Không tìm thấy giao dịch." });

            return Json(new
            {
                policy_no = payment.PolicyNo,
                due_date = payment.DueDate,
                paid_date = payment.PaidDate,
                amount = payment.Amount,
                channel = payment.Channel,
                reference = payment.Reference,
                status = payment.Status
            });
        }

        [HttpGet]
        public IActionResult GetPayoutDetail(string id)
        {
            var payout = _context.ClaimPayouts
                .Find(p => p.Id == id)
                .FirstOrDefault();

            if (payout == null)
                return NotFound(new { success = false, message = "Không tìm thấy chi trả này." });

            // Lấy claim_no -> tìm policy_no từ bảng Claims
            var claim = _context.Claims
                .Find(c => c.ClaimNo == payout.ClaimNo)
                .FirstOrDefault();

            return Json(new
            {
                claim_no = payout.ClaimNo,
                policy_no = claim?.PolicyNo,
                requested_amount = payout.RequestedAmount,
                approved_amount = payout.ApprovedAmount,
                paid_amount = payout.PaidAmount,
                pay_method = payout.PayMethod,
                paid_at = payout.PaidAt,
                reference = payout.Reference
            });
        }

    }
}