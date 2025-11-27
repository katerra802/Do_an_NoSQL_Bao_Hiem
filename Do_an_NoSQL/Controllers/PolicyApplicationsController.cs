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

namespace Do_an_NoSQL.Controllers
{
    [Authorize]
    public class PolicyApplicationsController : Controller
    {
        private readonly MongoDbContext _context; private readonly ILogger<PolicyApplicationsController> _logger;

        public PolicyApplicationsController(MongoDbContext context, ILogger<PolicyApplicationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ================================
        // AUTO GENERATE APP NO
        // ================================
        private string GenerateAppNo()
        {
            int year = DateTime.Now.Year;

            Random random = new Random();
            int randomNumber = random.Next(0, 1000);  

            string randomAppNo = randomNumber.ToString("D3");  

            var filter = Builders<PolicyApplication>.Filter.Where(x => x.AppNo.StartsWith($"APP-{year}-{randomAppNo}"));
            var existingApp = _context.PolicyApplications.Find(filter).FirstOrDefault();

            while (existingApp != null)
            {
                randomNumber = random.Next(0, 1000);  
                randomAppNo = randomNumber.ToString("D3"); 
                existingApp = _context.PolicyApplications.Find(filter).FirstOrDefault();
            }

            return $"APP-{year}-{randomAppNo}";
        }

        // ================================
        // GET: LIST + FILTER + SORT + PAGING
        // ================================
        public IActionResult Index(
    int page = 1,
    int per_page = 10,
    string? search = null,
    string? status = null,
    string? product = null,  // Thêm tham số product
    string? advisor = null,  // Thêm tham số advisor
    DateTime? from_date = null,
    DateTime? to_date = null,
    decimal? price_from = null,
    decimal? price_to = null,
    string sort = "date_desc"
)
        {
            if (!PermissionHelper.CanViewApplication(User, _context))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }
            var collection = _context.PolicyApplications.AsQueryable();

            // =======================
            // SEARCH chuẩn
            // =======================
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();

                // Tìm customer_id theo tên KH
                var matchedCustomers = _context.Customers
                    .Find(x => x.FullName.ToLower().Contains(search))
                    .ToList()
                    .Select(x => x.CustomerCode)
                    .ToList();

                // Tìm sản phẩm
                var matchedProducts = _context.Products
                    .Find(x => x.Name.ToLower().Contains(search))
                    .ToList()
                    .Select(x => x.ProductCode)
                    .ToList();

                collection = collection.Where(x =>
                    x.AppNo.ToLower().Contains(search) ||
                    matchedCustomers.Contains(x.CustomerId) ||
                    matchedProducts.Contains(x.ProductCode)
                );
            }

            // =======================
            // FILTERS
            // =======================
            if (!string.IsNullOrEmpty(status))
                collection = collection.Where(x => x.Status == status);

            if (!string.IsNullOrEmpty(product))  // Lọc theo sản phẩm
                collection = collection.Where(x => x.ProductCode == product);

            if (!string.IsNullOrEmpty(advisor))  // Lọc theo tư vấn viên
                collection = collection.Where(x => x.AdvisorId == advisor);

            if (from_date.HasValue)
                collection = collection.Where(x => x.SubmittedAt >= from_date.Value);

            if (to_date.HasValue)
                collection = collection.Where(x => x.SubmittedAt <= to_date.Value);

            if (price_from.HasValue)
                collection = collection.Where(x => x.SumAssured >= price_from.Value);

            if (price_to.HasValue)
                collection = collection.Where(x => x.SumAssured <= price_to.Value);

            // =======================
            // SORT
            // =======================
            collection = sort switch
            {
                "price_asc" => collection.OrderBy(x => x.SumAssured),
                "price_desc" => collection.OrderByDescending(x => x.SumAssured),
                "date_asc" => collection.OrderBy(x => x.SubmittedAt),
                _ => collection.OrderByDescending(x => x.SubmittedAt)
            };

            // =======================
            // LOAD EXTRA DATA
            // =======================
            var list = collection.ToList();

