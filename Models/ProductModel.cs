﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebServiceCosmetics.Models;

namespace WebServiceCosmetics.Models
{
    [Table("Product")]

    public class ProductModel
    {
        [Key]

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Автоинкремент

        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }

        public int? Unit_id { get; set; }

        // Навигационное свойство для связи с единицей измерения
        [ForeignKey("Unit_id")]
        [ValidateNever]

        public Unit Unit { get; set; }
        [Required(ErrorMessage = "Необходимо добавить хотя бы один ингредиент.")]

        public ICollection<IngredientModel> ingredient { get; set; } = new List<IngredientModel>();


    }
}
