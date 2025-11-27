using Do_an_NoSQL.Database;
using Do_an_NoSQL.Helpers;
using Do_an_NoSQL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;

namespace Do_an_NoSQL.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly MongoDbContext _context;

        public PaymentsController(MongoDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(
    string tab = "schedule",
    string search = "",
    string status = "",
    string channel = "",
    string pay_method = "",
    DateTime? from_date = null,
    DateTime? to_date = null,
    int page = 1,
    [FromQuery(Name = "per_page")] int pageSize = 10)
        {
            if (!PermissionHelper.CanViewPayment(User, _context))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            // Check quyền cho từng tab
            if (tab == "schedule" && !PermissionHelper.CanManagePayment(User, _context))
            {
                tab = "history"; // Chuyển sang tab chỉ xem
            }

            if (tab == "history" && !RoleHelper.CanAccessHistoryTab(User))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (tab == "payout" && !RoleHelper.CanAccessPayoutTab(User))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            ViewBag.ActiveTab = tab ?? "schedule";
            ViewBag.Search = search ?? "";
            ViewBag.Status = status ?? "";
            ViewBag.Channel = channel ?? "";
            ViewBag.PayMethod = pay_method ?? "";
            ViewBag.FromDate = from_date;
            ViewBag.ToDate = to_date;

            return tab switch
            {
                "payout" => GetClaimPayoutsView(search, from_date, to_date, page, pageSize, pay_method),
                "history" => GetPremiumPaymentsView(search, status, from_date, to_date, page, pageSize, channel),
                _ => GetPaymentSchedulesByCustomer(search, status, from_date, to_date, page, pageSize)
            };
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
                // ✅ CHỈ LẤY CÁC PAYMENT ĐÃ THANH TOÁN
                var query = _context.PremiumPayments
                    .AsQueryable()
                    .Where(x => x.Status == "paid"); // ✅ THÊM FILTER MẶC ĐỊNH

                // 🔍 Tìm kiếm theo mã hợp đồng hoặc mã tham chiếu
                if (!string.IsNullOrEmpty(search))
                {
                    var keyword = search.Trim().ToLower();
                    query = query.Where(x =>
                        x.PolicyNo.ToLower().Contains(keyword) ||
                        (x.Reference != null && x.Reference.ToLower().Contains(keyword))
                    );
                }

                // ✅ BỎ FILTER STATUS (vì đã mặc định là "paid" rồi)
                // Nếu muốn cho phép filter khác thì uncomment:
                // if (!string.IsNullOrEmpty(status))
                //     query = query.Where(x => x.Status == status);

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

                query = query.OrderByDescending(x => x.PaidDate); // ✅ Sắp xếp theo ngày thanh toán thay vì due date

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
            { "tab", "history" }, // ✅ SỬA từ "premium" sang "history"
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

        // ✅ SỬA LẠI - Hiển thị PremiumPayments theo khách hàng (thay vì PaymentSchedules)
        private IActionResult GetPaymentSchedulesByCustomer(
            string search,
            string status,
            DateTime? from_date,
            DateTime? to_date,
            int page,
            int pageSize)
        {
            try
            {
                // Lấy tất cả PremiumPayments
                var paymentsQuery = _context.PremiumPayments.AsQueryable();

                // Filter theo status
                if (!string.IsNullOrEmpty(status))
                {
                    paymentsQuery = paymentsQuery.Where(p => p.Status == status);
                }

                // Filter theo date range
                if (from_date.HasValue)
                {
                    paymentsQuery = paymentsQuery.Where(p => p.DueDate >= from_date.Value);
                }

                if (to_date.HasValue)
                {
                    paymentsQuery = paymentsQuery.Where(p => p.DueDate <= to_date.Value);
                }

                var payments = paymentsQuery.ToList();

                // Lấy danh sách PolicyNo unique
                var policyNos = payments.Select(p => p.PolicyNo).Distinct().ToList();

                // Lấy policies tương ứng
                var policies = _context.Policies
                    .Find(p => policyNos.Contains(p.PolicyNo))
                    .ToList();

                // Lấy CustomerIds unique
                var customerIds = policies.Select(p => p.CustomerId).Distinct().ToList();

                // Filter theo search (tên khách hàng hoặc policy_no)
                if (!string.IsNullOrEmpty(search))
                {
                    var keyword = search.Trim().ToLower();

                    // Nếu search theo policy_no
                    if (keyword.StartsWith("pl-"))
                    {
                        policies = policies.Where(p => p.PolicyNo.ToLower().Contains(keyword)).ToList();
                        customerIds = policies.Select(p => p.CustomerId).Distinct().ToList();
                    }
                    else
                    {
                        // Search theo tên khách hàng
                        var matchedCustomers = _context.Customers
                            .Find(x => x.FullName.ToLower().Contains(keyword))
                            .ToList();

                        customerIds = matchedCustomers.Select(c => c.CustomerCode).ToList();
                        policies = policies.Where(p => customerIds.Contains(p.CustomerId)).ToList();
                    }

                    // Filter lại payments theo policies còn lại
                    policyNos = policies.Select(p => p.PolicyNo).ToList();
                    payments = payments.Where(p => policyNos.Contains(p.PolicyNo)).ToList();
                }

                // Lấy thông tin customers
                var customers = _context.Customers
                    .Find(c => customerIds.Contains(c.CustomerCode))
                    .ToList();

                // Group payments by customer
                var customerGroups = customers.Select(customer => new
                {
                    CustomerId = customer.CustomerCode,
                    CustomerName = customer.FullName,
                    CustomerPhone = customer.Phone,
                    CustomerEmail = customer.Email,
                    Policies = policies
                        .Where(p => p.CustomerId == customer.CustomerCode)
                        .Select(policy => new
                        {
                            PolicyNo = policy.PolicyNo,
                            ProductCode = policy.ProductCode,
                            // Lấy payments cho policy này (đổi tên từ Schedules thành Payments để rõ nghĩa)
                            Schedules = payments
                                .Where(pm => pm.PolicyNo == policy.PolicyNo)
                                .OrderBy(pm => pm.DueDate)
                                .Select(pm => new
                                {
                                    Id = pm.Id,
                                    PeriodNo = GetPeriodFromPayment(pm, payments.Where(p => p.PolicyNo == policy.PolicyNo).ToList()),
                                    DueDate = pm.DueDate,
                                    PremiumDue = pm.Amount,
                                    Status = pm.Status,
                                    PaidDate = pm.PaidDate,
                                    Channel = pm.Channel,
                                    Reference = pm.Reference
                                })
                                .ToList()
                        })
                        .Where(p => p.Schedules.Any())
                        .ToList()
                })
                .Where(g => g.Policies.Any())
                .ToList();

                var totalItems = customerGroups.Count;
                var pagedGroups = customerGroups
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Cast<dynamic>()
                    .ToList();

                var pagedResult = new PagedResult<dynamic>
                {
                    Items = pagedGroups,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                ViewBag.RouteValues = new Dictionary<string, string>
        {
            { "tab", "schedule" },
            { "search", search ?? "" },
            { "status", status ?? "" },
            { "from_date", from_date?.ToString("yyyy-MM-dd") ?? "" },
            { "to_date", to_date?.ToString("yyyy-MM-dd") ?? "" }
        };

                return View(pagedResult);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi tải lịch thanh toán: " + ex.Message;
                Console.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return View(new PagedResult<dynamic>());
            }
        }

        // Helper method để tính số kỳ từ PremiumPayment
        private int GetPeriodFromPayment(PremiumPayment payment, List<PremiumPayment> allPaymentsForPolicy)
        {
            var orderedPayments = allPaymentsForPolicy.OrderBy(p => p.DueDate).ToList();
            return orderedPayments.IndexOf(payment) + 1;
        }

        // ✅ SỬA LẠI METHOD QuickPay - THÊM VALIDATION THANH TOÁN TUẦN TỰ
        [HttpPost]
        public IActionResult QuickPay([FromBody] QuickPaymentRequest request)
        {
            if (!PermissionHelper.CanManagePayment(User, _context))
            {
                return Json(new { success = false, message = "Bạn không có quyền thanh toán!" });
            }
            if (string.IsNullOrEmpty(request.PaymentId))
            {
                return Json(new { success = false, message = "Thiếu thông tin thanh toán!" });
            }

            try
            {
                // Tìm premium payment
                var payment = _context.PremiumPayments
                    .Find(p => p.Id == request.PaymentId)
                    .FirstOrDefault();

                if (payment == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khoản phí!" });
                }

                if (payment.Status == "paid")
                {
                    return Json(new { success = false, message = "Khoản phí này đã được thanh toán!" });
                }

                // ✅ VALIDATION: Kiểm tra các kỳ trước đã thanh toán chưa
                var allPaymentsForPolicy = _context.PremiumPayments
                    .Find(p => p.PolicyNo == payment.PolicyNo)
                    .SortBy(p => p.DueDate)
                    .ToList();

                var currentPeriod = GetPeriodFromPayment(payment, allPaymentsForPolicy);

                // Lấy các kỳ trước (period < currentPeriod)
                var previousPayments = allPaymentsForPolicy
                    .Take(currentPeriod - 1)
                    .ToList();

                var unpaidPrevious = previousPayments.Where(p => p.Status != "paid").ToList();

                if (unpaidPrevious.Any())
                {
                    var unpaidPeriods = string.Join(", ", unpaidPrevious.Select((p, idx) => idx + 1));
                    return Json(new
                    {
                        success = false,
                        message = $"Bạn phải thanh toán các kỳ trước đó trước! Các kỳ chưa thanh toán: {unpaidPeriods}"
                    });
                }

                // Cập nhật thông tin thanh toán
                var updateDef = Builders<PremiumPayment>.Update
                    .Set(p => p.Status, "paid")
                    .Set(p => p.PaidDate, DateTime.UtcNow)
                    .Set(p => p.Channel, request.Channel ?? "admin")
                    .Set(p => p.PayMethod, request.PayMethod ?? "cash")
                    .Set(p => p.PaymentType, payment.PaymentType ?? "normal")
                    .Set(p => p.PenaltyAmount, payment.PenaltyAmount)
                    .Set(p => p.Reference, request.Reference ?? $"ADMIN-{DateTime.UtcNow:yyyyMMddHHmmss}");

                var result = _context.PremiumPayments.UpdateOne(
                    p => p.Id == request.PaymentId,
                    updateDef
                );

                if (result.ModifiedCount > 0)
                {
                    // Cập nhật PaymentSchedule tương ứng (nếu có)
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
                Console.WriteLine($"[QuickPay Error] {ex.Message}\n{ex.StackTrace}");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // ✅ THÊM CLASS REQUEST
        public class QuickPaymentRequest
        {
            public string PaymentId { get; set; }
            public string Channel { get; set; }
            public string PayMethod { get; set; }
            public string Reference { get; set; }
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
                payment_type = payment.PaymentType,
                penalty_amount = payment.PenaltyAmount,
                related_schedule_id = payment.RelatedScheduleId,  // ✅ SỬA
                channel = payment.Channel,
                pay_method = payment.PayMethod,  // ✅ THÊM
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