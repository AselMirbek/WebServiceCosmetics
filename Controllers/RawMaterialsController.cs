using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebServiceCosmetics.Data;
using WebServiceCosmetics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace WebServiceCosmetics.Controllers

{

    public class RawMaterialsController : Controller
    {
         
        private readonly ILogger<RawMaterialsController> _logger;

      

        private readonly ApplicationDbContext _context;

        public RawMaterialsController(ApplicationDbContext context, ILogger<RawMaterialsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: RawMaterials
        public async Task<IActionResult> Index()
        {
            var materials = await _context.Raw_Materials
                .Include(r => r.Unit)
                .ToListAsync();
            return View(materials);
        }


        // GET: RawMaterials/Create
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
            return View(new RawMaterialModel());
        }

        // POST: RawMaterials/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RawMaterialModel material)
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
            if (material.Unit_id == 0)
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

                ViewBag.Units = new SelectList(_context.Units, "Id", "Name", material.Unit_id);
                return View(material);
            }

            try
            {
                _context.Add(material);
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
                ViewBag.Units = new SelectList(_context.Units, "Id", "Name", material.Unit_id);
                return View(material);
            }
        }


        // POST: RawMaterials/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RawMaterialModel material)
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
            if (id != material.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(material);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RawMaterialbExists(material.Id))
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
            ViewBag.Units = new SelectList(_context.Units, "Id", "Name", material.Unit_id);
            return View(material);
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

            var material = await _context.Raw_Materials
                .Include(r => r.Unit) // Подгружаем связанные данные, если нужно
                .FirstOrDefaultAsync(r => r.Id == id);

            if (material == null)
            {
                return NotFound();
            }

            ViewBag.Units = new SelectList(_context.Units, "Id", "Name", material.Unit_id);
            return View(material);
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
            var material = await _context.Raw_Materials.FindAsync(id);
            if (material != null)
            {
                _context.Raw_Materials.Remove(material);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

    }
}
