
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebServiceCosmetics.Data;
using WebServiceCosmetics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
namespace WebServiceCosmetics.Controllers
{
    public class BudgetController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BudgetController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Budget
        public async Task<IActionResult> Index()
        {
          

            var budget = await _context.Budget.FirstOrDefaultAsync();
            if (budget == null)
            {
                budget = new BudgetModel { Amount = 0 };
                _context.Budget.Add(budget);
                await _context.SaveChangesAsync();
            }
            return View(budget);
        }

        // GET: Budget/Edit
        public async Task<IActionResult> Edit()
        {
          


            var budget = await _context.Budget.FirstOrDefaultAsync();
            if (budget == null)
            {
                return NotFound();
            }
            return View(budget);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BudgetModel budgetModel)
        {
            if (ModelState.IsValid)
            {
                var budget = await _context.Budget.FirstOrDefaultAsync();

                if (budget == null)
                {
                    return NotFound();
                }

                // Обновляем поля
                budget.Amount = budgetModel.Amount;
                budget.Persent = budgetModel.Persent;

                _context.Update(budget);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(budgetModel);
        }

    }
}
