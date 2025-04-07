
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebServiceCosmetics.Models

{
    [Table("Ingredient")]

    public class IngredientModel
    {
        public int Id { get; set; }

        public int? Product_id { get; set; }
        [ForeignKey("Product_id")]
        [ValidateNever]

        public ProductModel ProductModel { get; set; }
        public int? Raw_Material_id { get; set; }
        [ForeignKey("Raw_Material_id")]
        [ValidateNever]

        public RawMaterialModel RawMaterialModel { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "Количество должно быть больше или равно 0.")]

        public decimal Quantity { get; set; }
    }
}

