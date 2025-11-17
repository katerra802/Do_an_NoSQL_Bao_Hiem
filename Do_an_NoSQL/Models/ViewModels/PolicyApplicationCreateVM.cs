using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Do_an_NoSQL.Models.ViewModels
{
    public class PolicyApplicationCreateVM
    {
        // ========== DÙNG CHUNG CHO CREATE + EDIT ==========
        public string? Id { get; set; } 

        public string CustomerId { get; set; } = string.Empty;
        public string AdvisorId { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;

        public decimal SumAssured { get; set; }

        public string PremiumMode { get; set; } = string.Empty;
        public string? BasePremium { get; set; }

        public string? Notes { get; set; }

        public string? Status { get; set; } 

        // ========== FILE UPLOAD ==========
        public List<IFormFile>? Documents { get; set; } 

        public List<string>? ExistingFiles { get; set; } 

        public List<string>? RemoveFiles { get; set; }

        public List<BeneficiaryVM> Beneficiaries { get; set; } = new List<BeneficiaryVM>();  // Thêm người thụ hưởng
    }
}
