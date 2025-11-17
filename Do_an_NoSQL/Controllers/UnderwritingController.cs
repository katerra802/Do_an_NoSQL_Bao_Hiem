using Do_an_NoSQL.Models;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;
using Do_an_NoSQL.Models.ViewModels;
using Do_an_NoSQL.Database;

namespace Do_an_NoSQL.Controllers
{
    public class UnderwritingController : Controller
    {
        private readonly MongoDbContext _context;

        public UnderwritingController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult Approve([FromBody] UnderwritingDecisionVM model)
        {
            try
            {
                var app = _context.PolicyApplications
                    .Find(x => x.Id == model.ApplicationId)
                    .FirstOrDefault();

                if (app == null)
                    return NotFound("Không tìm thấy hồ sơ.");

                // ========== 1. LƯU KẾT QUẢ THẨM ĐỊNH ==========
                var decision = new UnderwritingDecision
                {
                    AppNo = app.AppNo,
                    UnderwriterId = "uw01",
                    RiskLevel = model.RiskLevel,

                    BasePremium = model.BasePremium,        // phí gốc (tính theo SA + mode + rate)
                    ExtraPremium = model.ExtraPremium,      // phụ phí theo mức rủi ro
                    ApprovedPremium = model.ApprovedPremium, // phí cuối cùng

                    Decision = model.Decision,             // approved / rejected / approved_with_loading
                    Notes = model.Notes,
                    DecidedAt = DateTime.UtcNow
                };

                _context.UnderwritingDecisions.InsertOne(decision);


                // ========== 2. UPDATE HỒ SƠ YÊU CẦU ==========
                app.Notes = model.Notes;
                app.BasePremium = model.BasePremium;
                app.ApprovedPremium = model.ApprovedPremium;
                app.Decision = model.Decision;
                app.UnderwritingResult = model.RiskLevel;
                app.DecisionDate = DateTime.UtcNow;

                // Nếu bị từ chối → kết thúc
                if (model.Decision == "rejected")
                {
                    app.Status = "rejected";
                    app.Notes += "\n[System] Hồ sơ bị từ chối, không được phép phát hành hợp đồng.";
                }
                else
                {
                    // ⭐⭐⭐ CHUẨN NGHIỆP VỤ: Sau thẩm định → chờ KH xác nhận phí
                    app.Status = "approved";
                }

                _context.PolicyApplications.ReplaceOne(x => x.Id == app.Id, app);


                return Json(new
                {
                    success = true,
                    message = app.Status == "rejected"
                        ? "Hồ sơ đã bị từ chối và được khóa."
                        : "Đã cập nhật kết quả thẩm định. Chờ khách hàng xác nhận phí."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Underwriting Approve ERROR] {ex}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
