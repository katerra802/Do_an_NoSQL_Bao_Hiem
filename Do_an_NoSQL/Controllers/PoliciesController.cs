using ClosedXML.Excel;
using Do_an_NoSQL.Constants;
using Do_an_NoSQL.Database;
using Do_an_NoSQL.Helpers;
using Do_an_NoSQL.Models;
using Do_an_NoSQL.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OfficeOpenXml;
using System.Globalization;
using static Do_an_NoSQL.Controllers.PolicyApplicationsController;

namespace Do_an_NoSQL.Controllers
{
    [Authorize]
    public class PoliciesController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly ILogger<PoliciesController> _logger;

        public PoliciesController(MongoDbContext context, ILogger<PoliciesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private IMongoCollection<Policy> Collection => _context.Policies;

        // =======================
        // INDEX – LIST + FILTER
        // =======================
        public IActionResult Index(
     int page = 1,
     int per_page = 10,
     string? search = null,
     string? status = null,
     string? product = null,
     string? advisor = null,
     DateTime? from_date = null,
     DateTime? to_date = null,
     decimal? price_from = null,
     decimal? price_to = null,
     string sort = "date_desc"
 )
        {
            if (!PermissionHelper.CanViewPolicy(User, _context))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }
            var collection = _context.Policies.AsQueryable();

            // ===========================
            // SEARCH (tìm mã HĐ, KH, SP, TVV)
            // ===========================
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();

                var matchedCustomers = _context.Customers
                    .Find(x => x.FullName.ToLower().Contains(search))
                    .ToList()
                    .Select(x => x.CustomerCode)
                    .ToList();

                var matchedProducts = _context.Products
                    .Find(x => x.Name.ToLower().Contains(search))
                    .ToList()
                    .Select(x => x.ProductCode)
                    .ToList();

                var matchedAdvisors = _context.Advisors
                    .Find(x => x.FullName.ToLower().Contains(search))
                    .ToList()
                    .Select(x => x.Code)
                    .ToList();

                collection = collection.Where(x =>
                    x.PolicyNo.ToLower().Contains(search) ||
                    matchedCustomers.Contains(x.CustomerId) ||
                    matchedProducts.Contains(x.ProductCode) ||
                    matchedAdvisors.Contains(x.AdvisorId)
                );
            }

            // ===========================
            // FILTER BY PRODUCT
            // ===========================
            if (!string.IsNullOrEmpty(product))
                collection = collection.Where(x => x.ProductCode == product);

            // ===========================
            // FILTER BY ADVISOR (Tư vấn viên)
            // ===========================
            if (!string.IsNullOrEmpty(advisor))
                collection = collection.Where(x => x.AdvisorId == advisor);

            // ===========================
            // FILTER STATUS - DATE - PRICE
            // ===========================
            if (!string.IsNullOrEmpty(status))
                collection = collection.Where(x => x.Status == status);

            if (from_date.HasValue)
                collection = collection.Where(x => x.IssueDate >= from_date.Value);

            if (to_date.HasValue)
                collection = collection.Where(x => x.IssueDate <= to_date.Value);

            if (price_from.HasValue)
                collection = collection.Where(x => x.SumAssured >= price_from.Value);

            if (price_to.HasValue)
                collection = collection.Where(x => x.SumAssured <= price_to.Value);

            // ===========================
            // SORTING
            // ===========================
            collection = sort switch
            {
                "price_asc" => collection.OrderBy(x => x.SumAssured),
                "price_desc" => collection.OrderByDescending(x => x.SumAssured),
                "date_asc" => collection.OrderBy(x => x.IssueDate),
                _ => collection.OrderByDescending(x => x.IssueDate)
            };

            // ===========================
            // LOAD FULL DATA
            // ===========================
            var list = collection.ToList();

