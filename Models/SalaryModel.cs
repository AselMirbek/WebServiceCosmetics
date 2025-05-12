using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebServiceCosmetics.Models
{
    public class SalaryModel
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public int Year { get; set; }
        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        public int? NumberOfPurchases { get; set; }
        public int? NumberOfProductions { get; set; }
        public int? NumberOfSales { get; set; }
        // public int? Common { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int? Common { get; set; }  // Вычисляемое поле

        [Required]
        [Range(0, float.MaxValue)]
        public decimal SalaryAmount { get; set; }

        [Range(0, float.MaxValue)]
        public decimal? Bonus { get; set; }

        [Range(0, float.MaxValue)]
        public decimal? General { get; set; }

        public bool Issued { get; set; }
        [Required]
        public int? Employees_id { get; set; }
        [ForeignKey("Employees_id")]
        [ValidateNever]

        public Employer Employees { get; set; }
    }
}
