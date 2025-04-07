using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebServiceCosmetics.Data;
using WebServiceCosmetics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using Microsoft.Data.SqlClient;
using System.Data;
namespace WebServiceCosmetics.Controllers
{
    public class ProductSalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductSalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ProductSales
        public async Task<IActionResult> Index()
        {
            var productSales = await _context.Product_Sales
                                              .Include(p => p.ProductModel)
                                              .ToListAsync();
            return View(productSales);
        }

        // GET: ProductSales/Create
        public IActionResult Create()
        {
            ViewData["Product_id"] = new SelectList(_context.Product, "Id", "Name"); // Assuming Product has Name property
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Product_id,Quantity,Amount,Date")] ProductSalesModel productSalesModel)
        {
            if (ModelState.IsValid)
            {
                var productIdParam = new SqlParameter("@Product_id", productSalesModel.Product_id);
                var quantityParam = new SqlParameter("@Quantity", productSalesModel.Quantity);
                var amountParam = new SqlParameter("@Amount", productSalesModel.Amount);
                var dateParam = new SqlParameter("@Date", productSalesModel.Date);

                var resultParam = new SqlParameter("@Result", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };

                var messageParam = new SqlParameter("@Message", SqlDbType.NVarChar, 255)
                {
                    Direction = ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC SP_CheckAndInsertSale @Product_id, @Quantity, @Amount, @Date, @Result OUTPUT, @Message OUTPUT",
                    productIdParam, quantityParam, amountParam, dateParam, resultParam, messageParam
                );

                bool result = (bool)resultParam.Value;
                string message = messageParam.Value?.ToString();

                if (result)
                {
                    return RedirectToAction(nameof(Index));
                }

                // если ошибка
                ModelState.AddModelError(string.Empty, message);
            }

            ViewData["Product_id"] = new SelectList(_context.Product, "Id", "Name", productSalesModel.Product_id);
            return View(productSalesModel);
        }


        // GET: ProductSales/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productSalesModel = await _context.Product_Sales.FindAsync(id);
            if (productSalesModel == null)
            {
                return NotFound();
            }
            ViewData["Product_id"] = new SelectList(_context.Product, "Id", "Name", productSalesModel.Product_id);
            return View(productSalesModel);
        }

        // POST: ProductSales/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Product_id,Quantity,Amount,Date")] ProductSalesModel productSalesModel)
        {
            if (id != productSalesModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(productSalesModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductSalesModelExists(productSalesModel.Id))
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
            ViewData["Product_id"] = new SelectList(_context.Product, "Id", "Name", productSalesModel.Product_id);
            return View(productSalesModel);
        }

        // GET: ProductSales/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productSalesModel = await _context.Product_Sales
                .Include(p => p.ProductModel)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (productSalesModel == null)
            {
                return NotFound();
            }

            return View(productSalesModel);
        }

        // POST: ProductSales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productSalesModel = await _context.Product_Sales.FindAsync(id);
            _context.Product_Sales.Remove(productSalesModel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductSalesModelExists(int id)
        {
            return _context.Product_Sales.Any(e => e.Id == id);
        }
    }
}