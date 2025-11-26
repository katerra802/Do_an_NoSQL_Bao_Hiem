using System;
using System.Collections.Generic;

namespace Do_an_NoSQL.Models.ViewModels
{
    public class ProductCreateVM
    {
        public string? Id { get; set; }
        public string ProductCode { get; set; }          
        public string Name { get; set; }               
        public string Type { get; set; }               
        public decimal MinSumAssured { get; set; }       
        public decimal MaxSumAssured { get; set; }       
        public decimal PremiumRate { get; set; }
        public decimal LatePenaltyRate { get; set; }
        public int GracePeriodDays { get; set; }
        public int MinAge { get; set; }                 
        public int MaxAge { get; set; }              
        public int TermYears { get; set; }              
        public List<string> Purpose { get; set; } = new(); 
        public List<RiderVM> Riders { get; set; } = new();
    }

    public class RiderVM
    {
        public string Code { get; set; } 
        public string Name { get; set; }   
    }
}
