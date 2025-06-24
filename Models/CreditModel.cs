using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebServiceCosmetics.Models;

namespace WebServiceCosmetics.Models
{
    public class CreditModel
    {
        public int id { get; set; }
        public decimal Amount { get; set; }
        public DateTime StartDate { get; set; }
        public int Years { get; set; }
        public int Months { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public decimal? RemainingAmount { get; set; }
        public decimal? Penalties { get; set; }
        [Required]
        [Display(Name = "Тип платежа")]
        [RegularExpression("Annuity|Differentiated", ErrorMessage = "Допустимые значения: Annuity или Differentiated")]
        public string PaymentType { get; set; } = "Annuity"; // Значение по умолчанию

        public ICollection<PaymentsModel> Payment { get; set; } = new List<PaymentsModel>();

    }
}
