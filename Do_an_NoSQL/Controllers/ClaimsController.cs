using Do_an_NoSQL.Database;
using Do_an_NoSQL.Helpers;
using Do_an_NoSQL.Models;
using Do_an_NoSQL.Models.ViewModels;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

[Authorize]
public class ClaimsController : Controller
{
    private readonly MongoDbContext _context;
    private readonly ILogger<ClaimsController> _logger;

    public ClaimsController(MongoDbContext context, ILogger<ClaimsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: List Claims with filtering, sorting, and paging
    public IActionResult Index(
        int page = 1,
        int per_page = 10,
        string? search = null,
        string? status = null,
        string? claim_type = null,
        DateTime? from_date = null,
        DateTime? to_date = null,
        string sort = "submitted_at_desc"
    )
    {
        if (!PermissionHelper.CanViewClaim(User, _context))
        {
            return RedirectToAction("AccessDenied", "Auth");
        }
        var collection = _context.Claims.AsQueryable();

        // Filter
        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim().ToLower();
            collection = collection.Where(x =>
                x.ClaimNo.ToLower().Contains(search) ||
                x.PolicyNo.ToLower().Contains(search) ||
                x.BeneficiaryName.ToLower().Contains(search)
            );
        }

        if (!string.IsNullOrEmpty(status))
            collection = collection.Where(x => x.Status == status);

        if (!string.IsNullOrEmpty(claim_type))
            collection = collection.Where(x => x.ClaimType == claim_type);

        if (from_date.HasValue)
            collection = collection.Where(x => x.SubmittedAt >= from_date.Value);

        if (to_date.HasValue)
            collection = collection.Where(x => x.SubmittedAt <= to_date.Value);

        // Sort
        collection = sort switch
        {
            "submitted_at_asc" => collection.OrderBy(x => x.SubmittedAt),
            "submitted_at_desc" => collection.OrderByDescending(x => x.SubmittedAt),
            "claim_no_asc" => collection.OrderBy(x => x.ClaimNo),
            "claim_no_desc" => collection.OrderByDescending(x => x.ClaimNo),
            _ => collection.OrderByDescending(x => x.SubmittedAt)
        };

        // Count total items
        var totalItems = collection.Count();

        // Pagination + Load Data
        var list = collection.Skip((page - 1) * per_page)
                             .Take(per_page)
                             .ToList();

        // Convert to dynamic
        var dynamicList = list.Select(x => new
        {
            x.Id,
            x.ClaimNo,
            x.PolicyNo,
            x.CustomerId,
            x.BeneficiaryName,
            x.ClaimType,
            x.Status,
            x.SubmittedAt,
            RequestedAmount = x.Payout != null ? x.Payout.RequestedAmount : 0, // Kiểm tra null cho Payout
            ApprovedAmount = x.Payout != null ? x.Payout.ApprovedAmount : 0
        }).Cast<dynamic>().ToList();

        // PagedResult
        var paged = PagedResult<dynamic>.Create(dynamicList, page, per_page, totalItems);

        // ViewBag
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.ClaimType = claim_type;
        ViewBag.FromDate = from_date;
        ViewBag.ToDate = to_date;
        ViewBag.Sort = sort;

        return View(paged);
    }

