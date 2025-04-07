using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebServiceCosmetics.Data;
using WebServiceCosmetics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace WebServiceCosmetics.Controllers
{
    public class RawMaterialPurchaseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RawMaterialPurchaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: RawMaterialPurchase
        public async Task<IActionResult> Index()
        {
            var purchases = await _context.Raw_Materials_Purchase
                .Include(r => r.RawMaterialModel)
                .ToListAsync();
            return View(purchases);
        }
        [HttpGet]

        // GET: RawMaterialPurchase/Create
        public IActionResult Create()
        {
            ViewBag.RawMaterials = _context.Raw_Materials.ToList();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RawMaterialPurchaseModel purchase)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RawMaterials = _context.Raw_Materials.ToList();
                return View(purchase);
            }

            // Проверяем бюджет перед сохранением закупки
            var budget = await _context.Budget.FirstOrDefaultAsync();
            if (budget == null || budget.Amount < purchase.Amount)
            {
                TempData["ErrorMessage"] = "Недостаточно средств в бюджете для закупки!";
                ViewBag.RawMaterials = _context.Raw_Materials.ToList();
                return View(purchase); // НЕ СОХРАНЯЕМ, если бюджета недостаточно
            }

            // Уменьшаем бюджет
            budget.Amount -= purchase.Amount;
            _context.Update(budget);

            // Сохраняем закупку сырья
            _context.Add(purchase);

            // Обновляем количество и цену в RawMaterials
            var rawMaterial = await _context.Raw_Materials.FindAsync(purchase.Raw_Material_id);
            if (rawMaterial != null)
            {
                rawMaterial.Quantity += purchase.Quantity;
                rawMaterial.Price += purchase.Amount;
                _context.Update(rawMaterial);
            }

            // Только после всех проверок сохраняем изменения
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> DecreaseBudget(decimal amount)
        {
            // Получаем текущий бюджет
            var budget = await _context.Budget.FirstOrDefaultAsync();
            if (budget == null || budget.Amount < amount)
            {
                return false; // Недостаточно средств
            }

            // Уменьшаем бюджет
            budget.Amount -= amount;
            await _context.SaveChangesAsync();

            return true;
        }

        // GET: RawMaterialPurchase/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var purchase = await _context.Raw_Materials_Purchase.FindAsync(id);
            if (purchase == null)
            {
                return NotFound();
            }
            ViewBag.RawMaterials = _context.Raw_Materials.ToList();
            return View(purchase);
        }

        // POST: RawMaterialPurchase/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RawMaterialPurchaseModel purchase)
        {
            if (id != purchase.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(purchase);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(purchase);
        }

        // GET: RawMaterialPurchase/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var purchase = await _context.Raw_Materials_Purchase.Include(r => r.RawMaterialModel)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (purchase == null)
            {
                return NotFound();
            }

            return View(purchase);
        }

        // POST: RawMaterialPurchase/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var purchase = await _context.Raw_Materials_Purchase.FindAsync(id);
            _context.Raw_Materials_Purchase.Remove(purchase);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}