using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebServiceCosmetics.Models
{
    [Table("Product_Manufacturing")]

    public class ProductManufacturingModel 
    {
        [Key]

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Автоинкремент

        public int Id { get; set; }

            public int? Product_id { get; set; }
            [ForeignKey("Product_id")]
            [ValidateNever]

            public ProductModel ProductModel { get; set; }
          
            [Range(0, double.MaxValue, ErrorMessage = "Количество должно быть больше или равно 0.")]

            public decimal Quantity { get; set; }
            public DateTime Date { get; set; }



    }


  
}