    // Actions for handling claim details and creation
    [HttpGet]
    public IActionResult GetClaimDetails(string id)
    {
        try
        {
            var claim = _context.Claims.Find(x => x.Id == id).FirstOrDefault();
            if (claim == null)
                return Json(new { success = false, message = "Không tìm thấy yêu cầu bồi thường!" });

            // 🔹 Lấy thêm thông tin hợp đồng liên quan
            var policy = _context.Policies.Find(x => x.PolicyNo == claim.PolicyNo).FirstOrDefault();

            // 🔹 Lấy thêm người thụ hưởng (nếu có)
            var beneficiaries = _context.Beneficiaries
                .Find(b => b.PolicyNo == claim.PolicyNo)
                .ToList();

            return Json(new
            {
                success = true,
                claim = new
                {
                    id = claim.Id,
                    claim_no = claim.ClaimNo,
                    policy_no = claim.PolicyNo,
                    claim_type = claim.ClaimType,
                    status = claim.Status,
                    beneficiaries = beneficiaries.Select(b => new
                    {
                        full_name = b.FullName,
                        relation = b.Relation,
                        share_percent = b.SharePercent,
                        dob = b.Dob,
                        national_id = b.NationalId
                    }).ToList(),
                    policyholder_name = policy?.Customer?.FullName,
                    advisor_name = policy?.Advisor?.FullName,
                    product_name = policy?.Product?.Name,
                    submitted_at = claim.SubmittedAt,
                    payout = new
                    {
                        requested_amount = claim.Payout?.RequestedAmount ?? 0,
                        approved_amount = claim.Payout?.ApprovedAmount ?? 0,
                        paid_amount = claim.Payout?.PaidAmount ?? 0,
                        pay_method = claim.Payout?.PayMethod,
                        paid_at = claim.Payout?.PaidAt,
                        reference = claim.Payout?.Reference
                    },
                    notes = claim.Notes
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi lấy dữ liệu: " + ex.Message });
        }
    }

    // ================================
    // XÓA YÊU CẦU BỒI THƯỜNG ĐƠN LẺ
    // ================================
    [HttpPost]
    public IActionResult Delete(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Mã yêu cầu không hợp lệ." });

            var claim = _context.Claims.Find(x => x.Id == id).FirstOrDefault();
            if (claim == null)
                return Json(new { success = false, message = "Không tìm thấy yêu cầu bồi thường." });

            _context.Claims.DeleteOne(x => x.Id == id);

            return Json(new
            {
                success = true,
                message = $"Đã xóa yêu cầu bồi thường {claim.ClaimNo} thành công."
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DeleteClaim ERROR] " + ex.Message);
            return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }



    // ================================
    // XÓA HÀNG LOẠT YÊU CẦU BỒI THƯỜNG
    // ================================
    [HttpPost]
    public IActionResult BulkDelete([FromBody] object request, bool deleteAll = false)
    {
        try
        {
            if (deleteAll)
            {
                // 🔹 Khi chọn "Tất cả", trừ một số id bị loại trừ
                var json = request.ToJson();
                var parsed = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
                var excludeIds = parsed.Contains("excludeIds")
                    ? parsed["excludeIds"].AsBsonArray.Select(x => x.AsString).ToList()
                    : new List<string>();

                var filter = Builders<Claim>.Filter.Empty;
                if (excludeIds.Any())
                    filter = Builders<Claim>.Filter.Nin(x => x.Id, excludeIds);

                var result = _context.Claims.DeleteMany(filter);
                return Json(new
                {
                    success = true,
                    message = $"Đã xóa {result.DeletedCount} yêu cầu bồi thường (trừ {excludeIds.Count} yêu cầu được giữ lại)."
                });
            }
            else
            {
                // 🔹 Khi chọn riêng từng dòng
                var ids = System.Text.Json.JsonSerializer.Deserialize<List<string>>(request.ToString() ?? "[]");
                if (ids == null || ids.Count == 0)
                    return Json(new { success = false, message = "Không có yêu cầu nào được chọn." });

                var filter = Builders<Claim>.Filter.In(x => x.Id, ids);
                var result = _context.Claims.DeleteMany(filter);

                return Json(new
                {
                    success = true,
                    message = $"Đã xóa {result.DeletedCount} yêu cầu bồi thường thành công."
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[BulkDeleteClaims ERROR] " + ex);
            return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }


    [HttpPost]
    public IActionResult CreateClaimFromPolicy([FromBody] ClaimCreateVM model)
    {
        try
        {
            if (!PermissionHelper.CanManageClaim(User, _context))
            {
                return Json(new { success = false, message = "Bạn không có quyền tạo yêu cầu!" });
            }
            // Kiểm tra tính hợp lệ của model (bao gồm tất cả các trường bắt buộc)
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ.", errors = errors });
            }

            // Lấy thông tin hợp đồng từ PolicyNo
            var policy = _context.Policies.Find(x => x.PolicyNo == model.PolicyNo).FirstOrDefault();
            if (policy == null)
                return Json(new { success = false, message = "Không tìm thấy hợp đồng." });

            // Kiểm tra điều kiện phát hành yêu cầu
            var beneficiary = _context.Beneficiaries
                .Find(b => b.PolicyNo == model.PolicyNo)
                .FirstOrDefault();

            if (beneficiary == null)
                return Json(new { success = false, message = "Không tìm thấy người thụ hưởng trong hợp đồng." });

            // Sinh mã claim mới
            string claimNo = $"CL-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}";

            // Tạo hồ sơ yêu cầu bồi thường (Claim)
            var claim = new Claim
            {
                ClaimNo = claimNo,
                PolicyNo = model.PolicyNo,
                AppNo = policy.AppNo,
                CustomerId = policy.CustomerId,

                BeneficiaryName = beneficiary.FullName,  // Lấy tên người thụ hưởng từ đối tượng Beneficiary
                ClaimType = model.ClaimType,
                EventInfo = new ClaimEventInfo
                {
                    EventDate = model.EventDate,
                    EventPlace = model.EventPlace,
                    Description = model.Description,
                    Cause = model.Cause,
                    RelatedToExclusion = false
                },
                Status = "submitted",  // Trạng thái ban đầu
                SubmittedAt = DateTime.UtcNow,
                Payout = new PayoutInfo
                {
                    RequestedAmount = model.RequestedAmount,  // Sử dụng dữ liệu từ ViewModel
                    ApprovedAmount = 0,
                    PaidAmount = 0
                },
                Documents = model.Documents ?? new List<string>(),  // Lấy danh sách tài liệu (nếu có)
            };

            // Insert hồ sơ yêu cầu vào CSDL
            _context.Claims.InsertOne(claim);

            return Json(new { success = true, message = "Hồ sơ yêu cầu đã được tạo thành công!", claimNo = claimNo });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    [HttpPost]
    public IActionResult ApproveClaim([FromBody] ClaimApprovalVM model)
    {
        try
        {
            var claim = _context.Claims.Find(c => c.Id == model.ClaimId).FirstOrDefault();

            if (claim == null)
                return Json(new { success = false, message = "Không tìm thấy hồ sơ yêu cầu bồi thường." });

            // 🧩 Nếu yêu cầu thẩm định thêm
            if (model.Decision == "under_review")
            {
                claim.Status = "under_review";
                claim.Notes = model.Notes + "\n[System] Hồ sơ yêu cầu thêm chứng từ hoặc giám định y khoa độc lập để xác minh chi tiết vụ việc.";
            }
            else if (model.Decision == "approved")
            {
                claim.Status = "approved";
                claim.Payout = new PayoutInfo
                {
                    RequestedAmount = claim.Payout?.RequestedAmount ?? model.ApprovedAmount,
                    ApprovedAmount = model.ApprovedAmount,
                    PaidAmount = 0,
                    PayMethod = model.PayMethod,
                    PaidAt = null,
                    Reference = claim.ClaimNo + "-PAY-" + DateTime.Now.Ticks
                };
                claim.Notes = model.Notes +
                    "\n[System] Hồ sơ đã được phê duyệt và chuyển sang bộ phận kế toán để chi trả.";
            }
            else if (model.Decision == "rejected")
            {
                claim.Status = "rejected";
                claim.Notes = model.Notes +
                    "\n[System] Hồ sơ bị từ chối chi trả sau quá trình thẩm định.";
            }

            _context.Claims.ReplaceOne(c => c.Id == claim.Id, claim);

            return Json(new
            {
                success = true,
                message = model.Decision switch
                {
                    "approved" => "Đã phê duyệt hồ sơ bồi thường. Đang chờ chi trả.",
                    "under_review" => "Hồ sơ đang được yêu cầu thẩm định thêm. Vui lòng chờ kết quả.",
                    "rejected" => "Đã từ chối hồ sơ bồi thường.",
                    _ => "Trạng thái đã được cập nhật."
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ApproveClaim ERROR] " + ex);
            return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    [HttpPost]
    public IActionResult CreatePayout([FromBody] ClaimPayoutCreateVM data)
    {
        try
        {
            string claimId = data.ClaimId;
            decimal paidAmount = data.PaidAmount;
            string payMethod = data.PayMethod;
            DateTime paidAt = data.PaidAt;
            string reference = data.Reference;

            // 🔎 Lấy thông tin claim
            var claim = _context.Claims.Find(x => x.Id == claimId).FirstOrDefault();
            if (claim == null)
                return Json(new { success = false, message = "Không tìm thấy yêu cầu bồi thường!" });

            // 🧾 Tạo bản ghi chi trả (ClaimPayout)
            var payout = new ClaimPayout
            {
                ClaimNo = claim.ClaimNo,
                RequestedAmount = claim.Payout != null ? claim.Payout.RequestedAmount : 0,
                ApprovedAmount = claim.Payout != null ? claim.Payout.ApprovedAmount : 0,
                PaidAmount = paidAmount,
                PayMethod = payMethod,
                PaidAt = paidAt,
                Reference = reference
            };
            _context.ClaimPayouts.InsertOne(payout);

            // 🧩 Cập nhật lại PayoutInfo trong chính claim
            var payoutInfo = new PayoutInfo
            {
                RequestedAmount = payout.RequestedAmount,
                ApprovedAmount = payout.ApprovedAmount,
                PaidAmount = paidAmount,
                PayMethod = payMethod,
                PaidAt = paidAt,
                Reference = reference
            };

            // ✅ Cập nhật trạng thái claim → "paid"
            var update = Builders<Claim>.Update
                .Set(x => x.Status, "paid")
                .Set(x => x.Payout, payoutInfo)
                .Set(x => x.Notes, (claim.Notes ?? "") + "\n[System] Hồ sơ đã được chi trả đầy đủ.");

            _context.Claims.UpdateOne(x => x.Id == claimId, update);

            return Json(new { success = true, message = "Chi trả quyền lợi thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi tạo chi trả: " + ex.Message });
        }
    }

    [HttpGet]
    public IActionResult ExportExcel(
    string? ids,
    string? excludeIds,
    bool exportAll = false,
    string? search = null,
    string? status = null,
    string? claim_type = null,
    DateTime? from_date = null,
    DateTime? to_date = null
)
    {
        try
        {
            var query = _context.Claims.AsQueryable();

            // ======================
            // ÁP DỤNG BỘ LỌC
            // ======================
            if (exportAll)
            {
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.Trim().ToLower();
                    query = query.Where(x =>
                        x.ClaimNo.ToLower().Contains(search) ||
                        x.PolicyNo.ToLower().Contains(search) ||
                        x.BeneficiaryName.ToLower().Contains(search)
                    );
                }

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(x => x.Status == status);

                if (!string.IsNullOrEmpty(claim_type))
                    query = query.Where(x => x.ClaimType == claim_type);

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
                // Trường hợp không exportAll và không có ids => vẫn xuất theo filter hiện tại
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.Trim().ToLower();
                    query = query.Where(x =>
                        x.ClaimNo.ToLower().Contains(search) ||
                        x.PolicyNo.ToLower().Contains(search) ||
                        x.BeneficiaryName.ToLower().Contains(search)
                    );
                }

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(x => x.Status == status);

                if (!string.IsNullOrEmpty(claim_type))
                    query = query.Where(x => x.ClaimType == claim_type);

                if (from_date.HasValue)
                    query = query.Where(x => x.SubmittedAt >= from_date.Value);

                if (to_date.HasValue)
                    query = query.Where(x => x.SubmittedAt <= to_date.Value);
            }

            var claims = query.ToList();

            // ======================
            // TẠO FILE EXCEL
            // ======================
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Claims");

                // Header
                ws.Cell(1, 1).Value = "Mã yêu cầu";
                ws.Cell(1, 2).Value = "Mã hợp đồng";
                ws.Cell(1, 3).Value = "Người thụ hưởng";
                ws.Cell(1, 4).Value = "Loại yêu cầu";
                ws.Cell(1, 5).Value = "Trạng thái";
                ws.Cell(1, 6).Value = "Ngày nộp";
                ws.Cell(1, 7).Value = "Số tiền yêu cầu (₫)";
                ws.Cell(1, 8).Value = "Số tiền được duyệt (₫)";
                ws.Cell(1, 9).Value = "Số tiền đã chi trả (₫)";
                ws.Cell(1, 10).Value = "Phương thức chi trả";
                ws.Cell(1, 11).Value = "Ngày chi trả";
                ws.Cell(1, 12).Value = "Ghi chú";

                var header = ws.Range(1, 1, 1, 12);
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                header.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                // Dữ liệu
                int row = 2;
                foreach (var c in claims)
                {
                    ws.Cell(row, 1).Value = c.ClaimNo;
                    ws.Cell(row, 2).Value = c.PolicyNo;
                    ws.Cell(row, 3).Value = c.BeneficiaryName;
                    ws.Cell(row, 4).Value = c.ClaimType;
                    ws.Cell(row, 5).Value = c.Status;
                    ws.Cell(row, 6).Value = c.SubmittedAt.ToString("dd/MM/yyyy");

                    ws.Cell(row, 7).Value = c.Payout?.RequestedAmount ?? 0;
                    ws.Cell(row, 8).Value = c.Payout?.ApprovedAmount ?? 0;
                    ws.Cell(row, 9).Value = c.Payout?.PaidAmount ?? 0;

                    ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0 ₫";
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0 ₫";
                    ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0 ₫";

                    ws.Cell(row, 10).Value = c.Payout?.PayMethod ?? "-";
                    ws.Cell(row, 11).Value = c.Payout?.PaidAt?.ToString("dd/MM/yyyy") ?? "-";
                    ws.Cell(row, 12).Value = c.Notes ?? "";

                    row++;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var fileName = $"Claims_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ExportExcelClaims ERROR] " + ex.Message);
            return StatusCode(500, "Đã xảy ra lỗi khi xuất dữ liệu. Vui lòng thử lại.");
        }
    }


}
