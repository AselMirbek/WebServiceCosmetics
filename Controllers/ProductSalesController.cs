using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebServiceCosmetics.Data;
using WebServiceCosmetics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
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
                                              .Include(p => p.Employees)

                                              .ToListAsync();
            return View(productSales);
        }
        // GET: SaleProducts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var saleProduct = await _context.Product_Sales
                .Include(p => p.Employees)
                .Include(p => p.ProductModel)
                .FirstOrDefaultAsync(m => m.Id == id); // Поиск по ID

            return saleProduct == null ? NotFound() : View(saleProduct);
        }
        // GET: ProductSales/Create
        public IActionResult Create()
        {
            // Загружаем список продуктов и сотрудников для выбора в форме
            ViewBag.Product = new SelectList(_context.Product, "Id", "Name");
            ViewBag.Employees = new SelectList(_context.Employees, "Id", "Full_Name");

            return View();
        }
        // POST: ProductSales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Product_id,Employees_id,Quantity,Amount")] ProductSalesModel productSales)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Product = new SelectList(_context.Product, "Id", "Name", productSales.Product_id);
                ViewBag.Employees = new SelectList(_context.Employees, "Id", "Full_Name", productSales.Employees_id);
                return View(productSales);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Проверка наличия продукта
                await using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.Transaction = transaction.GetDbTransaction();
                    command.CommandText = "CheckProductAvailability";
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int)
                    {
                        Value = productSales.Product_id
                    });

                    command.Parameters.Add(new SqlParameter("@RequiredQuantity", SqlDbType.Decimal)
                    {
                        Value = productSales.Quantity,
                        Precision = 18,
                        Scale = 2
                    });

                    var returnValue = new SqlParameter
                    {
                        ParameterName = "@ReturnVal",
                        DbType = DbType.Int32,
                        Direction = ParameterDirection.ReturnValue
                    };
                    command.Parameters.Add(returnValue);

                    if (command.Connection.State != ConnectionState.Open)
                        await command.Connection.OpenAsync();

                    await command.ExecuteNonQueryAsync();

                    int result = (int)returnValue.Value;
                    if (result != 0)
                    {
                        return BadRequest("Не хватает продукта на складе.");
                    }
                }

                // Получаем цену продукта
                var product = await _context.Product.FindAsync(productSales.Product_id);
                if (product == null)
                    return BadRequest("Товар не найден");

                productSales.Amount = (product.Price / product.Quantity) * productSales.Quantity;

                // Добавляем продажу
                _context.Add(productSales);
                await _context.SaveChangesAsync();

                int saleId = productSales.Id;

                // Обновляем склад и бюджет
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC UpdateStockAndBudget @SaleId = {0}, @ProductId = {1}, @SoldQuantity = {2}",
                    saleId, productSales.Product_id, productSales.Quantity);

                // Фиксируем транзакцию
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Ошибка при выполнении транзакции: {ex.Message}");
            }
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