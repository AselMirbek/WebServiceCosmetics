using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebServiceCosmetics.Models;

namespace WebServiceCosmetics.Models
{
    [Table("Budget")]

    public class BudgetModel
    {
        [Key]

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Автоинкремент

        public int Id { get; set; }
        public decimal Amount { get; set; }
        public decimal? Persent { get; set; } // процент прибыли при продаже

        public decimal? Bonus { get; set; } // процент прибыли при продаже



    }
}
