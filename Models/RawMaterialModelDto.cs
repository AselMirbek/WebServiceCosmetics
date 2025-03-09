using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace WebServiceCosmetics.Models.DTO
{
    public class RawMaterialModelDto
    {
        public string Name { get; set; }
        public int? Unit_Id { get; set; }
        public string UnitName { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