            foreach (var p in list)
            {
                p.Customer = _context.Customers.Find(x => x.CustomerCode == p.CustomerId).FirstOrDefault();
                p.Product = _context.Products.Find(x => x.ProductCode == p.ProductCode).FirstOrDefault();
                p.Advisor = _context.Advisors.Find(x => x.Code == p.AdvisorId).FirstOrDefault();
            }

            // ===========================
            // DYNAMIC RETURN
            // ===========================
            var dynamicList = list.Select(x => new
            {
                x.Id,
                x.PolicyNo,
                Customer = x.Customer?.FullName,
                Product = x.Product?.Name,
                Advisor = x.Advisor?.FullName,
                x.SumAssured,
                x.IssueDate,
                x.Status
            }).Cast<dynamic>().ToList();

            var paged = PagedResult<dynamic>.Create(dynamicList, page, per_page);

            // ===========================
            // PASS FILTER VALUES
            // ===========================
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Product = product;
            ViewBag.Advisor = advisor;
            ViewBag.FromDate = from_date;
            ViewBag.ToDate = to_date;
            ViewBag.PriceFrom = price_from;
            ViewBag.PriceTo = price_to;
            ViewBag.Sort = sort;

            // Load Product list for Select2
            ViewBag.Products = _context.Products.Find(_ => true).ToList();
            ViewBag.Advisors = _context.Advisors.Find(_ => true).ToList();