            foreach (var app in list)
            {
                app.Customer = _context.Customers
                    .Find(x => x.CustomerCode == app.CustomerId)
                    .FirstOrDefault();

                app.Product = _context.Products
                    .Find(x => x.ProductCode == app.ProductCode)
                    .FirstOrDefault();

                app.Advisor = _context.Advisors
                    .Find(x => x.Code == app.AdvisorId)
                    .FirstOrDefault();
            }

            // =======================
            // DYNAMIC RESULT
            // =======================
            var dynamicList = list.Select(x => new
            {
                x.Id,
                x.AppNo,
                Customer = x.Customer?.FullName,
                Product = x.Product?.Name,
                Advisor = x.Advisor?.FullName,
                x.SumAssured,
                x.BasePremium,
                x.Status,
                x.SubmittedAt
            }).Cast<dynamic>().ToList();

            var paged = PagedResult<dynamic>.Create(dynamicList, page, per_page);

            // =======================
            // PASS FILTER
            // =======================
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Product = product;
            ViewBag.Advisor = advisor;  // Truyền giá trị advisor vào View
            ViewBag.FromDate = from_date;
            ViewBag.ToDate = to_date;
            ViewBag.PriceFrom = price_from;
            ViewBag.PriceTo = price_to;
            ViewBag.Sort = sort;

            // Truyền danh sách sản phẩm và tư vấn viên vào View
            ViewBag.Products = _context.Products.Find(_ => true).ToList();
            ViewBag.Advisors = _context.Advisors.Find(_ => true).ToList();  

            return View(paged);
        }




