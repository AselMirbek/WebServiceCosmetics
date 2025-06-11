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
using DinkToPdf;
using DinkToPdf.Contracts;
using System.IO;
using ClosedXML.Excel;

using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using System.Collections.Generic;


using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;

using System.IO;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;
using DocumentFormat.OpenXml.Spreadsheet;

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
            var productManufacturing = _context.Product_Manufacturing.Include(p => p.ProductModel) .Include(p => p.Employees);
            return View(await productManufacturing.ToListAsync());
        }
        // GET: ProductManufacturings/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (User.IsInRole("Менеджер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
         

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
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (User.IsInRole("Менеджер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Получаем список продуктов
            var products = await _context.Product.ToListAsync();
            // Передаем этот список в ViewBag
            ViewBag.Product_id = new SelectList(products, "Id", "Name");
            ViewBag.Employees_id = new SelectList(_context.Employees, "Id", "Full_Name");
            return View();
        }

        // Обработка POST-запроса для создания записи о производстве продукта
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Product_id,Quantity,Date,Employees_id")] ProductManufacturingModel productManufacturing)
        {

            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (User.IsInRole("Менеджер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
          
            // Если модель не прошла валидацию — возвращаем форму создания с ошибками
            if (!ModelState.IsValid)
                return ReturnInvalidCreateView(productManufacturing);

            // Начинаем транзакцию БД
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Получаем продукт по ID
                var product = await _context.Product.FindAsync(productManufacturing.Product_id);
                if (product == null)
                    return ProductNotFound(); // Если не найден — выводим сообщение об ошибке

                // Получаем список ингредиентов продукта
                var ingredients = await GetIngredientsAsync((int)productManufacturing.Product_id);

                // Проверяем, достаточно ли сырья на складе
                if (!HasSufficientRawMaterials(ingredients, productManufacturing.Quantity))
                    return ReturnInvalidCreateView(productManufacturing); // Если нет — ошибка

                // Списываем сырье и вычисляем себестоимость производства
                var totalCost = ProcessRawMaterials(ingredients, productManufacturing.Quantity);

                // Обновляем количество и цену продукта
                UpdateProductStock(product, productManufacturing.Quantity, totalCost);

                // Добавляем новую запись в таблицу производств
                _context.Add(productManufacturing);

                // Сохраняем изменения и коммитим транзакцию
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Показываем пользователю сообщение об успехе
                TempData["SuccessMessage"] = $"Успешно произведено {productManufacturing.Quantity} единиц продукции!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // В случае ошибки откатываем транзакцию
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при производстве продукта.");
                ModelState.AddModelError(string.Empty, $"Ошибка: {ex.Message}");
                return ReturnInvalidCreateView(productManufacturing);
            }
        }

        // Метод получает ингредиенты для указанного продукта
        private async Task<List<IngredientModel>> GetIngredientsAsync(int Product_id)
        {
            return await _context.Ingredient
                .Include(i => i.RawMaterialModel) // Подгружаем связанные данные о сырье
                .Where(i => i.Product_id == Product_id) // Фильтруем по ID продукта
                .ToListAsync();
        }

        // Проверяет, достаточно ли сырья на складе для производства нужного количества продукта
        private bool HasSufficientRawMaterials(List<IngredientModel> ingredients, decimal quantity)
        {
            // Проходим по каждому ингредиенту, входящему в состав продукта

            foreach (var ingredient in ingredients)
            {
                // Вычисляем, сколько единиц данного сырья потребуется: норма на единицу * общее количество продукции
                // Ingredient.Quantit это количество сырья, необходимое на одну единицу продукции.
                //quantity — это сколько штук продукции хочет произвести пользователь.
                //required — итоговое количество каждого сырья, которое нужно списать.
                var required = ingredient.Quantity * quantity; // Сколько нужно сырья
// Сравниваем необходимое количество сырья с тем, что есть в наличии на складе

                if (ingredient.RawMaterialModel.Quantity < required)
                //ingredient.RawMaterialModel.Quantity — сколько фактически имеется на складе.

                {
                    // Если сырья не хватает, добавляем ошибку в ModelState, чтобы показать пользователю сообщение на форме
                    ModelState.AddModelError("Quantity", $"Недостаточно сырья: {ingredient.RawMaterialModel.Name}. Требуется: {required}, доступно: {ingredient.RawMaterialModel.Quantity}");
                    // Возвращаем false — производство невозможно из-за нехватки хотя бы одного ингредиента

                    return false;
                }
            }

            return true; // Все сырья хватает
        }

        // Списывает сырьё и рассчитывает стоимость сырья, использованного в производстве
        private decimal ProcessRawMaterials(List<IngredientModel> ingredients, decimal quantity)
        {
            decimal totalCost = 0;
            // Проходим по каждому ингредиенту, необходимому для производства продукта
            foreach (var ingredient in ingredients)
            {
                // Вычисляем, сколько единиц сырья потребуется: норма на 1 единицу продукта * количество продукции
                var used = ingredient.Quantity * quantity; // Сколько единиц сырья нужно
                // Получаем сам объект сырья (модель сырья)

                var raw = ingredient.RawMaterialModel;
                // // Вычисляем цену за единицу сырья.
                // Если на складе есть сырьё, делим общую стоимость (Price) на количество.
                // Если вдруг количество сырья 0 (в теории может быть), то устанавливаем цену 0, чтобы избежать деления на ноль
                // Расчёт стоимости использованного сырья ведётся на основе средней цены: raw.Price / raw.Quantity.
                //условие? значение_если_истина : значение_если_ложь
                var costPerUnit = raw.Quantity > 0 ? raw.Price / raw.Quantity : 0; // Цена за единицу
                  // Считаем, сколько будет стоить использованное количество сырья

                var cost = (decimal)costPerUnit * used;
                // Списываем сырьё с остатка: уменьшаем количество на использованное
                raw.Quantity -= used; // Списываем сырье
                // Уменьшаем общую стоимость сырья на сумму списанного сырья
               // raw.Price — это общая стоимость всего сырья(а не цена за единицу).


                raw.Price -= cost;    // Уменьшаем стоимость сырья
                // Обновляем данные о сырье в базе данных (контексте)

                _context.Update(raw); 
              // Прибавляем стоимость этого ингредиента к общей стоимости всех материалов
              
                totalCost += cost;   
            }
            // Возвращаем итоговую сумму всех использованных материалов
            //После списания:

           // уменьшается raw.Quantity — сколько физически осталось на складе;

           // уменьшается raw.Price — чтобы отражать новую общую стоимость оставшегося сырья;

            //Метод возвращает суммарную стоимость списанных материалов, которая используется для расчёта цены произведённого товара.
            //себестоимость
            return totalCost;
        }

        // Обновляет количество и цену готовой продукции на складе
        private void UpdateProductStock(ProductModel product, decimal quantity, decimal price)
        {
            // Увеличиваем количество готового продукта на складе на количество произведённого
            //product.Quantity — это общее количество единиц готового продукта, доступных на складе.
            product.Quantity += quantity; // Увеличиваем запас продукта
            //product.Price как общая себестоимость всей партии, а не цена за штуку.
            product.Price += price;       // Увеличиваем общую стоимость (или себестоимость)
            _context.Update(product);     // Обновляем БД
        }

        // Возвращает форму создания с данными и ошибками
        private IActionResult ReturnInvalidCreateView(ProductManufacturingModel model)
        {
            PopulateViewData(model); // Загружаем ViewData
            return View(model);
        }

        // Подготавливает список продуктов для выпадающего списка в форме
        private void PopulateViewData(ProductManufacturingModel? model = null)
        {
            ViewData["Product_id"] = new SelectList(_context.Product, "Id", "Name", model?.Product_id);
        }

        // Добавляет ошибку, если продукт не найден
        private IActionResult ProductNotFound()
        {
            ModelState.AddModelError("Product_id", "Продукт не найден");
            return View();
        }

        // GET-запрос на редактирование записи о производстве продукта
        public async Task<IActionResult> Edit(int? id)
        {
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (User.IsInRole("Менеджер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (id == null)
                return NotFound();

            var productManufacturing = await _context.Product_Manufacturing.FindAsync(id);
            if (productManufacturing == null)
                return NotFound();

            // Загружаем список продуктов для выпадающего списка
            ViewData["Product_id"] = new SelectList(_context.Product, "Id", "Name", productManufacturing.Product_id);
            return View(productManufacturing);
        }

        // POST-запрос на редактирование записи
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Product_id,Quantity,Date")] ProductManufacturingModel productManufacturing)
        {
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (User.IsInRole("Менеджер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
          
            if (id != productManufacturing.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(productManufacturing); // Обновляем запись
                    await _context.SaveChangesAsync();     // Сохраняем изменения
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductManufacturingModelExists(productManufacturing.Id))
                        return NotFound();
                    else
                        throw; // Выбрасываем исключение дальше
                }
                return RedirectToAction(nameof(Index)); // Возвращаемся на главную
            }

            // Если модель не валидна — возвращаем форму с ошибками
            ViewData["Product_id"] = new SelectList(_context.Product, "Id", "Name", productManufacturing.Product_id);
            return View(productManufacturing);
        }

        // GET-запрос на удаление записи
        public async Task<IActionResult> Delete(int? id)
        {
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (User.IsInRole("Менеджер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        
            if (id == null)
                return NotFound();

            var productManufacturing = await _context.Product_Manufacturing
                .Include(p => p.ProductModel) // Загружаем связанный продукт
                .FirstOrDefaultAsync(m => m.Id == id);

            if (productManufacturing == null)
                return NotFound();

            return View(productManufacturing); // Отображаем подтверждение удаления
        }

        // POST-запрос на удаление записи
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (User.IsInRole("Менеджер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
         
            var productManufacturing = await _context.Product_Manufacturing.FindAsync(id);
            _context.Product_Manufacturing.Remove(productManufacturing); // Удаляем
            await _context.SaveChangesAsync(); // Сохраняем изменения
            return RedirectToAction(nameof(Index)); // Возвращаемся на список
        }

        // Проверяет, существует ли запись производства по ID
        private bool ProductManufacturingModelExists(int id)
        {
            return _context.Product_Manufacturing.Any(e => e.Id == id);
        }
       
    }
}
