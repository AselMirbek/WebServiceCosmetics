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
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            if (User.IsInRole("Бухгалтер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
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
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            if (User.IsInRole("Бухгалтер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            // Загружаем список продуктов и сотрудников для выбора в форме
            ViewBag.Product = new SelectList(_context.Product, "Id", "Name");
            ViewBag.Employees = new SelectList(_context.Employees, "Id", "Full_Name");

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Product_id,Employees_id,Quantity")] ProductSalesModel productSales)
        {
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            if (User.IsInRole("Бухгалтер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Product = new SelectList(_context.Product, "Id", "Name", productSales.Product_id);
                ViewBag.Employees = new SelectList(_context.Employees, "Id", "Full_Name", productSales.Employees_id);
                return View(productSales);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Вызываем хранимую процедуру для обработки продажи и вставки данных
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC ProcessProductSale @ProductId = {0}, @SoldQuantity = {1}, @EmployeeId = {2}",
                    productSales.Product_id, productSales.Quantity, productSales.Employees_id);

                await transaction.CommitAsync();

                // Возвращаем пользователя на главную страницу после успешной транзакции
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
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            if (User.IsInRole("Бухгалтер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
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
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            if (User.IsInRole("Бухгалтер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
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
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            if (User.IsInRole("Бухгалтер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
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
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            if (User.IsInRole("Бухгалтер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
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