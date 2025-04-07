using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PdfSharpCore.Drawing;
using WebServiceCosmetics.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebServiceCosmetics.Models
{
    [Table("Raw_Materials")]
    public class RawMaterialModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Автоинкремент

        public int Id { get; set; }
        public string Name { get; set; } = "";
        // Внешний ключ для связи с таблицей Unit
        public int? Unit_id { get; set; }

        // Навигационное свойство для связи с единицей измерения
        [ForeignKey("Unit_id")]
        [ValidateNever]

        public Unit Unit { get; set; }

        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        [Required(ErrorMessage = "Необходимо добавить хотя бы один ингредиент.")]

        public ICollection<IngredientModel> ingredient { get; set; } = new List<IngredientModel>();
        //public ICollection<ProductManufacturingModel> Product_Manufacturing { get; set; } = new List<ProductManufacturingModel>();

        public ICollection<RawMaterialPurchaseModel> Raw_Materials_Purchase { get; set; } = new List<RawMaterialPurchaseModel>();

    }
}

[Table("Units")]
public class Unit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Name { get; set; }
    public ICollection<RawMaterialModel> RawMaterials { get; set; }
    public ICollection<ProductModel> Product { get; set; }

}
