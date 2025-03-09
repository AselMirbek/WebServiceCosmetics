
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServiceCosmetics.Models

{
    [Table("Ingredient")]

    public class IngredientModel
    {
        public int Id { get; set; }

        public int? Product_id { get; set; }
        public int Raw_Material_id { get; set; }

        public decimal Quantity { get; set; }
    }
}

