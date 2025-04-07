using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebServiceCosmetics.Data;
using WebServiceCosmetics.Models;

namespace WebServiceCosmetics.Controllers
{
    public class ProductManufacturingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductManufacturingController> _logger;

        public ProductManufacturingController(ApplicationDbContext context, ILogger<ProductManufacturingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: ProductManufacturing
        public async Task<IActionResult> Index()
        {
            var productManufacturing = _context.Product_Manufacturing.Include(p => p.ProductModel);
            return View(await productManufacturing.ToListAsync());
        }
        // GET: ProductManufacturings/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (id == null || _context.Product == null)
            {
                return NotFound();
            }
            var production = await _context.Product_Manufacturing
                .Include(p => p.ProductModel)
                .FirstOrDefaultAsync(m => m.Product_id == id);

            return production == null ? NotFound() : View(production);
        }
        // GET: ProductManufacturings/Create
        public async Task<IActionResult> Create()
        {
            // Получаем список продуктов
            var products = await _context.Product.ToListAsync();
            // Передаем этот список в ViewBag
            ViewBag.Product_id = new SelectList(products, "Id", "Name");

            return View();
        }


        // POST: ProductManufacturing/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Product_id,Quantity,Date")] ProductManufacturingModel productManufacturing)
        {
            if (!ModelState.IsValid)
                return ReturnInvalidCreateView(productManufacturing);
            

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Product.FindAsync(productManufacturing.Product_id);
                if (product == null)
                    return ProductNotFound();

                var ingredients = await GetIngredientsAsync((int)productManufacturing.Product_id);
                if (!HasSufficientRawMaterials(ingredients, productManufacturing.Quantity))
                    return ReturnInvalidCreateView(productManufacturing);

                var totalCost = ProcessRawMaterials(ingredients, productManufacturing.Quantity);
                UpdateProductStock(product, productManufacturing.Quantity, totalCost);
                _context.Add(productManufacturing);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Успешно произведено {productManufacturing.Quantity} единиц продукции!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при производстве продукта.");
                ModelState.AddModelError(string.Empty, $"Ошибка: {ex.Message}");
                return ReturnInvalidCreateView(productManufacturing);
            }
        }

        // Вспомогательные методы

        private async Task<List<IngredientModel>> GetIngredientsAsync(int Product_id)
        {
            return await _context.Ingredient
                .Include(i => i.RawMaterialModel)
                .Where(i => i.Product_id == Product_id)
                .ToListAsync();
        }

        private bool HasSufficientRawMaterials(List<IngredientModel> ingredients, decimal quantity)
        {
            foreach (var ingredient in ingredients)
            {
                var required = ingredient.Quantity * quantity;
                if (ingredient.RawMaterialModel.Quantity < required)
                {
                    ModelState.AddModelError("Quantity", $"Недостаточно сырья: {ingredient.RawMaterialModel.Name}. Требуется: {required}, доступно: {ingredient.RawMaterialModel.Quantity}");
                    return false;
                }
            }
            return true;
        }

        private decimal ProcessRawMaterials(List<IngredientModel> ingredients, decimal quantity)
        {
            decimal totalCost = 0;
            foreach (var ingredient in ingredients)
            {
                var used = ingredient.Quantity * quantity;
                var raw = ingredient.RawMaterialModel;
                var costPerUnit = raw.Quantity > 0 ? raw.Price / raw.Quantity : 0;
                var cost = (decimal)costPerUnit * used;  // Преобразуем в decimal

                raw.Quantity -= used;
                raw.Price -= cost;

                _context.Update(raw);
                totalCost += cost;
            }
            return totalCost;
        }

        private void UpdateProductStock(ProductModel product, decimal quantity, decimal price)
        {
            product.Quantity += quantity;
            product.Price += price;
            _context.Update(product);
        }

        private IActionResult ReturnInvalidCreateView(ProductManufacturingModel model)
        {
            PopulateViewData(model);
            return View(model);
        }

        private void PopulateViewData(ProductManufacturingModel? model = null)
        {
            ViewData["Product_id"] = new SelectList(_context.Product, "Id", "Name", model?.Product_id);
        }

        private IActionResult ProductNotFound()
        {
            ModelState.AddModelError("Product_id", "Продукт не найден");
            return View();
        }



        // GET: ProductManufacturing/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productManufacturing = await _context.Product_Manufacturing.FindAsync(id);
            if (productManufacturing == null)
            {
                return NotFound();
            }
            ViewData["Product_id"] = new SelectList(_context.Product, "Id", "Name", productManufacturing.Product_id);
            return View(productManufacturing);
        }

        // POST: ProductManufacturing/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Product_id,Quantity,Date")] ProductManufacturingModel productManufacturing)
        {
            if (id != productManufacturing.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(productManufacturing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductManufacturingModelExists(productManufacturing.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["Product_id"] = new SelectList(_context.Product, "Id", "Name", productManufacturing.Product_id);
            return View(productManufacturing);
        }

        // GET: ProductManufacturing/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productManufacturing = await _context.Product_Manufacturing
                .Include(p => p.ProductModel)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (productManufacturing == null)
            {
                return NotFound();
            }

            return View(productManufacturing);
        }

        // POST: ProductManufacturing/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productManufacturing = await _context.Product_Manufacturing.FindAsync(id);
            _context.Product_Manufacturing.Remove(productManufacturing);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductManufacturingModelExists(int id)
        {
            return _context.Product_Manufacturing.Any(e => e.Id == id);
        }
    }
}

