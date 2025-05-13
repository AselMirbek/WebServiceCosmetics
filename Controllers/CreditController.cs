using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceCosmetics.Data;
using WebServiceCosmetics.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using System.IO;
using Run = DocumentFormat.OpenXml.Spreadsheet.Run;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace WebServiceCosmetics.Controllers
{
    public class CreditController : Controller
    {
        private readonly ApplicationDbContext _context;
        private List<PaymentsModel> payment;

        // Конструктор для внедрения зависимости контекста базы данных
        public CreditController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CreditController
        public async Task<ActionResult> Index()
        {
            // Получаем список всех кредитов из базы данных
            var credits = await _context.Credit.ToListAsync();
            return View(credits); // Отображаем список кредитов
        }

        [HttpGet("Credit/Details/{id}")]
        public IActionResult Details(int id)
        {
            var credit = _context.Credit
                .Include(c => c.Payment)
                .FirstOrDefault(c => c.id == id);

            if (credit == null)
            {
                return NotFound();
            }

            var viewModel = new CreditDetailsViewModel
            {
                Credit = credit,
                Payment = (List<PaymentsModel>)credit.Payment
            };
            return View(viewModel);
        }
     

        // GET: CreditController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CreditController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreditModel newCredit)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Credit.Add(newCredit);
                    await _context.SaveChangesAsync();

                    // Вызов хранимой процедуры для создания графика платежей
                    var parameters = new[]
                    {
                new SqlParameter("@CreditId", newCredit.id),
                new SqlParameter("@LoanAmount", newCredit.Amount),
                new SqlParameter("@AnnualInterestRate", newCredit.AnnualInterestRate),
                new SqlParameter("@LoanTermYears", newCredit.Years),
                new SqlParameter("@StartDate", newCredit.StartDate)
            };

                    await _context.Database.ExecuteSqlRawAsync("EXEC CreateCreditWithSchedule @CreditId, @LoanAmount, @AnnualInterestRate, @LoanTermYears, @StartDate", parameters);

                    // Обновление бюджета
                    await _context.Database.ExecuteSqlRawAsync("EXEC UpdateBudgetAmountCredit @TransactionType = {0}, @Amount = {1}", "Credit", newCredit.Amount);

                    TempData["Message"] = "Кредит успешно создан, и график платежей сгенерирован.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Message"] = $"Ошибка: {ex.Message}";
                    return RedirectToAction(nameof(Create));

                }
            }

            return View(newCredit);
        }





        // Метод поиска платежей по дате (для демонстрации поиска)
        public async Task<IActionResult> SearchPayments(DateTime startDate, DateTime endDate)
        {
            var payments = await _context.Payment
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .ToListAsync();

            return View(payments); // Отображаем платежи, попавшие в указанный диапазон
        }

        [HttpGet("Credit/Details/{id}/{startDate}/{endDate}")]
        public IActionResult Details(int id, DateTime startDate, DateTime endDate)
        {
            // Вызываем хранимую процедуру для получения платежей за период
            var payments = _context.Payment
                .FromSqlRaw("EXEC GetCreditPaymentsByDate @StartDate, @EndDate",
                            new SqlParameter("@StartDate", startDate),
                            new SqlParameter("@EndDate", endDate))
                .ToList();

            var credit = _context.Credit
                .Include(c => c.Payment)
                .FirstOrDefault(c => c.id == id);

            if (credit == null)
            {
                return NotFound();
            }

            // Создаем модель для передачи в представление
            var viewModel = new CreditDetailsViewModel
            {
                Credit = credit,
                Payment = payment
            };

            return View(viewModel);
        }

        // Действие для оплаты
        [HttpPost]
        public async Task<IActionResult> Pay(int paymentId)
        {
            var payment = await _context.Payment.FirstOrDefaultAsync(p => p.id == paymentId);

            if (payment != null && !payment.IsPaid)
            {
                payment.IsPaid = true;
                payment.PaymentDate = DateTime.Now; // Устанавливаем дату платежа
                                                    // Обновляем остаток по платежу, если необходимо

                // Обновляем остаток кредита
                var credit = await _context.Credit.FirstOrDefaultAsync(c => c.id == payment.Credit_id);
                if (credit != null)
                {
                    credit.RemainingAmount -= payment.PaymentAmount; // Уменьшаем остаток кредита на сумму платежа
                    try
                    {
                        // Выполняем хранимую процедуру обновления бюджета
                        await _context.Database.ExecuteSqlRawAsync("EXEC UpdateBudgetAmountCredit @TransactionType = {0}, @Amount = {1}", "Payment", payment.PaymentAmount);
                    }
                    catch (Exception ex)
                    {
                        // Если произошла ошибка (например, недостаточно средств в бюджете), выводим сообщение
                        TempData["Message"] = $"Ошибка: {ex.Message}";
                        return RedirectToAction("Details", new { id = payment.Credit_id });
                    }
                }


                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = payment.Credit_id });
        }
        [HttpGet("Credit/Details/ExportToWord/{id}")]

        public IActionResult ExportToWord(int id)
        {
            var credit = _context.Credit
                .Include(c => c.Payment)
                .FirstOrDefault(c => c.id == id);

            if (credit == null)
            {
                return NotFound();
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());

                    var body = mainPart.Document.Body;

                    // Заголовок документа
                    var title = new Paragraph(
                        new DocumentFormat.OpenXml.Drawing.Run(
                            new DocumentFormat.OpenXml.Math.Text($"Кредит № {credit.id}")));
                    body.Append(title);

                    // Информация о кредите
                    var creditDetails = new Paragraph(
                        new Run(
                            new DocumentFormat.OpenXml.Math.Text($"Сумма кредита: {credit.Amount}")));

                    body.Append(creditDetails);

                    // Платежи по кредиту
                    var paymentsParagraph = new Paragraph(
                        new Run(
                            new DocumentFormat.OpenXml.Math.Text("Платежи по кредиту:")));
                    body.Append(paymentsParagraph);

                    foreach (var payment in credit.Payment)
                    {
                        var paymentDetails = new Paragraph(
                            new Run(
                                new DocumentFormat.OpenXml.Math.Text($"Дата платежа: {payment.PaymentDate}, Сумма: {payment.PaymentAmount}")));
                        body.Append(paymentDetails);
                    }

                    wordDocument.Save();
                }

                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "CreditDetails.docx");
            }
        }
        [HttpGet("Credit/ExportToExcel/{id}")]

        public IActionResult ExportToExcel(int id)
        {
            var credit = _context.Credit
                .Include(c => c.Payment)
                .FirstOrDefault(c => c.id == id);

            if (credit == null)
            {
                return NotFound();
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();
                    WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    worksheetPart.Worksheet = new Worksheet(new SheetData());

                    Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
                    Sheet sheet = new Sheet() { Name = "Кредитные данные", SheetId = 1, Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart) };
                    sheets.Append(sheet);

                    SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                    // Заголовки столбцов
                    Row headerRow = new Row();
                    headerRow.Append(
                        new Cell() { CellValue = new CellValue("Кредит №"), DataType = CellValues.String },
                        new Cell() { CellValue = new CellValue("Сумма кредита"), DataType = CellValues.String },
                        new Cell() { CellValue = new CellValue("Дата платежа"), DataType = CellValues.String },
                        new Cell() { CellValue = new CellValue("Сумма платежа"), DataType = CellValues.String }
                    );
                    sheetData.Append(headerRow);

                    // Данные по кредиту
                    foreach (var payment in credit.Payment)
                    {
                        Row row = new Row();
                        row.Append(
                            new Cell() { CellValue = new CellValue(credit.id.ToString()), DataType = CellValues.Number },
                            new Cell() { CellValue = new CellValue(credit.Amount.ToString()), DataType = CellValues.Number },
                            new Cell() { CellValue = new CellValue(payment.PaymentDate.ToString("yyyy-MM-dd")), DataType = CellValues.String },
                            new Cell() { CellValue = new CellValue(payment.PaymentAmount.ToString()), DataType = CellValues.Number }
                        );
                        sheetData.Append(row);
                    }

                    spreadsheetDocument.Save();
                }

                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CreditDetails.xlsx");
            }

        }
        [HttpGet("Credit/ExportToPdf/{id}")]

        public IActionResult ExportToPdf(int id)
        {
            var credit = _context.Credit
                .Include(c => c.Payment)
                .FirstOrDefault(c => c.id == id);

            if (credit == null)
            {
                return NotFound();
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                PdfDocument document = new PdfDocument();
                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);

                // Заголовок
                XFont fontTitle = new XFont("Arial", 20, XFontStyle.Bold);
                gfx.DrawString($"Кредит № {credit.id}", fontTitle, XBrushes.Black, new XPoint(40, 40));

                // Информация о кредите
                XFont fontContent = new XFont("Arial", 12);
                gfx.DrawString($"Сумма кредита: {credit.Amount}", fontContent, XBrushes.Black, new XPoint(40, 80));

                // Платежи по кредиту
                gfx.DrawString("Платежи по кредиту:", fontContent, XBrushes.Black, new XPoint(40, 120));

                int yPosition = 140;
                foreach (var payment in credit.Payment)
                {
                    gfx.DrawString($"Дата: {payment.PaymentDate.ToString("yyyy-MM-dd")}, Сумма: {payment.PaymentAmount}", fontContent, XBrushes.Black, new XPoint(40, yPosition));
                    yPosition += 20;
                }

                document.Save(memoryStream, false);
                return File(memoryStream.ToArray(), "application/pdf", "CreditDetails.pdf");
            }
        }
   
    }
}

public class CreditDetailsViewModel
{
    public CreditModel Credit { get; set; }
    public List<PaymentsModel> Payment { get; set; }
}
