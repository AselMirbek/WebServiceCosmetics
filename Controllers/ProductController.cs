
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebServiceCosmetics.Data;
using WebServiceCosmetics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Security.Claims;


namespace WebServiceCosmetics.Controllers
{
    public class ProductController : Controller
    {

        private readonly ILogger<ProductController> _logger;



        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Product
        public async Task<IActionResult> Index()
        {
            var products = await _context.Product
                .Include(r => r.Unit)
                .ToListAsync();
            return View(products);
        }

      
        // GET: Product/Create
        public IActionResult Create()
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



            ViewBag.Units = new SelectList(_context.Units, "Id", "Name");
            return View(new ProductModel());
        }

        // POST: Product/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductModel product)
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

            _logger.LogInformation("Валидация ModelState началась.");

            // Проверка обязательности Unit_id
            if (product.Unit_id == 0)
            {
                ModelState.AddModelError("Unit_id", "Выберите единицу измерения");
            }

            _logger.LogInformation($"ModelState.IsValid: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors.Select(e => e.ErrorMessage).ToList();
                    foreach (var error in errors)
                    {
                        _logger.LogError($"Ошибка в поле '{key}': {error}");
                    }
                }

                ViewBag.Units = new SelectList(_context.Units, "Id", "Name", product.Unit_id);
                return View(product);
            }

            try
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Сохранение прошло успешно.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка БД: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"InnerException: {ex.InnerException?.Message}");
                }

                ModelState.AddModelError("", "Ошибка при сохранении. Проверьте корректность данных.");
                ViewBag.Units = new SelectList(_context.Units, "Id", "Name", product.Unit_id);
                return View(product);
            }
        }


        // POST: RawMaterials/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductModel product)
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
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RawMaterialbExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            // Обновляем список единиц измерения в случае ошибки валидации
            ViewBag.Units = new SelectList(_context.Units, "Id", "Name", product.Unit_id);
            return View(product);
        }

        // GET: RawMaterials/Edit/5
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
            if (User.IsInRole("Технолог"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product
                .Include(r => r.Unit) // Подгружаем связанные данные, если нужно
                .FirstOrDefaultAsync(r => r.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Units = new SelectList(_context.Units, "Id", "Name", product.Unit_id);
            return View(product);
        }
        private bool RawMaterialbExists(int id)
        {
            return _context.Raw_Materials.Any(e => e.Id == id);
        }

        // Удалить сырьё
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
            if (User.IsInRole("Технолог"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            var product = await _context.Product.FindAsync(id);
            if (product != null)
            {
                _context.Product.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

    }
}