            return View(paged);
        }

        private void GeneratePaymentSchedules(Policy policy, Product product, decimal approvedPremium)
        {
            // Tính số kỳ thanh toán dựa trên premium_mode
            int totalPeriods = policy.PremiumMode switch
            {
                "monthly" => policy.TermYears * 12,           // 12 kỳ/năm
                "quarterly" => policy.TermYears * 4,          // 4 kỳ/năm
                "semi_annually" => policy.TermYears * 2,      // 2 kỳ/năm
                "annually" => policy.TermYears,               // 1 kỳ/năm
                _ => policy.TermYears
            };

            // Tính số tiền mỗi kỳ
            decimal premiumPerPeriod = approvedPremium;

            // Tính số tháng giữa các kỳ
            int monthsInterval = policy.PremiumMode switch
            {
                "monthly" => 1,
                "quarterly" => 3,
                "semi_annually" => 6,
                "annually" => 12,
                _ => 12
            };

            for (int i = 1; i <= totalPeriods; i++)
            {
                // Tính ngày đến hạn cho từng kỳ
                DateTime dueDate = policy.EffectiveDate.AddMonths((i - 1) * monthsInterval);

                // 1. Tạo PaymentSchedule
                var schedule = new PaymentSchedule
                {
                    PolicyNo = policy.PolicyNo,
                    PeriodNo = i,
                    DueDate = dueDate,
                    PremiumDue = premiumPerPeriod,
                    Status = i == 1 && policy.FirstPremiumPaid ? "paid" : "due"
                };
                _context.PaymentSchedules.InsertOne(schedule);

                // 2. Tạo PremiumPayment tương ứng
                var payment = new PremiumPayment
                {
                    PolicyNo = policy.PolicyNo,
                    RelatedScheduleId = schedule.Id,
                    DueDate = dueDate,
                    Amount = premiumPerPeriod,
                    Status = i == 1 && policy.FirstPremiumPaid ? "paid" : "pending",
                    PaymentType = i == 1 ? "initial" : "normal",
                    PenaltyAmount = 0,
                    PaidDate = i == 1 && policy.FirstPremiumPaid ? DateTime.UtcNow : (DateTime?)null,
                    Channel = i == 1 && policy.FirstPremiumPaid ? "advisor" : null,
                    PayMethod = i == 1 && policy.FirstPremiumPaid ? "cash" : null,
                    Reference = i == 1 && policy.FirstPremiumPaid ? $"PAY-{DateTime.UtcNow:yyyy}-{i:D4}" : null,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PremiumPayments.InsertOne(payment);

                _logger.LogInformation($"Created schedule and payment for PolicyNo: {policy.PolicyNo}, Period: {i}/{totalPeriods}");
            }
        }

        [HttpPost]
        public IActionResult CreateFromApplication([FromBody] CreatePolicyRequest request)
        {
            if (!PermissionHelper.CanManagePolicy(User, _context))
            {
                return Ok(new { success = false, message = "Bạn không có quyền phát hành hợp đồng!" });
            }
            try
            {
                // === 1. Lấy hồ sơ yêu cầu ===
                var app = _context.PolicyApplications
                    .Find(x => x.Id == request.AppId)
                    .FirstOrDefault();

                if (app == null)
                    return Ok(new { success = false, message = "Không tìm thấy hồ sơ yêu cầu." });

                // === 2. Kiểm tra điều kiện phát hành ===
                if (!string.IsNullOrEmpty(app.IssuedPolicyNo))
                    return Ok(new { success = false, message = $"Hồ sơ đã được phát hành hợp đồng: {app.IssuedPolicyNo}" });

                if (app.Decision != "approved" && app.Decision != "approved_with_loading")
                    return Ok(new { success = false, message = "Hồ sơ chưa được duyệt hoặc bị từ chối." });

                if (!app.IsFirstPremiumReceived)
                    return Ok(new { success = false, message = "Chưa hoàn tất thu phí bảo hiểm lần đầu. Vui lòng thu phí trước khi phát hành." });

                // === 3. Lấy thông tin liên quan ===
                var product = _context.Products
                    .Find(p => p.ProductCode == app.ProductCode)
                    .FirstOrDefault();

                if (product == null)
                    return Ok(new { success = false, message = "Không tìm thấy thông tin sản phẩm." });

                // === 4. Tạo mã hợp đồng từ app_no ===
                string policyNo = app.AppNo.Replace("APP-", "PL-");

                // === 5. Tính toán phí hàng năm ===
                // === 5. Tính toán phí hàng năm ===
                decimal annualPremium = app.PremiumMode switch
                {
                    "monthly" => (app.ApprovedPremium ?? 0) * 12,
                    "quarterly" => (app.ApprovedPremium ?? 0) * 4,
                    "semi_annually" => (app.ApprovedPremium ?? 0) * 2,  // ✅ THÊM
                    "annually" => app.ApprovedPremium ?? 0,
                    _ => app.ApprovedPremium ?? 0
                };

                // === 6. Tạo hợp đồng mới ===
                var policy = new Policy
                {
                    PolicyNo = policyNo,
                    AppNo = app.AppNo,
                    CustomerId = app.CustomerId,
                    AdvisorId = app.AdvisorId,
                    ProductCode = app.ProductCode,

                    IssueDate = DateTime.Parse(request.IssueDate),
                    EffectiveDate = DateTime.Parse(request.IssueDate),
                    MaturityDate = DateTime.Parse(request.IssueDate).AddYears(product.TermYears),

                    Status = "inforce",
                    ApprovedPremium = app.ApprovedPremium ?? 0,
                    AnnualPremium = annualPremium,
                    PremiumMode = app.PremiumMode,
                    TermYears = product.TermYears,
                    SumAssured = app.SumAssured,

                    FirstPremiumPaid = true,
                    IssueChannel = request.IssueChannel ?? "advisor",
                    PolicyPdf = $"{policyNo}.pdf",
                    Notes = request.Notes,
                    Remark = "Hợp đồng được phát hành sau khi hoàn tất thu phí đầu tiên.",

                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    ModifiedBy = User.Identity?.Name ?? "System",
                    IsLocked = false
                };

                _context.Policies.InsertOne(policy);

                // ✅ === 7. TẠO PAYMENT SCHEDULES VÀ PREMIUM PAYMENTS ===
                GeneratePaymentSchedules(policy, product, app.ApprovedPremium ?? 0);

                // === 8. Cập nhật hồ sơ yêu cầu ===
                var updateDef = Builders<PolicyApplication>.Update 
                    .Set(x => x.Status, "issued")
                    .Set(x => x.IssuedPolicyNo, policyNo);

                _context.PolicyApplications.UpdateOne(x => x.Id == app.Id, updateDef);

                // === 8. Cập nhật người thụ hưởng ===
                var beneficiaryUpdate = Builders<Beneficiary>.Update.Set(x => x.PolicyNo, policyNo);
                _context.Beneficiaries.UpdateMany(b => b.AppNo == app.AppNo, beneficiaryUpdate);

                // === 9. Lấy thông tin người thụ hưởng ===
                var beneficiaries = _context.Beneficiaries
                    .Find(b => b.AppNo == app.AppNo)
                    .ToList();

                // Trả về thông báo và thông tin người thụ hưởng
                return Ok(new
                {
                    success = true,
                    message = "Hợp đồng và lịch thanh toán đã được tạo thành công!",  // ✅ SỬA
                    policyNo = policyNo,
                    issueDate = policy.IssueDate,
                    effectiveDate = policy.EffectiveDate,
                    beneficiaries = beneficiaries.Select(b => new
                    {
                        b.FullName,
                        b.Relation,
                        b.SharePercent,
                        b.Dob,
                        b.NationalId
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        public class CreatePolicyRequest
        {
            public string AppId { get; set; }
            public string IssueDate { get; set; }
            public string IssueChannel { get; set; }
            public string Notes { get; set; }
        }

        [HttpPost]
        public IActionResult UpdatePolicyInfo([FromBody] dynamic body)
        {
            try
            {
                string id = body.Id;
                string advisorName = body.advisor_name;
                string notes = body.notes;
                decimal sumAssured = Convert.ToDecimal(body.sum_assured);
                DateTime issueDate = DateTime.Parse(body.issue_date.ToString());

                var policy = _context.Policies.Find(x => x.Id == id).FirstOrDefault();
                if (policy == null) return NotFound();

                var advisor = _context.Advisors.Find(x => x.FullName == advisorName).FirstOrDefault();

                if (advisor != null)
                    policy.AdvisorId = advisor.Code;

                policy.SumAssured = sumAssured;
                policy.IssueDate = issueDate;
                policy.Notes = notes;
                policy.LastModified = DateTime.Now;

                _context.Policies.ReplaceOne(x => x.Id == id, policy);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "Lỗi khi cập nhật hợp đồng");
            }
        }

        // ===================
        // GetTotalCount
        // ===================
        [HttpGet]
        public IActionResult GetTotalCount(string? status, string? search,
            decimal? price_from, decimal? price_to,
            DateTime? from_date, DateTime? to_date)
        {
            var filter = Builders<Policy>.Filter.Empty;
            var builder = Builders<Policy>.Filter;

            if (!string.IsNullOrEmpty(status))
                filter &= builder.Eq("status", status);

            if (!string.IsNullOrEmpty(search))
                filter &= builder.Regex("policy_no", new MongoDB.Bson.BsonRegularExpression(search, "i"));

            if (price_from.HasValue)
                filter &= builder.Gte("sum_assured", price_from.Value);

            if (price_to.HasValue)
                filter &= builder.Lte("sum_assured", price_to.Value);

            if (from_date.HasValue)
                filter &= builder.Gte("issue_date", from_date.Value);

            if (to_date.HasValue)
                filter &= builder.Lte("issue_date", to_date.Value);

            long count = Collection.CountDocuments(filter);
            return Json(new { count });
        }

        // ===================
        // EXPORT EXCEL
        // ===================
        [HttpGet]
        public IActionResult ExportExcel(
    string? ids,
    string? excludeIds,
    bool exportAll = false,
    string? status = null,
    decimal? price_from = null,
    decimal? price_to = null,
    string? search = null,
    DateTime? from_date = null,
    DateTime? to_date = null)
        {
            try
            {
                var query = _context.Policies.AsQueryable();

                if (exportAll)
                {
                    if (!string.IsNullOrEmpty(status))
                        query = query.Where(x => x.Status == status);
                    if (price_from.HasValue)
                        query = query.Where(x => x.SumAssured >= price_from.Value);
                    if (price_to.HasValue)
                        query = query.Where(x => x.SumAssured <= price_to.Value);
                    if (!string.IsNullOrEmpty(search))
                        query = query.Where(x => x.PolicyNo.Contains(search));
                    if (from_date.HasValue)
                        query = query.Where(x => x.IssueDate >= from_date.Value);
                    if (to_date.HasValue)
                        query = query.Where(x => x.IssueDate <= to_date.Value);

                    if (!string.IsNullOrEmpty(excludeIds))
                    {
                        var excludeList = excludeIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                        query = query.Where(x => !excludeList.Contains(x.Id));
                    }
                }
                else if (!string.IsNullOrEmpty(ids))
                {
                    var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                    query = query.Where(x => idList.Contains(x.Id));
                }

                var policies = query.ToList();

                foreach (var p in policies)
                {
                    p.Customer = _context.Customers.Find(x => x.CustomerCode == p.CustomerId).FirstOrDefault();
                    p.Product = _context.Products.Find(x => x.ProductCode == p.ProductCode).FirstOrDefault();
                    p.Advisor = _context.Advisors.Find(x => x.Code == p.AdvisorId).FirstOrDefault();
                }

                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Policies");

                    ws.Cell(1, 1).Value = "Mã Hợp Đồng";
                    ws.Cell(1, 2).Value = "Khách hàng";
                    ws.Cell(1, 3).Value = "Sản phẩm";
                    ws.Cell(1, 4).Value = "Tư vấn viên";
                    ws.Cell(1, 5).Value = "Số tiền bảo hiểm";
                    ws.Cell(1, 6).Value = "Ngày cấp";
                    ws.Cell(1, 7).Value = "Trạng thái";

                    var headerRange = ws.Range(1, 1, 1, 7);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    int row = 2;
                    foreach (var p in policies)
                    {
                        ws.Cell(row, 1).Value = p.PolicyNo;
                        ws.Cell(row, 2).Value = p.Customer?.FullName ?? "—";
                        ws.Cell(row, 3).Value = p.Product?.Name ?? "—";
                        ws.Cell(row, 4).Value = p.Advisor?.FullName ?? "—";
                        ws.Cell(row, 5).Value = p.SumAssured;
                        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0 ₫";
                        ws.Cell(row, 6).Value = p.IssueDate.ToString("dd/MM/yyyy");
                        ws.Cell(row, 7).Value = p.Status;
                        row++;
                    }

                    ws.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var fileName = $"Policies_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        return File(stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export Error: {ex.Message}");
                return StatusCode(500, "Đã xảy ra lỗi khi xuất dữ liệu.");
            }
        }


        // =======================
        // BULK DELETE
        // =======================
        [HttpPost]
        public IActionResult BulkUpdateStatus(bool updateAll, [FromBody] PolicyStatusUpdate request)
        {
            try
            {
                var update = Builders<Policy>.Update
                    .Set(x => x.Status, request.NewStatus)
                    .Set(x => x.LastModified, DateTime.UtcNow)
                    .Set(x => x.ModifiedBy, User?.Identity?.Name ?? "System")
                    .Set(x => x.Notes, request.Notes ?? "")
                    .Set(x => x.IsLocked, request.IsLocked ?? false)
                    .Set(x => x.LockReason, request.LockReason ?? "");

                if (updateAll)
                {
                    var filter = Builders<Policy>.Filter.Empty;

                    if (request.ExcludeIds != null && request.ExcludeIds.Any())
                        filter &= Builders<Policy>.Filter.Nin(x => x.Id, request.ExcludeIds);

                    var result = _context.Policies.UpdateMany(filter, update);
                    Console.WriteLine($"[DEBUG] BulkUpdate ALL: Matched={result.MatchedCount}, Modified={result.ModifiedCount}");
                }
                else
                {
                    if (request.Ids == null || !request.Ids.Any())
                        return BadRequest(new { message = "Không có ID nào được chọn." });

                    var filter = Builders<Policy>.Filter.In(x => x.Id, request.Ids);
                    var result = _context.Policies.UpdateMany(filter, update);
                    Console.WriteLine($"[DEBUG] BulkUpdate SELECTED: Matched={result.MatchedCount}, Modified={result.ModifiedCount}");
                }

                return Json(new { success = true, message = "Cập nhật trạng thái hàng loạt thành công!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BulkUpdateStatus ERROR] {ex}");
                return StatusCode(500, ex.Message);
            }
        }



        // =======================
        // UPDATE STATUS SINGLE
        // =======================
        [HttpPost]
        public IActionResult UpdateStatus([FromBody] PolicyStatusUpdate request)
        {
            if (string.IsNullOrEmpty(request.Id) || string.IsNullOrEmpty(request.NewStatus))
                return BadRequest(new { message = "Thiếu thông tin bắt buộc." });

            var filter = Builders<Policy>.Filter.Eq(x => x.Id, request.Id);

            var update = Builders<Policy>.Update
                .Set(x => x.Status, request.NewStatus)
                .Set(x => x.LastModified, DateTime.UtcNow)
                .Set(x => x.ModifiedBy, User?.Identity?.Name ?? "System")
                .Set(x => x.Notes, request.Notes ?? "")
                .Set(x => x.IsLocked, request.IsLocked ?? false)
                .Set(x => x.LockReason, request.LockReason ?? "");

            _context.Policies.UpdateOne(filter, update);

            return Json(new { success = true, message = "Cập nhật trạng thái hợp đồng thành công!" });
        }


        // =======================
        // DETAILS for MODAL
        // =======================
        [HttpGet]
        public IActionResult GetPolicyDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Thiếu ID hợp đồng." });

            try
            {
                _logger.LogInformation($"Searching for policy with Id: {id}");
                var policy = _context.Policies.Find(x => x.Id == id).FirstOrDefault();
                if (policy == null)
                    return NotFound(new { message = "Không tìm thấy hợp đồng." });

                // Lấy thông tin liên quan
                policy.Customer = _context.Customers.Find(x => x.CustomerCode == policy.CustomerId).FirstOrDefault();
                policy.Advisor = _context.Advisors.Find(x => x.Code == policy.AdvisorId).FirstOrDefault();
                policy.Product = _context.Products.Find(x => x.ProductCode == policy.ProductCode).FirstOrDefault();

                // Lấy thông tin người thụ hưởng từ PolicyNo thay vì AppNo
                var beneficiaries = _context.Beneficiaries
                    .Find(b => b.PolicyNo == policy.PolicyNo)
                    .ToList();

                // Kiểm tra và trả về thông tin người thụ hưởng chính xác
                return Json(new
                {
                    success = true,
                    policy_no = policy.PolicyNo,
                    app_no = policy.AppNo,
                    customer = policy.Customer != null ? new { full_name = policy.Customer.FullName } : null,
                    advisor = policy.Advisor != null ? new { full_name = policy.Advisor.FullName } : null,
                    product = policy.Product != null ? new { name = policy.Product.Name } : null,
                    issue_date = policy.IssueDate,
                    effective_date = policy.EffectiveDate,
                    maturity_date = policy.MaturityDate,
                    premium_mode = policy.PremiumMode,
                    term_years = policy.TermYears,
                    sum_assured = policy.SumAssured,
                    annual_premium = policy.AnnualPremium,
                    status = policy.Status,
                    notes = policy.Notes,
                    is_locked = policy.IsLocked,
                    lock_reason = policy.LockReason,
                    beneficiaries = beneficiaries.Select(b => new
                    {
                        full_name = b.FullName,
                        relation = b.Relation,
                        share_percent = b.SharePercent,
                        dob = b.Dob,
                        national_id = b.NationalId
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving policy details.");
                return Json(new { success = false, message = "Không tìm thấy hợp đồng." });
            }
        }

    }
}
