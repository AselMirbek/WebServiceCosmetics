using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceCosmetics.Data;
using WebServiceCosmetics.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;

namespace WebServiceCosmetics.Controllers
{
    public class IngredientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IngredientController(ApplicationDbContext context)
        {
            _context = context;
        }
        // Действие для отображения всех ингредиентов
        public async Task<IActionResult> Index()
        {
            var ingredients = await _context.Ingredient
                 .Include(r => r.RawMaterialModel)
                .ToListAsync();
            return View(ingredients); // Отправляем данные в представление
        }



        // GET: Ingredient/Create
        [HttpGet]

        public IActionResult Create(int productId)
        {
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (User.IsInRole("Менеджер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
         

            var product = _context.Product.Find(productId); // Находим продукт по ID
            if (product == null)
            {
                return NotFound(); // Если продукт не найден, возвращаем 404
            }
            var rawMaterials = _context.Raw_Materials.ToList(); // Получаем список сырья
            ViewBag.RawMaterials = new SelectList(rawMaterials, "Id", "Name"); // Передаем список сырья в представление
            ViewBag.Product_id = productId; // Передаем ID выбранного продукта
            ViewBag.ProductName = product.Name; // Передаем название продукта

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IngredientModel ingredient)
        {
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (User.IsInRole("Менеджер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
       
            // Проверка, существует ли уже этот ингредиент
            bool exists = await _context.Ingredient.AnyAsync(i =>
                 i.Product_id == ingredient.Product_id &&
                 i.Raw_Material_id == ingredient.Raw_Material_id);

            if (exists)
            {
                ModelState.AddModelError("", "Такой ингредиент уже добавлен для этого продукта!");
            }

            // Валидация количества
            if (ingredient.Quantity != null && decimal.TryParse(ingredient.Quantity.ToString(), out decimal quantity))
            {
                ingredient.Quantity = quantity;
            }
            else
            {
                ModelState.AddModelError("Quantity", "Некорректное значение для количества.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.RawMaterials = new SelectList(_context.Raw_Materials, "Id", "Name");
                var product = await _context.Product.FindAsync(ingredient.Product_id);
                ViewBag.ProductName = product?.Name;
                ViewBag.Product_id = ingredient.Product_id;
                return View(ingredient);
            }
            if (ModelState.IsValid)
            {
                // Если всё валидно, сохраняем ингредиент
                _context.Add(ingredient);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { productId = ingredient.Product_id }); // Перенаправляем на страницу ингредиентов для выбранного продукта
            }


                // Если есть ошибки, возвращаем на форму с теми же данными
                ViewBag.ProductName = _context.Product.FirstOrDefault(p => p.Id == ingredient.Product_id).Name;
            return View(ingredient);
        }


     

        // Получаем ингредиенты для выбранного продукта (для отображения в таблице на главной)
        [HttpGet]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            try
            {
                var ingredients = await _context.Ingredient
                            .Include(i => i.RawMaterialModel)
                            .Where(i => i.Product_id == productId)
                            .Select(i => new
                            {
                                i.Id,
                                Raw_Material_Name = i.RawMaterialModel.Name,
                                i.Quantity
                            })
                            .ToListAsync();

                return Json(ingredients);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error retrieving data", message = ex.Message });
            }
        }

        // GET: Ingredient/Edit/5
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
         

            if (id == null) { return NotFound(); }

            var ingredient = await _context.Ingredient.FindAsync(id);
            if (ingredient == null) { return NotFound(); }
            var product = await _context.Product.FindAsync(ingredient.Product_id);
            ViewBag.ProductName = product?.Name; // Название продукта

            var rawMaterials = await _context.Raw_Materials.ToListAsync();
            ViewBag.RawMaterials = new SelectList(rawMaterials, "Id", "Name", ingredient.Raw_Material_id); // Передаем список сырья и выбираем текущее
            return View(ingredient);
        }

        // POST: Ingredient/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IngredientModel ingredient)
        {
            if (id != ingredient.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(ingredient);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { productId = ingredient.Product_id });
            }
            return View(ingredient);
        }

        [HttpPost]
    public IActionResult Delete(int id)
        {
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (User.IsInRole("Менеджер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            if (User.IsInRole("Технолог"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            var ingredient = _context.Ingredient.FirstOrDefault(i => i.Id == id);
            if (ingredient == null)
            {
                return Json(new { success = false, message = "Ингредиент не найден" });
            }
            var productId = ingredient.Product_id; // Сохраняем ID продукта

            _context.Ingredient.Remove(ingredient);
            _context.SaveChanges();

            return Json(new { success = true, productId = productId });
        }


    }
}
