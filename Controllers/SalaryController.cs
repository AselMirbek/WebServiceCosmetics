using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebServiceCosmetics.Data;
using WebServiceCosmetics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using Microsoft.AspNetCore.Mvc;
using Fall.Core.BaseModels.EFModels;
using Microsoft.Data.SqlClient;
using PdfSharpCore.Pdf.IO;
using static WebServiceCosmetics.Controllers.SalaryController;
using System.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;
using DocumentFormat.OpenXml;


namespace WebServiceCosmetics.Controllers
{
    public class SalaryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalaryController(ApplicationDbContext context)
        {
            _context = context;
        }
        // Главная страница с выбором периода
        public async Task<IActionResult> Index(int? year, int? month)
        {
            try
            {

                var currentDate = DateTime.Now;
                var selectedYear = year ?? currentDate.Year;
                var selectedMonth = month ?? currentDate.Month;

                // Проверка на будущий период (теперь на стороне сервера)
                var periodDate = new DateTime(selectedYear, selectedMonth, 1);
                if (periodDate > currentDate)
                {
                    ViewBag.ErrorMessage = "Нельзя рассчитать зарплату за будущий период";
                    return View(new List<SalaryModel>());
                }

                // Загрузка данных
                var salary = await CalculateSalaries(selectedYear, selectedMonth);

                ViewBag.SelectedYear = selectedYear;
                ViewBag.SelectedMonth = selectedMonth;
                return View(salary);
            }
            catch (Exception ex)
            {
                //  при ошибках
                ViewBag.ErrorMessage = $"Ошибка: {ex.Message}";
                return View(new List<SalaryModel>());
            }
        }