        [HttpGet]
        public IActionResult GetPolicyApplicationDetails(string id)
        {
            try
            {
                var app = _context.PolicyApplications
                    .Find(x => x.Id == id)
                    .FirstOrDefault();

                if (app == null)
                    return NotFound(new { error = "Application not found" });

                // Load relationships
                app.Customer = _context.Customers
                    .Find(c => c.CustomerCode == app.CustomerId)
                    .FirstOrDefault();

                app.Advisor = _context.Advisors
                    .Find(a => a.Code == app.AdvisorId)
                    .FirstOrDefault();

                app.Product = _context.Products
                    .Find(p => p.ProductCode == app.ProductCode)
                    .FirstOrDefault();

                // Load underwriting
                var uw = _context.UnderwritingDecisions
                    .Find(u => u.AppNo == app.AppNo)
                    .SortByDescending(u => u.DecidedAt)
                    .FirstOrDefault();

                // Load beneficiaries
                var beneficiaries = _context.Beneficiaries
                    .Find(b => b.AppNo == app.AppNo)
                    .ToList();

                var result = new
                {
                    app_no = app.AppNo,
                    customer = app.Customer != null ? new { full_name = app.Customer.FullName } : null,
                    advisor = app.Advisor != null ? new { full_name = app.Advisor.FullName } : null,
                    product = app.Product != null ? new
                    {
                        name = app.Product.Name,
                        term_years = app.Product.TermYears
                    } : null,
                    sum_assured = app.SumAssured,
                    premium_mode = app.PremiumMode,
                    status = app.Status switch
                    {
                        "approved" => "Đã duyệt",
                        "submitted" => "Đã nộp",
                        "under_review" => "Đang xử lý",
                        "rejected" => "Từ chối",
                        _ => app.Status ?? "Không xác định"
                    },
                    submitted_at = app.SubmittedAt,
                    notes = app.Notes ?? "",
                    documents = app.Documents?.Any() == true ? app.Documents : new List<string>(),

                    // Underwriting info
                    underwriting = uw != null ? new
                    {
                        app_no = uw.AppNo,
                        underwriter_id = uw.UnderwriterId,
                        risk_level = uw.RiskLevel,
                        base_premium = app.BasePremium ?? 0,    // ⭐ LẤY TỪ APP
                        extra_premium = uw.ExtraPremium,
                        approved_premium = uw.ApprovedPremium ,
                        medical_required = uw.MedicalRequired,
                        decision = uw.Decision,
                        decided_at = uw.DecidedAt,
                        notes = uw.Notes ?? ""
                    } : null,

                    // Beneficiaries
                    beneficiaries = beneficiaries.Select(b => new
                    {
                        full_name = b.FullName ?? "",
                        relation = b.Relation ?? "",
                        share_percent = b.SharePercent,
                        dob = b.Dob,
                        national_id = b.NationalId ?? ""
                    }).ToList()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetPolicyApplicationDetails: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });  // ⭐ TRẢ CHI TIẾT LỖI
            }
        }

        [HttpPost]
        public IActionResult CalculatePremium([FromBody] PremiumCalcRequest req)
        {
            if (req.SumAssured <= 0 || string.IsNullOrEmpty(req.ProductCode))
                return Json(new { success = false });

            var product = _context.Products.Find(p => p.ProductCode == req.ProductCode).FirstOrDefault();
            if (product == null || product.PremiumRate <= 0)
                return Json(new { success = false, message = "Sản phẩm chưa có rate!" });

            // 1) Annual premium
            decimal annualPremium = req.SumAssured * product.PremiumRate;

            // 2) Mode
            decimal basePremium = req.PremiumMode switch
            {
                "yearly" => annualPremium,
                "halfyear" => annualPremium / 2,
                "quarter" => annualPremium / 4,
                "month" => annualPremium / 12,
                _ => annualPremium
            };

            return Json(new
            {
                success = true,
                basePremium = Math.Round(basePremium, 0)
            });
        }

        public class PremiumCalcRequest
        {
            public decimal SumAssured { get; set; }
            public string PremiumMode { get; set; }
            public string ProductCode { get; set; }
        }


        // ================================
        // GET: CREATE
        // ================================
        public IActionResult Create()
        {
            if (!PermissionHelper.CanManageApplication(User, _context))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }
            ModelState.Clear();
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            ViewBag.Customers = _context.Customers.Find(_ => true).ToList();
            ViewBag.Advisors = _context.Advisors.Find(_ => true).ToList();
            ViewBag.Products = _context.Products.Find(_ => true).ToList();

            return View(new PolicyApplicationCreateVM());
        }


        // ================================
        // POST: CREATE
        // ================================
        [HttpPost]
        public IActionResult Create(PolicyApplicationCreateVM model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

            try
            {
                decimal basePremium = 0;
                if (!string.IsNullOrWhiteSpace(model.BasePremium))
                {
                    string cleaned = model.BasePremium
                        .Replace(".", "")
                        .Replace(",", "")
                        .Replace("đ", "")
                        .Trim();
                    decimal.TryParse(cleaned, out basePremium);
                }

                // Lưu file
                List<string> savedFiles = new();
                if (model.Documents != null)
                {
                    string folder = Path.Combine("wwwroot", "uploads");
                    Directory.CreateDirectory(folder);
                    foreach (var file in model.Documents)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string path = Path.Combine(folder, fileName);
                        using var stream = new FileStream(path, FileMode.Create);
                        file.CopyTo(stream);
                        savedFiles.Add(fileName);
                    }
                }

                // Tạo AppNo
                string appNo = GenerateAppNo();

                // Tạo hồ sơ yêu cầu
                var app = new PolicyApplication
                {
                    AppNo = appNo,
                    CustomerId = model.CustomerId,
                    AdvisorId = model.AdvisorId,
                    ProductCode = model.ProductCode,
                    PremiumMode = model.PremiumMode,
                    SumAssured = model.SumAssured,
                    BasePremium = basePremium,
                    Status = "submitted",
                    Notes = model.Notes,
                    Documents = savedFiles,
                    SubmittedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PolicyApplications.InsertOne(app);

                // ⭐ Lưu người thụ hưởng vào collection riêng
                if (model.Beneficiaries != null && model.Beneficiaries.Count > 0)
                {
                    foreach (var ben in model.Beneficiaries)
                    {
                        var beneficiary = new Beneficiary
                        {
                            AppNo = appNo,          // ⭐ Link với hồ sơ
                            PolicyNo = null,        // ⭐ Chưa có hợp đồng
                            FullName = ben.FullName,
                            Relation = ben.Relation,
                            SharePercent = ben.SharePercent,
                            Dob = ben.Dob,
                            NationalId = ben.NationalId,
                        };

                        _context.Beneficiaries.InsertOne(beneficiary);
                    }
                }

                return Json(new { success = true, message = "Tạo hồ sơ thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating policy application: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        public IActionResult GetBeneficiaries(string appNo)
        {
            var beneficiaries = _context.Beneficiaries.Find(b => b.AppNo == appNo).ToList();
            return View(beneficiaries);  // Trả về danh sách người thụ hưởng
        }


        // ================================
        // GET: EDIT
        // ================================
        public IActionResult Edit(string id)
        {
            if (!PermissionHelper.CanApproveApplication(User, _context))
            {
                return Json(new { success = false, message = "Bạn không có quyền phê duyệt!" });
            }
            var app = _context.PolicyApplications
                .Find(x => x.Id == id)
                .FirstOrDefault();

            if (app == null)
                return NotFound();

            // ✅ Dùng lại PolicyApplicationCreateVM cho form Edit
            var vm = new PolicyApplicationCreateVM
            {
                Id = app.Id,
                CustomerId = app.CustomerId,
                AdvisorId = app.AdvisorId,
                ProductCode = app.ProductCode,
                PremiumMode = app.PremiumMode,
                SumAssured = app.SumAssured,
                Notes = app.Notes,
                ExistingFiles = app.Documents // nếu có field này trong VM
            };

            ViewBag.Customers = _context.Customers.Find(_ => true).ToList();
            ViewBag.Advisors = _context.Advisors.Find(_ => true).ToList();
            ViewBag.Products = _context.Products.Find(_ => true).ToList();

            return View(vm);
        }


        // ================================
        // POST: EDIT
        // ================================
        [HttpPost]
        public IActionResult Edit(PolicyApplicationCreateVM model)
        {
            if (!PermissionHelper.CanApproveApplication(User, _context))
            {
                return Json(new { success = false, message = "Bạn không có quyền phê duyệt!" });
            }
            var app = _context.PolicyApplications
                .Find(x => x.Id == model.Id)
                .FirstOrDefault();

            if (app == null)
                return NotFound();

            // ====== Cập nhật dữ liệu cơ bản ======
            app.CustomerId = model.CustomerId;
            app.AdvisorId = model.AdvisorId;
            app.ProductCode = model.ProductCode;
            app.PremiumMode = model.PremiumMode;
            app.SumAssured = model.SumAssured;
            app.Notes = model.Notes;
            app.Status = model.Status ?? app.Status;

            // ====== Xử lý danh sách file ======
            List<string> files = app.Documents ?? new List<string>(); // Nếu không có file cũ, tạo danh sách rỗng.

            // Lấy danh sách các file cần xóa từ input "RemoveFiles"
            string removeFilesRaw = Request.Form["RemoveFiles"];
            if (!string.IsNullOrEmpty(removeFilesRaw))
            {
                var toRemove = removeFilesRaw.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var remove in toRemove)
                {
                    // Xóa file khỏi danh sách
                    files.Remove(remove.Trim());
                    string path = Path.Combine("wwwroot", "uploads", remove.Trim());

                    // Xóa file trên server
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }
            }

            // ====== Xử lý thêm file mới ======
            if (model.Documents != null && model.Documents.Any())
            {
                string uploadsFolder = Path.Combine("wwwroot", "uploads");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var file in model.Documents)
                {
                    if (file.Length > 0)
                    {
                        string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        string filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }

                        // Thêm file mới vào danh sách
                        files.Add(fileName);
                    }
                }
            }

            // Cập nhật danh sách file sau khi xóa và thêm mới
            app.Documents = files;

            // Lưu lại dữ liệu
            _context.PolicyApplications.ReplaceOne(x => x.Id == app.Id, app);
            return Json(new { success = true, message = "Cập nhật hồ sơ thành công!" });
        }

        [HttpPost]
        public IActionResult Delete(string id)
        {
            if (!PermissionHelper.CanManageApplication(User, _context))
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa!" });
            }
            var app = _context.PolicyApplications.Find(x => x.Id == id).FirstOrDefault();
            if (app != null)
            {
                _context.PolicyApplications.DeleteOne(x => x.Id == id);
                return Json(new { success = true, message = "Hồ sơ đã được xóa thành công!" });
            }

            return Json(new { success = false, message = "Không thể tìm thấy hồ sơ." });
        }

        [HttpPost]
        public IActionResult UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Id))
                    return BadRequest("Invalid request data");

                var app = _context.PolicyApplications.Find(x => x.Id == request.Id).FirstOrDefault();
                if (app == null)
                    return NotFound("Không tìm thấy hồ sơ");

                app.Status = request.NewStatus;
                _context.PolicyApplications.ReplaceOne(x => x.Id == request.Id, app);

                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateStatus ERROR] {ex.Message}");
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        public class UpdateStatusRequest
        {
            public string Id { get; set; }
            public string NewStatus { get; set; }
        }

        [HttpGet]
        public IActionResult GetTotalCount(
    string? search = null,
    string? status = null,
    DateTime? from_date = null,
    DateTime? to_date = null,
    decimal? price_from = null,
    decimal? price_to = null)
        {
            var collection = _context.PolicyApplications.AsQueryable();

            // Áp dụng bộ lọc giống Index
            if (!string.IsNullOrEmpty(search))
                collection = collection.Where(x =>
                    x.AppNo.Contains(search)
                    || (x.Customer != null && x.Customer.FullName.Contains(search))
                );

            if (!string.IsNullOrEmpty(status))
                collection = collection.Where(x => x.Status == status);

            if (from_date.HasValue)
                collection = collection.Where(x => x.SubmittedAt >= from_date.Value);

            if (to_date.HasValue)
                collection = collection.Where(x => x.SubmittedAt <= to_date.Value);

            if (price_from.HasValue)
                collection = collection.Where(x => x.SumAssured >= price_from.Value);

            if (price_to.HasValue)
                collection = collection.Where(x => x.SumAssured <= price_to.Value);

            var totalCount = collection.Count();
            return Json(new { count = totalCount });
        }

        [HttpPost]
        public IActionResult BulkDelete([FromBody] List<string> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest(new { message = "Không có hồ sơ nào được chọn." });

            var filter = Builders<PolicyApplication>.Filter.In(x => x.Id, ids);
            _context.PolicyApplications.DeleteMany(filter);

            return Ok(new { message = "Đã xóa thành công " + ids.Count + " hồ sơ." });
        }

        [HttpPost]
        public IActionResult BulkUpdateStatus([FromBody] BulkUpdateStatusRequest request)
        {
            if (request == null || request.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "Không có hồ sơ nào được chọn." });

            if (string.IsNullOrEmpty(request.NewStatus))
                return BadRequest(new { message = "Thiếu trạng thái mới." });

            var filter = Builders<PolicyApplication>.Filter.In(x => x.Id, request.Ids);
            var update = Builders<PolicyApplication>.Update
                .Set(x => x.Status, request.NewStatus);

            _context.PolicyApplications.UpdateMany(filter, update);

            return Ok(new { message = $"Đã cập nhật {request.Ids.Count} hồ sơ sang trạng thái mới." });
        }

        public class BulkUpdateStatusRequest
        {
            public List<string> Ids { get; set; }
            public string NewStatus { get; set; }
        }


        public IActionResult DownloadTemplate()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "PolicyApplication_Template.xlsx");

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "Không tìm thấy file mẫu" });

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = "PolicyApplication_Template.xlsx";

            return File(fileBytes, contentType, fileName);
        }

        [HttpPost]
        public IActionResult ImportExcel(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1); 

                        foreach (var row in worksheet.RowsUsed().Skip(1))
                        {
                            var customerName = row.Cell(1).GetValue<string>();  
                            var advisorName = row.Cell(2).GetValue<string>();  
                            var sumAssured = row.Cell(3).GetValue<decimal>();   
                            var premiumMode = row.Cell(4).GetValue<string>();   
                            var productName = row.Cell(5).GetValue<string>();  
                            var notes = row.Cell(6).GetValue<string>();         
                            var fileAttachments = row.Cell(7).GetValue<string>(); 

                            var customer = _context.Customers.Find(x => x.FullName == customerName).FirstOrDefault();
                            var advisor = _context.Advisors.Find(x => x.FullName == advisorName).FirstOrDefault();
                            var product = _context.Products.Find(x => x.Name == productName).FirstOrDefault();

                            if (customer == null)
                            {
                                TempData["ToastType"] = "error";
                                TempData["ToastMessage"] = $"Không tìm thấy khách hàng: {customerName}";
                                return RedirectToAction("Index");
                            }

                            if (advisor == null)
                            {
                                TempData["ToastType"] = "error";
                                TempData["ToastMessage"] = $"Không tìm thấy tư vấn viên: {advisorName}";
                                return RedirectToAction("Index");
                            }

                            if (product == null)
                            {
                                TempData["ToastType"] = "error";
                                TempData["ToastMessage"] = $"Không tìm thấy sản phẩm: {productName}";
                                return RedirectToAction("Index");
                            }

                            var appNo = GenerateAppNo();

                            var policyApplication = new PolicyApplication
                            {
                                AppNo = appNo,
                                CustomerId = customer.CustomerCode,
                                AdvisorId = advisor.Code,          
                                ProductCode = product.ProductCode, 
                                SumAssured = sumAssured,           
                                PremiumMode = premiumMode,       
                                Status = "submitted",              
                                Notes = notes,                     
                                SubmittedAt = DateTime.Now,         
                                Documents = string.IsNullOrEmpty(fileAttachments) ? null : fileAttachments.Split(',').ToList()
                            };

                            _context.PolicyApplications.InsertOne(policyApplication); 
                        }
                    }
                }

                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = "Import dữ liệu thành công!";
                return RedirectToAction("Index");
            }

            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Chưa chọn file để import!";
            return RedirectToAction("Index");
        }

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
                var query = _context.PolicyApplications.AsQueryable();

                // Áp dụng các bộ lọc giống như trong phương thức Index
                if (exportAll)
                {
                    if (!string.IsNullOrEmpty(status))
                        query = query.Where(x => x.Status == status);
                    if (price_from.HasValue)
                        query = query.Where(x => x.SumAssured >= price_from.Value);
                    if (price_to.HasValue)
                        query = query.Where(x => x.SumAssured <= price_to.Value);
                    if (!string.IsNullOrEmpty(search))
                        query = query.Where(x => x.AppNo.Contains(search) ||
                            (x.Customer != null && x.Customer.FullName.Contains(search)));
                    if (from_date.HasValue)
                        query = query.Where(x => x.SubmittedAt >= from_date.Value);
                    if (to_date.HasValue)
                        query = query.Where(x => x.SubmittedAt <= to_date.Value);

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
                else
                {
                    if (!string.IsNullOrEmpty(status))
                        query = query.Where(x => x.Status == status);
                    if (price_from.HasValue)
                        query = query.Where(x => x.SumAssured >= price_from.Value);
                    if (price_to.HasValue)
                        query = query.Where(x => x.SumAssured <= price_to.Value);
                    if (!string.IsNullOrEmpty(search))
                        query = query.Where(x => x.AppNo.Contains(search) ||
                            (x.Customer != null && x.Customer.FullName.Contains(search)));
                    if (from_date.HasValue)
                        query = query.Where(x => x.SubmittedAt >= from_date.Value);
                    if (to_date.HasValue)
                        query = query.Where(x => x.SubmittedAt <= to_date.Value);
                }

                var policyApplications = query.ToList();

                // Load related data
                foreach (var app in policyApplications)
                {
                    app.Customer = _context.Customers.Find(x => x.CustomerCode == app.CustomerId).FirstOrDefault();
                    app.Product = _context.Products.Find(x => x.ProductCode == app.ProductCode).FirstOrDefault();
                    app.Advisor = _context.Advisors.Find(x => x.Code == app.AdvisorId).FirstOrDefault();
                }

                // Tạo Excel file
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("PolicyApplications");

                    // Header
                    worksheet.Cell(1, 1).Value = "Mã hồ sơ";
                    worksheet.Cell(1, 2).Value = "Khách hàng";
                    worksheet.Cell(1, 3).Value = "Sản phẩm";
                    worksheet.Cell(1, 4).Value = "Số tiền bảo hiểm";
                    worksheet.Cell(1, 5).Value = "Trạng thái";
                    worksheet.Cell(1, 6).Value = "Ngày nộp";
                    worksheet.Cell(1, 7).Value = "Ghi chú";
                    worksheet.Cell(1, 8).Value = "Tài liệu đính kèm";
                    worksheet.Cell(1, 9).Value = "Tư vấn viên";

                    // Style header
                    var headerRange = worksheet.Range(1, 1, 1, 9);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Data rows
                    int row = 2;
                    foreach (var app in policyApplications)
                    {
                        worksheet.Cell(row, 1).Value = app.AppNo;
                        worksheet.Cell(row, 2).Value = app.Customer?.FullName ?? "Không có khách hàng";
                        worksheet.Cell(row, 3).Value = app.Product?.Name ?? "Không có sản phẩm";
                        worksheet.Cell(row, 4).Value = app.SumAssured;
                        worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0 ₫";
                        worksheet.Cell(row, 5).Value = app.Status;
                        worksheet.Cell(row, 6).Value = app.SubmittedAt.ToString("dd/MM/yyyy");
                        worksheet.Cell(row, 7).Value = app.Notes;
                        worksheet.Cell(row, 8).Value = app.Documents != null ? string.Join(", ", app.Documents) : "Không có tài liệu";
                        worksheet.Cell(row, 9).Value = app.Advisor?.FullName ?? "Không có tư vấn viên";
                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var fileName = $"PolicyApplications_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        return File(stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                return StatusCode(500, "Đã xảy ra lỗi khi xuất dữ liệu. Vui lòng thử lại.");
            }
        }


        [HttpPost]
        public IActionResult ConfirmPremium([FromBody] IdRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Id))
                    return Json(new { success = false, message = "ID không hợp lệ." });

                var app = _context.PolicyApplications
                    .Find(x => x.Id == request.Id)
                    .FirstOrDefault();

                if (app == null)
                    return Json(new { success = false, message = "Không tìm thấy hồ sơ." });

                if (app.Status != "approved")
                    return Json(new { success = false, message = "Hồ sơ chưa được duyệt rủi ro." });


                // UPDATE
                app.Status = "confirmed";
                _context.PolicyApplications.ReplaceOne(x => x.Id == app.Id, app);

                return Json(new { success = true, message = "Khách hàng đã xác nhận phí thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        public class IdRequest
        {
            public string Id { get; set; }
        }


        [HttpPost]
        public IActionResult CollectFirstPremium([FromBody] IdRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Id))
                return Json(new { success = false, message = "ID không hợp lệ." });

            var app = _context.PolicyApplications.Find(x => x.Id == request.Id).FirstOrDefault();
            if (app == null)
                return Json(new { success = false, message = "Không tìm thấy hồ sơ." });

            if (app.Status != "confirmed")
                return Json(new { success = false, message = "Hồ sơ chưa được khách hàng xác nhận phí." });

            // UPDATE
            app.Status = "ready_to_issue";
            app.IsFirstPremiumReceived = true;
            _context.PolicyApplications.ReplaceOne(x => x.Id == app.Id, app);

            return Json(new { success = true, message = "Thu phí bảo hiểm đầu tiên thành công!" });
        }
    
    }

}
