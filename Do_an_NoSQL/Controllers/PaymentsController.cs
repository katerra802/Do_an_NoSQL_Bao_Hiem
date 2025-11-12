using Microsoft.AspNetCore.Mvc;
using Do_an_NoSQL.Models;

namespace Do_an_NoSQL.Controllers
{
    public class PaymentsController : Controller
    {
        // GET: Payments
        public IActionResult Index()
        {
            // TODO: Load payments from database
            return View();
        }

        // GET: Payments/PremiumDetails/5
        public IActionResult PremiumDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // TODO: Load premium payment from database
            // var payment = _premiumPaymentService.GetById(id);
            // if (payment == null) return NotFound();

            // Mock data
            var payment = new PremiumPayment
            {
                Id = id,
                PolicyNo = "POL2024001",
                DueDate = new DateTime(2024, 10, 15),
                PaidDate = null,
                Amount = 5500000,
                Status = "Pending",
                Channel = "",
                Reference = ""
            };

            return View(payment);
        }

        // GET: Payments/ClaimDetails/5
        public IActionResult ClaimDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // TODO: Load claim payout from database
            // var claim = _claimPayoutService.GetById(id);
            // if (claim == null) return NotFound();

            // Mock data
            var claim = new ClaimPayout
            {
                Id = id,
                ClaimNo = "CL2024001",
                ApprovedAmount = 15000000,
                PaidAmount = 0,
                PayMethod = "",
                PaidAt = null,
                Reference = ""
            };

            return View(claim);
        }

        // POST: Payments/UpdatePremiumStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdatePremiumStatus(string id, string status, string channel, string reference)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                // TODO: Update premium payment in database
                // var payment = _premiumPaymentService.GetById(id);
                // payment.Status = status;
                // payment.Channel = channel;
                // payment.Reference = reference;
                // if (status == "Paid")
                // {
                //     payment.PaidDate = DateTime.UtcNow;
                // }
                // _premiumPaymentService.Update(id, payment);

                TempData["SuccessMessage"] = "Cập nhật trạng thái thanh toán phí thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(PremiumDetails), new { id });
            }
        }

        // POST: Payments/UpdateClaimStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateClaimStatus(string id, string payMethod, decimal paidAmount, string reference)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                // TODO: Update claim payout in database
                // var claim = _claimPayoutService.GetById(id);
                // claim.PayMethod = payMethod;
                // claim.PaidAmount = paidAmount;
                // claim.Reference = reference;
                // claim.PaidAt = DateTime.UtcNow;
                // _claimPayoutService.Update(id, claim);

                TempData["SuccessMessage"] = "Cập nhật trạng thái bồi thường thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(ClaimDetails), new { id });
            }
        }
    }
}