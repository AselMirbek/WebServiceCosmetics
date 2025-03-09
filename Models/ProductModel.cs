using System.ComponentModel.DataAnnotations.Schema;

namespace WebServiceCosmetics.Models
{
    [Table("Product")]

    public class ProductModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public int Unit_Id { get; set; }



    }
}
