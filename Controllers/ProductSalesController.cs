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
using DinkToPdf;
using DinkToPdf.Contracts;
using System.IO;
using ClosedXML.Excel;

using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;


using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

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
        public IActionResult ExportToPdf()
        {
            // Получаем данные о продажах
            var salesData = _context.Product_Sales
                                     .Include(p => p.ProductModel)
                                     .Include(p => p.Employees)
                                     .ToList();

            // Создаем новый PDF-документ
            var pdfDocument = new PdfDocument();
            var page = pdfDocument.AddPage();
            var graphics = XGraphics.FromPdfPage(page);

            // Устанавливаем шрифт
            var font = new XFont("Arial", 12);

            // Добавляем заголовок
            graphics.DrawString("Отчёт по продажам", font, XBrushes.Black, 20, 30);

            // Рисуем таблицу с данными
            int yPosition = 60;
            foreach (var sale in salesData)
            {
                string row = $"{sale.ProductModel.Name} - {sale.Employees.Full_Name} - {sale.Quantity} - {sale.Amount} - {sale.Date.ToShortDateString()}";
                graphics.DrawString(row, font, XBrushes.Black, 20, yPosition);
                yPosition += 20;
            }

            // Сохранение PDF в файл
            var filePath = "SalesReport.pdf";
            pdfDocument.Save(filePath);

            // Отправка PDF
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf", "SalesReport.pdf");
        }
        public IActionResult ExportToExcel()
        {
            var salesData = _context.Product_Sales
                                    .Include(p => p.ProductModel)
                                    .Include(p => p.Employees)
                                    .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Продажи");

                // Заголовки
                worksheet.Cell(1, 1).Value = "Продукт";
                worksheet.Cell(1, 2).Value = "Сотрудник";
                worksheet.Cell(1, 3).Value = "Количество";
                worksheet.Cell(1, 4).Value = "Сумма";
                worksheet.Cell(1, 5).Value = "Дата";

                int row = 2;
                foreach (var sale in salesData)
                {
                    worksheet.Cell(row, 1).Value = sale.ProductModel.Name;
                    worksheet.Cell(row, 2).Value = sale.Employees.Full_Name;
                    worksheet.Cell(row, 3).Value = sale.Quantity;
                    worksheet.Cell(row, 4).Value = sale.Amount;
                    worksheet.Cell(row, 5).Value = sale.Date.ToShortDateString();
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesReport.xlsx");
                }
            }
        }
        public IActionResult ExportToWord()
        {
            var salesData = _context.Product_Sales
                                    .Include(p => p.ProductModel)
                                    .Include(p => p.Employees)
                                    .ToList();

            using (MemoryStream mem = new MemoryStream())
            {
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(mem, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
                {
                    MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    Body body = mainPart.Document.AppendChild(new Body());

                    body.Append(new Paragraph(new Run(new Text("Отчёт по продажам"))) { ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center }) });

                    foreach (var sale in salesData)
                    {
                        var line = $"{sale.Date.ToShortDateString()} | {sale.ProductModel.Name} | {sale.Employees.Full_Name} | {sale.Quantity} | {sale.Amount}";
                        body.Append(new Paragraph(new Run(new Text(line))));
                    }

                }

                return File(mem.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "SalesReport.docx");
            }
        }
    }
}