        // Вызов SP_CalculateSalaries
        private async Task<List<SalaryModel>> CalculateSalaries(int year, int month)
        {
            var result = new List<SalaryModel>();

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SP_CalculateSalaries";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@Year", year));
                command.Parameters.Add(new SqlParameter("@Month", month));

                await _context.Database.OpenConnectionAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var salary = new SalaryModel
                        {
                            ID = reader.GetInt32(0), // int
                            Employees_id = reader.GetInt32(1), // short
                            Year = reader.GetInt32(3), // int
                            Month = reader.GetInt32(4), // int
                            NumberOfPurchases = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                            NumberOfProductions = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6),
                            NumberOfSales = reader.IsDBNull(7) ? null : (int?)reader.GetInt32(7),
                            Common = reader.IsDBNull(8) ? null : (int?)reader.GetInt32(8), // Возможно вычисляемое поле
                            SalaryAmount = reader.IsDBNull(9) ? 0 : reader.GetDecimal(9), // decimal
                            Bonus = reader.IsDBNull(10) ? null : (decimal?)reader.GetDecimal(10), // decimal
                            General = reader.IsDBNull(11) ? null : (decimal?)reader.GetDecimal(11), // decimal
                            Issued = reader.GetBoolean(12), // bool
                            Employees = new Employer { Full_Name = reader.GetString(2) }
                        };

                        result.Add(salary);
                    }
                }

            }

            return result;
        }
        // Обновление General через SP_UpdateSalaryGeneralAmount (AJAX)
        [HttpPost]
        public async Task<IActionResult> UpdateSalary(int id, float generalAmount)
        {
            try
            {
                var salary = await _context.Salary.FindAsync(id);
                if (salary == null)
                    return Json(new { success = false, message = "Запись не найдена" });

                if (salary.Issued)
                    return Json(new { success = false, message = "Зарплата уже выдана" });

                // Вызов хранимой процедуры
                var parameters = new[]
                {
                    new SqlParameter("@SalaryID", id),
                    new SqlParameter("@NewGeneralAmount", generalAmount),
                    new SqlParameter("@Success", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                    new SqlParameter("@Message", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output }
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC SP_UpdateSalaryGeneralAmount @SalaryID, @NewGeneralAmount, @Success OUTPUT, @Message OUTPUT",
                    parameters);

                bool success = (bool)parameters[2].Value;
                string message = (string)parameters[3].Value;

                return Json(new { success, message });
            }
            catch 
            (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        // Вызов SP_PaySalaries (AJAX)
        [HttpPost]
        public async Task<IActionResult> PaySalaries(int year, int month)
        {
            try
            {
                // Создаем параметр для возвращаемого значения
                var returnValue = new SqlParameter
                {
                    ParameterName = "@ReturnValue",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Output
                };

                var parameters = new[]
                {
                    new SqlParameter("@Year", year),
                    new SqlParameter("@Month", month),
                    returnValue
                };

                await _context.Database.ExecuteSqlRawAsync(
            "EXEC @ReturnValue = SP_PaySalaries @Year, @Month",
            parameters);
                int resultCode = (int)returnValue.Value;

                var message = resultCode switch
                {
                    0 => "Зарплаты успешно выплачены",
                    1 => "Не указаны год или месяц",
                    2 => "Некорректный месяц",
                    3 => "Нельзя выплачивать зарплату за будущий период",
                    4 => "Зарплата за этот период уже выплачена",
                    5 => "Бюджет не найден",
                    6 => "Нет зарплат для выплаты",
                    7 => "Недостаточно средств в бюджете",
                    -99 => "Ошибка при выполнении транзакции",
                    _ => $"Неизвестный код ошибки: {resultCode}"
                };
                if (resultCode == 0)
                {
                    // Обновление поля Issued
                    var salariesToUpdate = await _context.Salary
                        .Where(s => s.Year == year && s.Month == month && !s.Issued)
                        .ToListAsync();

                    foreach (var salary in salariesToUpdate)
                    {
                        salary.Issued = true;
                    }

                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = resultCode == 0,
                    message = message
                });

            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Сообщение: {ex.Message}"
                });
            }
        }

        public IActionResult ExportToWord(int year, int month)
        {
            try
            {
                var salaries = _context.Salary
                    .Where(s => s.Year.ToString() == year.ToString() && s.Month.ToString() == month.ToString())
                    .ToList();
                if (salaries.Count == 0)
                {
                    return Json(new { success = false, message = "Нет данных для указанного года и месяца" });
                }
                using (var memoryStream = new MemoryStream())
                {
                    using (var wordDocument = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
                    {
                        var mainPart = wordDocument.AddMainDocumentPart();
                        mainPart.Document = new Document(new Body());

                        var body = mainPart.Document.Body;
                        var paragraph = new Paragraph(new Run(new Text("Salaries Report")));
                        body.AppendChild(paragraph);

                        foreach (var salary in salaries)
                        {
                            var salaryParagraph = new Paragraph();
                            salaryParagraph.AppendChild(new Run(new Text($"Employee: {salary.Employees.Full_Name}, Salary Amount: {salary.SalaryAmount}")));
                            body.AppendChild(salaryParagraph);
                        }

                        wordDocument.Save(); // Save the document to the memory stream
                    }

                    return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "SalariesReport.docx");
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }


        public IActionResult ExportToExcel(int year, int month)
        {
            try
            {
                var salaries = _context.Salary.Where(s => s.Year == year && s.Month == month).ToList();

                using (var memoryStream = new MemoryStream())
                {
                    using (var spreadsheetDocument = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
                    {
                        var workbookPart = spreadsheetDocument.AddWorkbookPart();
                        workbookPart.Workbook = new Workbook();
                        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                        worksheetPart.Worksheet = new Worksheet(new SheetData());

                        var sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
                        sheets.AppendChild(new Sheet() { Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Salaries" });

                        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                        var headerRow = new Row();
                        headerRow.AppendChild(new Cell { CellValue = new CellValue("ID"), DataType = CellValues.String });
                        headerRow.AppendChild(new Cell { CellValue = new CellValue("Employee"), DataType = CellValues.String });
                        headerRow.AppendChild(new Cell { CellValue = new CellValue("Salary Amount"), DataType = CellValues.String });
                        sheetData.AppendChild(headerRow);

                        foreach (var salary in salaries)
                        {
                            var row = new Row();
                            row.AppendChild(new Cell { CellValue = new CellValue(salary.ID.ToString()), DataType = CellValues.Number });
                            row.AppendChild(new Cell { CellValue = new CellValue(salary.Employees.Full_Name), DataType = CellValues.String });
                            row.AppendChild(new Cell { CellValue = new CellValue(salary.SalaryAmount.ToString()), DataType = CellValues.Number });
                            sheetData.AppendChild(row);
                        }

                    }

                    return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Salaries.xlsx");
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

public IActionResult ExportToPdf(int year, int month)
    {
        try
        {
            var salaries = _context.Salary.Where(s => s.Year == year && s.Month == month).ToList();

            using (var memoryStream = new MemoryStream())
            {
                var document = new PdfDocument();
                var page = document.AddPage();
                var gfx = XGraphics.FromPdfPage(page);
                var font = new XFont("Verdana", 12);

                int yPosition = 20;
                gfx.DrawString("Salaries Report", font, XBrushes.Black, new XPoint(20, yPosition));
                yPosition += 20;

                foreach (var salary in salaries)
                {
                    gfx.DrawString($"Employee: {salary.Employees.Full_Name}, Salary Amount: {salary.SalaryAmount}", font, XBrushes.Black, new XPoint(20, yPosition));
                    yPosition += 20;
                }

                document.Save(memoryStream);
                return File(memoryStream.ToArray(), "application/pdf", "SalariesReport.pdf");
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
        }
    }

}
    

    // ViewModel для страницы
    public class SalaryViewModel
    {
        public int SelectedYear { get; set; }
        public int SelectedMonth { get; set; }
        public List<SalaryModel> Salary { get; set; } = new List<SalaryModel>();
    }
}
