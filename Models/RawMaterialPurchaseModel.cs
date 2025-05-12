using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PdfSharpCore.Drawing;
using WebServiceCosmetics.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace WebServiceCosmetics.Models
{
    [Table("Raw_Materials_Purchase")]

    public class RawMaterialPurchaseModel
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Автоинкремент

        public int Id { get; set; }
        public int? Raw_Material_id { get; set; }
        [ForeignKey("Raw_Material_id")]
        [ValidateNever]

        public RawMaterialModel RawMaterialModel { get; set; }
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public int? Employees_id { get; set; }
        [ForeignKey("Employees_id")]
        [ValidateNever]

        public Employer Employees { get; set; }



    }
}
