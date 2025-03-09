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
           ViewBag.Units = new SelectList(_context.Units, "Id", "Name");
            return View(new RawMaterialModel());
        }

        // POST: RawMaterials/Create


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RawMaterialModel material)
        {
            // Проверка обязательности Unit_id
            if (material.Unit_id == 0)
            {
                ModelState.AddModelError("Unit_id", "Выберите единицу измерения");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Units = new SelectList(_context.Units, "Id", "Name", material.Unit_id);
                return View(material);
            }

            try
            {
                _context.Add(material);

                // Логируем состояние сущностей перед сохранением
                var entries = _context.ChangeTracker.Entries();
                foreach (var entry in entries)
                {
                    _logger.LogInformation($"Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Сохранение прошло успешно.");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка БД: {ex.Message}");
                _logger.LogError($"Стек ошибки: {ex.StackTrace}");

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
            if (id != material.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(material);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(material);
        }

        // Удалить сырьё
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
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
