 using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServiceCosmetics.Data; // Твой контекст
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
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
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Word = DocumentFormat.OpenXml.Wordprocessing;
using Drawing = DocumentFormat.OpenXml.Drawing;

public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }
    public async Task<IActionResult> Index()
    {
       
        return View();

    }
    private static TableCell CreateStyledCell(string text, bool isHeader = false)
    {
        var runProps = new Drawing.RunProperties();
        if (isHeader)
          new Word.Bold();

        var paragraph = new Paragraph(
            new Run(runProps, new Text(text ?? ""))
        )
        {
            ParagraphProperties = new ParagraphProperties(new Justification { Val = JustificationValues.Left })
        };

        var cellProps = new TableCellProperties(
            new TableCellWidth { Type = TableWidthUnitValues.Auto }
        );

        return new TableCell(cellProps, paragraph);
    }

    // Экспорт отчёта о производстве продуктов в Word
    public IActionResult ExportProductManufacturingToWord()
    {
        var productions = _context.Product_Manufacturing
            .Include(p => p.ProductModel)
            .Include(p => p.Employees)
            .ToList();

        using var ms = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Заголовок
            var heading = new Paragraph(new Run(new Text("Отчет о производстве продуктов")))
            {
                ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center })
            };
            body.AppendChild(heading);

            // Создаём таблицу
            var table = new DocumentFormat.OpenXml.Wordprocessing.Table();

            // Заголовок таблицы
            var headerRow = new TableRow();
            headerRow.Append(
                CreateTableCell("Продукт", true),
                CreateTableCell("Сотрудник", true),
                CreateTableCell("Количество", true),
                CreateTableCell("Дата", true)
            );
            table.AppendChild(headerRow);

            // Данные
            foreach (var item in productions)
            {
                var row = new TableRow();
                row.Append(
                    CreateTableCell(item.ProductModel?.Name ?? ""),
                    CreateTableCell(item.Employees?.Full_Name ?? ""),
                    CreateTableCell(item.Quantity.ToString()),
                    CreateTableCell(item.Date.ToShortDateString())
                );
                table.AppendChild(row);
            }

            body.AppendChild(table);
            mainPart.Document.Save();
        }

        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "ProductManufacturingReport.docx");
    }

    // Экспорт отчёта о производстве продуктов в Excel
    public IActionResult ExportProductManufacturingToExcel()
    {
        var productions = _context.Product_Manufacturing
            .Include(p => p.ProductModel)
            .Include(p => p.Employees)
            .ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Производство");

        // Заголовки
        worksheet.Cell(1, 1).Value = "Продукт";
        worksheet.Cell(1, 2).Value = "Сотрудник";
        worksheet.Cell(1, 3).Value = "Количество";
        worksheet.Cell(1, 4).Value = "Дата";

        int row = 2;
        foreach (var item in productions)
        {
            worksheet.Cell(row, 1).Value = item.ProductModel?.Name ?? "";
            worksheet.Cell(row, 2).Value = item.Employees?.Full_Name ?? "";
            worksheet.Cell(row, 3).Value = item.Quantity;
            worksheet.Cell(row, 4).Value = item.Date.ToShortDateString();
            row++;
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ProductManufacturingReport.xlsx");
    }

    // Экспорт отчёта о производстве продуктов в PDF
    public IActionResult ExportProductManufacturingToPdf()
    {
        var productions = _context.Product_Manufacturing
            .Include(p => p.ProductModel)
            .Include(p => p.Employees)
            .ToList();

        using var ms = new MemoryStream();
        var document = new PdfDocument();
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("Verdana", 12, XFontStyle.Regular);

        double yPoint = 40;

        // Заголовок
        gfx.DrawString("Отчет о производстве продуктов", new XFont("Verdana", 14, XFontStyle.Bold), XBrushes.Black,
            new XRect(0, yPoint, page.Width, page.Height), XStringFormats.TopCenter);
        yPoint += 40;

        // Заголовки таблицы
        gfx.DrawString("Продукт", font, XBrushes.Black, new XRect(40, yPoint, 100, page.Height), XStringFormats.TopLeft);
        gfx.DrawString("Сотрудник", font, XBrushes.Black, new XRect(150, yPoint, 100, page.Height), XStringFormats.TopLeft);
        gfx.DrawString("Количество", font, XBrushes.Black, new XRect(260, yPoint, 100, page.Height), XStringFormats.TopLeft);
        gfx.DrawString("Дата", font, XBrushes.Black, new XRect(370, yPoint, 100, page.Height), XStringFormats.TopLeft);
        yPoint += 25;

        foreach (var item in productions)
        {
            gfx.DrawString(item.ProductModel?.Name ?? "", font, XBrushes.Black, new XRect(40, yPoint, 100, page.Height), XStringFormats.TopLeft);
            gfx.DrawString(item.Employees?.Full_Name ?? "", font, XBrushes.Black, new XRect(150, yPoint, 100, page.Height), XStringFormats.TopLeft);
            gfx.DrawString(item.Quantity.ToString(), font, XBrushes.Black, new XRect(260, yPoint, 100, page.Height), XStringFormats.TopLeft);
            gfx.DrawString(item.Date.ToShortDateString(), font, XBrushes.Black, new XRect(370, yPoint, 100, page.Height), XStringFormats.TopLeft);
            yPoint += 20;

            if (yPoint > page.Height - 40)
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                yPoint = 40;
            }
        }

        document.Save(ms);
        return File(ms.ToArray(), "application/pdf", "ProductManufacturingReport.pdf");
    }

    // Вспомогательный метод для создания ячейки Word таблицы
    private TableCell CreateTableCell(string text, bool isBold = false)
    {
        var run = new Run(new Text(text));
        if (isBold)
            run.RunProperties = new Word.RunProperties(new Word.Bold());

        var paragraph = new Paragraph(run);
        var tableCell = new TableCell(paragraph);
        return tableCell;
    }

    // Отчёт по продажам — экспорт в PDF
    public IActionResult ExportProductSalesToPdf()
        {
            var salesData = _context.Product_Sales
                                    .Include(p => p.ProductModel)
                                    .Include(p => p.Employees)
                                    .ToList();

            var pdfDocument = new PdfDocument();
            var page = pdfDocument.AddPage();
            var graphics = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12);

            graphics.DrawString("Отчёт по продажам", font, XBrushes.Black, 20, 30);

            int yPosition = 60;
            foreach (var sale in salesData)
            {
                string row = $"{sale.ProductModel.Name} - {sale.Employees.Full_Name} - {sale.Quantity} - {sale.Amount} - {sale.Date.ToShortDateString()}";
                graphics.DrawString(row, font, XBrushes.Black, 20, yPosition);
                yPosition += 20;

                if (yPosition > page.Height - 40) // новая страница
                {
                    page = pdfDocument.AddPage();
                    graphics = XGraphics.FromPdfPage(page);
                    yPosition = 40;
                }
            }

            using var ms = new MemoryStream();
            pdfDocument.Save(ms);
            return File(ms.ToArray(), "application/pdf", "SalesReport.pdf");
        }

        // Отчёт по продажам — экспорт в Excel
        public IActionResult ExportProductSalesToExcel()
        {
            var salesData = _context.Product_Sales
                                    .Include(p => p.ProductModel)
                                    .Include(p => p.Employees)
                                    .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Продажи");

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

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesReport.xlsx");
        }

        // Отчёт по продажам — экспорт в Word
        public IActionResult ExportProductSalesToWord()
        {
            var salesData = _context.Product_Sales
                                    .Include(p => p.ProductModel)
                                    .Include(p => p.Employees)
                                    .ToList();

            using var mem = new MemoryStream();
            using (var wordDoc = WordprocessingDocument.Create(mem, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                body.Append(new Paragraph(new Run(new Text("Отчёт по продажам")))
                {
                    ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center })
                });

                foreach (var sale in salesData)
                {
                    var line = $"{sale.Date.ToShortDateString()} | {sale.ProductModel.Name} | {sale.Employees.Full_Name} | {sale.Quantity} | {sale.Amount}";
                    body.Append(new Paragraph(new Run(new Text(line))));
                }
            }

            return File(mem.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "SalesReport.docx");
        }
    // Экспорт RawMaterialPurchase в Word
    [HttpGet]
    public IActionResult ExportRawMaterialPurchaseToWord()
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

    // Экспорт RawMaterialPurchase в Excel
    [HttpGet]
    public IActionResult ExportRawMaterialPurchaseToExcel()
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

    // Экспорт RawMaterialPurchase в PDF
    [HttpGet]
    public async Task<IActionResult> ExportRawMaterialPurchaseToPdf()
    {
        var purchases = await _context.Raw_Materials_Purchase
            .Include(r => r.RawMaterialModel)
            .Include(r => r.Employees)
            .ToListAsync();

        using var stream = new MemoryStream();
        var document = new PdfDocument();
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("Verdana", 12);

        double y = 40;
        foreach (var purchase in purchases)
        {
            gfx.DrawString($"Сырье: {purchase.RawMaterialModel?.Name}, Кол-во: {purchase.Quantity}, Сумма: {purchase.Amount}, Сотрудник: {purchase.Employees?.Full_Name}",
                font, XBrushes.Black, new XRect(20, y, page.Width - 40, page.Height), XStringFormats.TopLeft);
            y += 25;
            if (y > page.Height - 40)
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                y = 40;
            }
        }

        document.Save(stream, false);

        stream.Position = 0;
        return File(stream.ToArray(), "application/pdf", "RawMaterialPurchases.pdf");
    }
    [HttpGet("Report/ExportCreditToWord/{id}")]
    public IActionResult ExportCreditToWord(int id)
    {
        var credit = _context.Credit
            .Include(c => c.Payment)
            .FirstOrDefault(c => c.id == id);

        if (credit == null)
            return NotFound();

        using var memoryStream = new MemoryStream();
        using (var wordDocument = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body;

            body.Append(
                new Paragraph(new Run(new Text($"Кредит № {credit.id}"))),
                new Paragraph(new Run(new Text($"Сумма кредита: {credit.Amount}"))),
                new Paragraph(new Run(new Text("Платежи по кредиту:")))
            );

            foreach (var payment in credit.Payment)
            {
                body.Append(new Paragraph(new Run(new Text(
                    $"Дата: {payment.PaymentDate:yyyy-MM-dd}, Сумма: {payment.PaymentAmount}"))));
            }

            wordDocument.Save();
        }

        return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "CreditDetails.docx");
    }

    [HttpGet("Report/ExportCreditToExcel/{id}")]
    public IActionResult ExportCreditToExcel(int id)
    {
        var credit = _context.Credit
            .Include(c => c.Payment)
            .FirstOrDefault(c => c.id == id);

        if (credit == null)
            return NotFound();

        using var memoryStream = new MemoryStream();
        using (var spreadsheet = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = spreadsheet.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());
            var sheets = spreadsheet.WorkbookPart.Workbook.AppendChild(new Sheets());
            var sheet = new Sheet() { Name = "Кредит", SheetId = 1, Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart) };
            sheets.Append(sheet);

            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            var header = new Row();
            header.Append(
                new Cell { CellValue = new CellValue("Кредит №"), DataType = CellValues.String },
                new Cell { CellValue = new CellValue("Сумма"), DataType = CellValues.String },
                new Cell { CellValue = new CellValue("Дата платежа"), DataType = CellValues.String },
                new Cell { CellValue = new CellValue("Сумма платежа"), DataType = CellValues.String }
            );
            sheetData.Append(header);

            foreach (var payment in credit.Payment)
            {
                var row = new Row();
                row.Append(
                    new Cell { CellValue = new CellValue(credit.id.ToString()), DataType = CellValues.Number },
                    new Cell { CellValue = new CellValue(credit.Amount.ToString()), DataType = CellValues.Number },
                    new Cell { CellValue = new CellValue(payment.PaymentDate.ToString("yyyy-MM-dd")), DataType = CellValues.String },
                    new Cell { CellValue = new CellValue(payment.PaymentAmount.ToString()), DataType = CellValues.Number }
                );
                sheetData.Append(row);
            }

            workbookPart.Workbook.Save();
        }

        return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CreditDetails.xlsx");
    }

    [HttpGet("Report/ExportCreditToPdf/{id}")]
    public IActionResult ExportCreditToPdf(int id)
    {
        var credit = _context.Credit
            .Include(c => c.Payment)
            .FirstOrDefault(c => c.id == id);

        if (credit == null)
            return NotFound();

        using var memoryStream = new MemoryStream();
        var document = new PdfDocument();
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var fontHeader = new XFont("Arial", 16, XFontStyle.Bold);
        var fontContent = new XFont("Arial", 12);

        gfx.DrawString($"Кредит № {credit.id}", fontHeader, XBrushes.Black, new XPoint(40, 40));
        gfx.DrawString($"Сумма кредита: {credit.Amount}", fontContent, XBrushes.Black, new XPoint(40, 80));
        gfx.DrawString("Платежи по кредиту:", fontContent, XBrushes.Black, new XPoint(40, 110));

        int y = 140;
        foreach (var payment in credit.Payment)
        {
            gfx.DrawString($"Дата: {payment.PaymentDate:yyyy-MM-dd}, Сумма: {payment.PaymentAmount}", fontContent, XBrushes.Black, new XPoint(40, y));
            y += 20;
        }

        document.Save(memoryStream, false);
        return File(memoryStream.ToArray(), "application/pdf", "CreditDetails.pdf");
    }

}



