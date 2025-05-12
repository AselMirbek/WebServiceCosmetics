using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebServiceCosmetics.Models;
namespace WebServiceCosmetics.Models
{
    public class PaymentsModel
    {
      
            public int id { get; set; }
        
              public int? Credit_id { get; set; }

        // Навигационное свойство для связи с единицей измерения
        [ForeignKey("Credit_id")]
        [ValidateNever]

        public CreditModel Credit { get; set; }

        public DateTime PaymentDate { get; set; }
            public decimal PaymentAmount { get; set; }
            public decimal Interest { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal RemainingAmount { get; set; }
            public int? OverdueDays { get; set; }
            public decimal? Penalty { get; set; }
        public bool IsPaid { get; set; } // Оплачен или нет
        public decimal FinalAmount { get; set; }
    }


}
