using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebServiceCosmetics.Data;
using WebServiceCosmetics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

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
                    .Include(r => r.Employees)
                .ToListAsync();
            return View(purchases);
        }
  
        [HttpGet]

        // GET: RawMaterialPurchase/Create
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
            ViewBag.RawMaterials = _context.Raw_Materials.ToList();
            ViewBag.Employees = _context.Employees.ToList(); 

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RawMaterialPurchaseModel purchase)
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
                ViewBag.RawMaterials = _context.Raw_Materials.ToList();
                ViewBag.Employees = _context.Employees.ToList();

                return View(purchase);
            }

            // Проверяем бюджет перед сохранением закупки
            var budget = await _context.Budget.FirstOrDefaultAsync();
            if (budget == null || budget.Amount < purchase.Amount)
            {
                TempData["ErrorMessage"] = "Недостаточно средств в бюджете для закупки!";
                ViewBag.RawMaterials = _context.Raw_Materials.ToList();
                ViewBag.Employees = _context.Employees.ToList();

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
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            if (User.IsInRole("Бухгалтер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
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
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


            if (User.IsInRole("Бухгалтер"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            var purchase = await _context.Raw_Materials_Purchase.FindAsync(id);
            _context.Raw_Materials_Purchase.Remove(purchase);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }





        [HttpGet]
        public IActionResult ExportToWord()
        {
            using var stream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                var purchases = _context.Raw_Materials_Purchase
                    .Include(r => r.RawMaterialModel)
                    .Include(r => r.Employees)
                    .ToList();

                foreach (var purchase in purchases)
                {
                    var para = new Paragraph(
                        new Run(
                            new Text($"ID: {purchase.Id}, Материал: {purchase.RawMaterialModel?.Name}, Кол-во: {purchase.Quantity}, Сумма: {purchase.Amount}, Сотрудник: {purchase.Employees?.Full_Name}")
                        )
                    );
                    body.AppendChild(para);
                }

                mainPart.Document.Save();
            }

            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "RawMaterialPurchases.docx");
        }


        [HttpGet]
        public IActionResult ExportToExcel()
        {
            using var stream = new MemoryStream();
            using (var spreadsheetDoc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = spreadsheetDoc.AddWorkbookPart();
                workbookPart.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new DocumentFormat.OpenXml.Spreadsheet.SheetData();
                worksheetPart.Worksheet = new DocumentFormat.OpenXml.Spreadsheet.Worksheet(sheetData);

                var sheets = spreadsheetDoc.WorkbookPart.Workbook.AppendChild(new DocumentFormat.OpenXml.Spreadsheet.Sheets());
                var sheet = new DocumentFormat.OpenXml.Spreadsheet.Sheet()
                {
                    Id = spreadsheetDoc.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Закупки"
                };
                sheets.Append(sheet);

                // Заголовки
                var headerRow = new DocumentFormat.OpenXml.Spreadsheet.Row();
                headerRow.Append(
                    CreateCell("ID"),
                    CreateCell("Материал"),
                    CreateCell("Количество"),
                    CreateCell("Сумма"),
                    CreateCell("Сотрудник")
                );
                sheetData.AppendChild(headerRow);

                // Данные
                var purchases = _context.Raw_Materials_Purchase
                    .Include(r => r.RawMaterialModel)
                    .Include(r => r.Employees)
                    .ToList();

                foreach (var purchase in purchases)
                {
                    var row = new DocumentFormat.OpenXml.Spreadsheet.Row();
                    row.Append(
                        CreateCell(purchase.Id.ToString()),
                        CreateCell(purchase.RawMaterialModel?.Name ?? ""),
                        CreateCell(purchase.Quantity.ToString()),
                        CreateCell(purchase.Amount.ToString()),
                        CreateCell(purchase.Employees?.Full_Name ?? "")
                    );
                    sheetData.AppendChild(row);
                }

                workbookPart.Workbook.Save();
            }

            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "RawMaterialPurchases.xlsx");
        }

        private DocumentFormat.OpenXml.Spreadsheet.Cell CreateCell(string text)
        {
            return new DocumentFormat.OpenXml.Spreadsheet.Cell
            {
                DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String,
                CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(text)
            };
        }


        public async Task<IActionResult> ExportToPdf()
        {
            var purchases = await _context.Raw_Materials_Purchase.Include(r => r.RawMaterialModel).Include(r => r.Employees).ToListAsync();

            using var stream = new MemoryStream();
            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Verdana", 12);

            double y = 40;
            foreach (var purchase in purchases)
            {
                gfx.DrawString($"Сырье: {purchase.RawMaterialModel?.Name}, Кол-во: {purchase.Quantity}, Сумма: {purchase.Amount}, Сотрудник: {purchase.Employees?.Full_Name}", font, XBrushes.Black, new XRect(20, y, page.Width - 40, page.Height), XStringFormats.TopLeft);
                y += 25;
                if (y > page.Height - 40)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = 40;
                }
            }

            document.Save(stream, false);
            return File(stream.ToArray(), "application/pdf", "RawMaterialPurchases.pdf");
        }
    
    }
}