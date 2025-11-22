using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Do_an_NoSQL.Models.ViewModels
{
    public class ClaimCreateVM
    {
        public string? Id { get; set; }

        // Kiểm tra yêu cầu hợp đồng
        [Required(ErrorMessage = "Vui lòng chọn hợp đồng")]
        [Display(Name = "Mã hợp đồng")]
        public string PolicyNo { get; set; }

        // Kiểm tra yêu cầu số tiền
        [Required(ErrorMessage = "Vui lòng nhập số tiền yêu cầu")]
        [Range(1, double.MaxValue, ErrorMessage = "Số tiền yêu cầu phải lớn hơn 0")]
        public decimal RequestedAmount { get; set; }

        [Display(Name = "Người thụ hưởng")]
        public string BeneficiaryName { get; set; }

        // Kiểm tra loại yêu cầu
        [Required(ErrorMessage = "Vui lòng chọn loại yêu cầu")]
        [Display(Name = "Loại yêu cầu")]
        public string ClaimType { get; set; }

        // Kiểm tra ngày sự kiện
        [Required(ErrorMessage = "Vui lòng nhập ngày sự kiện")]
        [Display(Name = "Ngày sự kiện")]
        public DateTime EventDate { get; set; }

        // Kiểm tra địa điểm xảy ra sự kiện
        [Required(ErrorMessage = "Vui lòng nhập địa điểm")]
        [Display(Name = "Địa điểm xảy ra")]
        public string EventPlace { get; set; }

        // Kiểm tra mô tả sự kiện
        [Display(Name = "Mô tả sự kiện")]
        public string? Description { get; set; }

        // Kiểm tra nguyên nhân
        [Display(Name = "Nguyên nhân")]
        public string? Cause { get; set; }

        // Kiểm tra tài liệu đính kèm
        public List<string>? Documents { get; set; }
    }
}
