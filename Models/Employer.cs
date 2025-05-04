using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebServiceCosmetics.Models;

namespace WebServiceCosmetics.Models
{
    [Table("Employees")]

    public class Employer
    {
        [Key]

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // Автоинкремент для Id

        public int Id { get; set; }
        public string Full_Name { get; set; }
        public int? Position_id { get; set; }

        // Навигационное свойство для связи с единицей измерения
        [ForeignKey("Position_id")]
        [ValidateNever]

        public Positions Positions { get; set; }
   

        public decimal Salary { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public ICollection<ProductSalesModel> Product_Sales { get; set; } = new List<ProductSalesModel>();

    }
}
[Table("Positions")]

public class Positions
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Name { get; set; }
    public ICollection<Employer> Employees { get; set; }
}



