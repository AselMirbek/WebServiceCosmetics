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
        // POST: ProductSales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Product_id,Quantity,Amount,Date")] ProductSalesModel productSalesModel)
        {
            if (ModelState.IsValid)
            {
                // Приведение Amount к типу decimal(10,2)
                decimal salePrice = Math.Round(productSalesModel.Amount, 2);

                // Параметры для хранимой процедуры
                var productIdParam = new SqlParameter("@ProductID", productSalesModel.Product_id);
                var quantityParam = new SqlParameter("@Quantity", productSalesModel.Quantity);
                var salePriceParam = new SqlParameter("@SalePrice", salePrice);

                // Параметры для вывода результата хранимой процедуры
                var resultParam = new SqlParameter("@Result", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };

                var messageParam = new SqlParameter("@Message", SqlDbType.NVarChar, 255)
                {
                    Direction = ParameterDirection.Output
                };

                try
                {
                    // Вызов хранимой процедуры для обработки продажи
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC SP_CheckAndProcessSale @ProductID, @Quantity, @SalePrice, @Result OUTPUT, @Message OUTPUT",
                        productIdParam, quantityParam, salePriceParam, resultParam, messageParam
                    );

                    // Получаем результат из хранимой процедуры
                    bool result = (bool)resultParam.Value;
                    string message = messageParam.Value?.ToString();

                    // Если продажа прошла успешно, добавляем запись в Product_Sales
                    if (result)
                    {
                        var productSale = new ProductSalesModel
                        {
                            Product_id = productSalesModel.Product_id,
                            Quantity = productSalesModel.Quantity,
                            Amount = salePrice,
                            Date = productSalesModel.Date
                        };

                        // Добавляем запись о продаже в таблицу Product_Sales
                        _context.Product_Sales.Add(productSale);
                        await _context.SaveChangesAsync();

                        // Перенаправляем на страницу со списком продаж
                        return RedirectToAction(nameof(Index));
                    }

                    // Если произошла ошибка, выводим сообщение об ошибке
                    ModelState.AddModelError(string.Empty, message);
                }
                catch (SqlException ex)
                {
                    // Логируем ошибку и добавляем сообщение в ModelState
                    ModelState.AddModelError(string.Empty, "Ошибка при выполнении запроса: " + ex.Message);
                }
            }

            // Если модель не прошла валидацию, возвращаем данные в форму
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