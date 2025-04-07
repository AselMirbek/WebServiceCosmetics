using Microsoft.EntityFrameworkCore;
using WebServiceCosmetics.Data;
using WebServiceCosmetics.Models;

namespace WebServiceCosmetics.Controllers
{
    public class ManufacturingService
    {
        private readonly ApplicationDbContext _context;

        public ManufacturingService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Проверка наличия сырья для производства
        public bool CheckRawMaterialAvailability(int productId, decimal quantityToProduce)
        {
            // Получаем все ингредиенты для продукта
            var ingredients = _context.Ingredient
                .Where(i => i.Product_id == productId)
                .Include(i => i.RawMaterialModel)
                .ToList();

            foreach (var ingredient in ingredients)
            {
                // Проверка, хватит ли сырья
                decimal requiredQuantity = ingredient.Quantity * quantityToProduce;

                if (ingredient.RawMaterialModel.Quantity < requiredQuantity)
                {
                    return false; // Если хотя бы одного сырья не хватает
                }
            }

            return true; // Если всего хватает
        }

        // Обновление складов после производства
        public void UpdateStockAfterProduction(int productId, decimal quantityToProduce)
        {
            // Получаем все ингредиенты для продукта
            var ingredients = _context.Ingredient
                .Where(i => i.Product_id == productId)
                .Include(i => i.RawMaterialModel)
                .ToList();

            // Обновление склада сырья (уменьшаем количество)
            foreach (var ingredient in ingredients)
            {
                decimal requiredQuantity = ingredient.Quantity * quantityToProduce;
                ingredient.RawMaterialModel.Quantity -= requiredQuantity; // Уменьшаем количество сырья
                _context.Update(ingredient.RawMaterialModel);
            }

            // Обновление склада готовой продукции
            var product = _context.Product.Find(productId);
            if (product != null)
            {
                product.Quantity += quantityToProduce; // Увеличиваем количество готового продукта
                product.Price += quantityToProduce * product.Price; // Увеличиваем сумму (если нужно)
                _context.Update(product);
            }

            _context.SaveChanges();
        }

        // Основной метод для начала производства
        public string ProduceProduct(int productId, decimal quantityToProduce)
        {
            // Проверяем наличие достаточного количества сырья
            if (!CheckRawMaterialAvailability(productId, quantityToProduce))
            {
                return "Не хватает сырья для производства данного количества продукта.";
            }

            // Обновляем склад сырья и готовой продукции
            UpdateStockAfterProduction(productId, quantityToProduce);

            // Создаем запись о производстве
            var manufacturingRecord = new ProductManufacturingModel
            {
                Product_id = productId,
                Quantity = quantityToProduce,
                Date = DateTime.Now,
            };

            _context.Product_Manufacturing.Add(manufacturingRecord);
            _context.SaveChanges();

            return "Производство успешно завершено. Склад сырья и готовой продукции обновлены.";
        }

        public static implicit operator ManufacturingService(ApplicationDbContext v)
        {
            throw new NotImplementedException();
        }
    }

}
