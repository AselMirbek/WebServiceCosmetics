// Controllers/ReportController.cs

using System.Globalization;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using WebServiceCosmetics.Data;
using WebServiceCosmetics.ViewModel;
using WebServiceCosmetics.ViewModel;
namespace WebServiceCosmetics.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // DASHBOARD
        // =====================================================

        public async Task<IActionResult> Index()
        {
            var vm = new ReportDashboardVM
            {
                TotalRevenue = await _context.Product_Sales
                    .SumAsync(x => (decimal?)x.Amount) ?? 0,

                TotalSales =
                    await _context.Product_Sales
                        .CountAsync(),

                TotalManufacturing =
                    await _context.Product_Manufacturing
                        .CountAsync(),

                TotalPurchases =
                    await _context.Raw_Materials_Purchase
                        .CountAsync(),

                TotalSalary =
                    await _context.Salary
                        .SumAsync(x =>
                            (decimal?)x.General) ?? 0,

                TotalCredits =
                    await _context.Credit
                        .SumAsync(x =>
                            (decimal?)x.Amount) ?? 0
            };

            return View(vm);
        }

        // =====================================================
        // SALES REPORT
        // =====================================================

        public async Task<IActionResult>
            Sales(ReportFilterVM filter)
        {
            var sales = _context.Product_Sales
                .Include(x => x.ProductModel)
                .Include(x => x.Employees)
                .AsQueryable();

            // FILTER DATE FROM

            if (filter.DateFrom.HasValue)
            {
                sales = sales.Where(x =>
                    x.Date >= filter.DateFrom.Value);
            }

            // FILTER DATE TO

            if (filter.DateTo.HasValue)
            {
                sales = sales.Where(x =>
                    x.Date <= filter.DateTo.Value);
            }

            // FILTER EMPLOYEE

            if (filter.Employees_id.HasValue)
            {
                sales = sales.Where(x =>
                    x.Employees_id ==
                    filter.Employees_id.Value);
            }

            var vm = new SalesReportVM
            {
                Sales = await sales
                    .OrderByDescending(x => x.Date)
                    .ToListAsync(),

                Employees = await _context.Employees
                    .Select(x => new SelectListItem
                    {
                        Text = x.Full_Name,
                        Value = x.Id.ToString()
                    })
                    .ToListAsync(),

                Filter = filter
            };

            return View(vm);
        }
        // =====================================================
        // SALES PDF
        // =====================================================

        public async Task<IActionResult>
            SalesPdf()
        {
            var sales = await _context.Product_Sales
                .Include(x => x.ProductModel)
                .Include(x => x.Employees)
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            PdfDocument document =
                new PdfDocument();

            document.Info.Title =
                "Sales Report";

            PdfPage page =
                document.AddPage();

            XGraphics gfx =
                XGraphics.FromPdfPage(page);

            XFont titleFont =
                new XFont("Verdana", 18);

            XFont headerFont =
                new XFont("Verdana", 10,
                    XFontStyle.Bold);

            XFont font =
                new XFont("Verdana", 9);

            // TITLE

            gfx.DrawString(
                "Sales Report",
                titleFont,
                XBrushes.Black,
                new XRect(0, 20,
                    page.Width,
                    page.Height),
                XStringFormats.TopCenter);

            int y = 70;

            // TABLE HEADER

            gfx.DrawRectangle(
                XPens.Black,
                20, y, 550, 25);

            gfx.DrawString(
                "Product",
                headerFont,
                XBrushes.Black,
                new XPoint(25, y + 15));

            gfx.DrawString(
                "Employee",
                headerFont,
                XBrushes.Black,
                new XPoint(160, y + 15));

            gfx.DrawString(
                "Qty",
                headerFont,
                XBrushes.Black,
                new XPoint(300, y + 15));

            gfx.DrawString(
                "Amount",
                headerFont,
                XBrushes.Black,
                new XPoint(370, y + 15));

            gfx.DrawString(
                "Date",
                headerFont,
                XBrushes.Black,
                new XPoint(470, y + 15));

            y += 25;

            // DATA

            foreach (var item in sales)
            {
                gfx.DrawRectangle(
                    XPens.Black,
                    20, y, 550, 25);

                gfx.DrawString(
                    item.ProductModel?.Name ??
                    "No product",
                    font,
                    XBrushes.Black,
                    new XPoint(25, y + 15));

                gfx.DrawString(
                    item.Employees?.Full_Name ??
                    "No employee",
                    font,
                    XBrushes.Black,
                    new XPoint(160, y + 15));

                gfx.DrawString(
                    item.Quantity.ToString(),
                    font,
                    XBrushes.Black,
                    new XPoint(300, y + 15));

                gfx.DrawString(
                    item.Amount.ToString(),
                    font,
                    XBrushes.Black,
                    new XPoint(370, y + 15));

                gfx.DrawString(
                    item.Date
                        .ToString("dd.MM.yyyy"),
                    font,
                    XBrushes.Black,
                    new XPoint(470, y + 15));

                y += 25;

                // NEW PAGE

                if (y > 750)
                {
                    page = document.AddPage();

                    gfx = XGraphics
                        .FromPdfPage(page);

                    y = 40;
                }
            }
            // TOTALS

            decimal totalQuantity =
                sales.Sum(x => x.Quantity);

            decimal totalAmount =
                sales.Sum(x => x.Amount);

            y += 20;

            gfx.DrawRectangle(
                XPens.Black,
                20, y, 550, 30);

            gfx.DrawString(
                "TOTAL",
                headerFont,
                XBrushes.Black,
                new XPoint(160, y + 18));

            gfx.DrawString(
                totalQuantity.ToString(),
                headerFont,
                XBrushes.Black,
                new XPoint(300, y + 18));

            gfx.DrawString(
                totalAmount.ToString(),
                headerFont,
                XBrushes.Black,
                new XPoint(370, y + 18));
            using var stream = new MemoryStream();

            document.Save(stream);

            return File(
                stream.ToArray(),
                "application/pdf",
                "SalesReport.pdf");
        }
        // =====================================================
        // SALES WORD
        // =====================================================

        public async Task<IActionResult>
            SalesWord()
        {
            var sales = await _context.Product_Sales
                .Include(x => x.ProductModel)
                .Include(x => x.Employees)
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            using var stream = new MemoryStream();

            using (WordprocessingDocument doc =
                   WordprocessingDocument.Create(
                       stream,
                       DocumentFormat.OpenXml
                           .WordprocessingDocumentType
                           .Document,
                       true))
            {
                MainDocumentPart mainPart =
                    doc.AddMainDocumentPart();

                mainPart.Document =
                    new Document();

                Body body = new Body();

                // TITLE

                body.Append(
                    new Paragraph(
                        new Run(
                            new Text(
                                "Sales Report"))));

                // TABLE

                Table table = new Table();

                // HEADER

                TableRow header =
                    new TableRow();

                header.Append(CreateCell("Product"));
                header.Append(CreateCell("Employee"));
                header.Append(CreateCell("Quantity"));
                header.Append(CreateCell("Amount"));
                header.Append(CreateCell("Date"));

                table.Append(header);

                // DATA

                foreach (var item in sales)
                {
                    TableRow row =
                        new TableRow();

                    row.Append(
                        CreateCell(
                            item.ProductModel?.Name ??
                            "No product"));

                    row.Append(
                        CreateCell(
                            item.Employees?.Full_Name ??
                            "No employee"));

                    row.Append(
                        CreateCell(
                            item.Quantity.ToString()));

                    row.Append(
                        CreateCell(
                            item.Amount.ToString()));

                    row.Append(
                        CreateCell(
                            item.Date
                                .ToString("dd.MM.yyyy")));

                    table.Append(row);
                }
                // TOTALS

                decimal totalQuantity =
                    sales.Sum(x => x.Quantity);

                decimal totalAmount =
                    sales.Sum(x => x.Amount);

                TableRow totalRow =
                    new TableRow();

                totalRow.Append(
                    CreateCell(""));

                totalRow.Append(
                    CreateCell("TOTAL"));

                totalRow.Append(
                    CreateCell(
                        totalQuantity.ToString()));

                totalRow.Append(
                    CreateCell(
                        totalAmount.ToString()));

                totalRow.Append(
                    CreateCell(""));

                table.Append(totalRow);

                body.Append(table);

                mainPart.Document.Append(body);

                mainPart.Document.Save();
            }

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "SalesReport.docx");
        }

        // =====================================================
        // WORD HELPER
        // =====================================================

        private TableCell CreateCell(string text)
        {
            return new TableCell(
                new Paragraph(
                    new Run(
                        new Text(text))));
        }
        // =====================================================
        // SALES EXCEL
        // =====================================================

        public async Task<IActionResult>
            SalesExcel()
        {
            var sales = await _context.Product_Sales
                .Include(x => x.ProductModel)
                .Include(x => x.Employees)
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var sheet = workbook.Worksheets
                .Add("Sales");

            // HEADERS

            sheet.Cell(1, 1).Value = "Product";
            sheet.Cell(1, 2).Value = "Employee";
            sheet.Cell(1, 3).Value = "Quantity";
            sheet.Cell(1, 4).Value = "Amount";
            sheet.Cell(1, 5).Value = "Date";

            // STYLE HEADER

            var headerRange =
                sheet.Range("A1:E1");

            headerRange.Style.Font.Bold = true;

            int row = 2;

            foreach (var item in sales)
            {
                sheet.Cell(row, 1).Value =
                    item.ProductModel?.Name ??
                    "No product";

                sheet.Cell(row, 2).Value =
                    item.Employees?.Full_Name ??
                    "No employee";

                sheet.Cell(row, 3).Value =
                    item.Quantity;

                sheet.Cell(row, 4).Value =
                    item.Amount;

                sheet.Cell(row, 5).Value =
                    item.Date.ToString("dd.MM.yyyy");

                row++;
            }
            // TOTALS

            decimal totalQuantity =
                sales.Sum(x => x.Quantity);

            decimal totalAmount =
                sales.Sum(x => x.Amount);

            sheet.Cell(row + 1, 2).Value =
                "TOTAL";

            sheet.Cell(row + 1, 3).Value =
                totalQuantity;

            sheet.Cell(row + 1, 4).Value =
                totalAmount;

            // STYLE TOTAL ROW

            var totalRange =
                sheet.Range($"B{row + 1}:D{row + 1}");

            totalRange.Style.Font.Bold = true;

            // TABLE STYLE

            sheet.Columns().AdjustToContents();

            sheet.RangeUsed().Style.Border
                .OutsideBorder =
                    XLBorderStyleValues.Thin;

            sheet.RangeUsed().Style.Border
                .InsideBorder =
                    XLBorderStyleValues.Thin;
      

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SalesReport.xlsx");
        }
        // =====================================================
        // MANUFACTURING REPORT
        // =====================================================

        public async Task<IActionResult>
            Manufacturing(ReportFilterVM filter)
        {
            var manufacturing =
                _context.Product_Manufacturing
                    .Include(x => x.ProductModel)
                    .Include(x => x.Employees)
                    .AsQueryable();

            if (filter.DateFrom.HasValue)
            {
                manufacturing = manufacturing.Where(x =>
                    x.Date >= filter.DateFrom.Value);
            }

            if (filter.DateTo.HasValue)
            {
                manufacturing = manufacturing.Where(x =>
                    x.Date <= filter.DateTo.Value);
            }

            if (filter.Employees_id.HasValue)
            {
                manufacturing = manufacturing.Where(x =>
                    x.Employees_id ==
                    filter.Employees_id.Value);
            }

            var vm = new ManufacturingReportVM
            {
                Manufacturing = await manufacturing
                    .OrderByDescending(x => x.Date)
                    .ToListAsync(),

                Employees = await _context.Employees
                    .Select(x => new SelectListItem
                    {
                        Text = x.Full_Name,
                        Value = x.Id.ToString()
                    })
                    .ToListAsync(),

                Filter = filter
            };

            return View(vm);
        }
        // =====================================================
        // MANUFACTURING EXCEL
        // =====================================================

        public async Task<IActionResult>
            ManufacturingExcel()
        {
            var manufacturing =
                await _context.Product_Manufacturing
                    .Include(x => x.ProductModel)
                    .Include(x => x.Employees)
                    .OrderByDescending(x => x.Date)
                    .ToListAsync();

            using var workbook = new XLWorkbook();

            var sheet = workbook.Worksheets
                .Add("Manufacturing");

            // HEADERS

            sheet.Cell(1, 1).Value = "Product";
            sheet.Cell(1, 2).Value = "Employee";
            sheet.Cell(1, 3).Value = "Quantity";
            sheet.Cell(1, 4).Value = "Date";

            // DATA

            int row = 2;

            foreach (var item in manufacturing)
            {
                sheet.Cell(row, 1).Value =
                    item.ProductModel?.Name ??
                    "No product";

                sheet.Cell(row, 2).Value =
                    item.Employees?.Full_Name ??
                    "No employee";

                sheet.Cell(row, 3).Value =
                    item.Quantity;

                sheet.Cell(row, 4).Value =
                    item.Date.ToString("dd.MM.yyyy");

                row++;
            }

            // STYLE

            sheet.Columns().AdjustToContents();

            sheet.RangeUsed().Style.Border
                .OutsideBorder =
                    XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ManufacturingReport.xlsx");
        }
        // =====================================================
        // MANUFACTURING PDF
        // =====================================================

        public async Task<IActionResult>
            ManufacturingPdf()
        {
            var manufacturing =
                await _context.Product_Manufacturing
                    .Include(x => x.ProductModel)
                    .Include(x => x.Employees)
                    .OrderByDescending(x => x.Date)
                    .ToListAsync();

            PdfDocument document =
                new PdfDocument();

            document.Info.Title =
                "Manufacturing Report";

            PdfPage page =
                document.AddPage();

            XGraphics gfx =
                XGraphics.FromPdfPage(page);

            XFont titleFont =
                new XFont("Verdana", 18);

            XFont font =
                new XFont("Verdana", 10);

            // TITLE

            gfx.DrawString(
                "Manufacturing Report",
                titleFont,
                XBrushes.Black,
                new XRect(0, 20,
                    page.Width,
                    page.Height),
                XStringFormats.TopCenter);

            // TABLE

            int y = 70;

            foreach (var item in manufacturing)
            {
                string line =
                    $"{item.ProductModel?.Name} | " +
                    $"{item.Employees?.Full_Name} | " +
                    $"Qty: {item.Quantity}";

                gfx.DrawString(
                    line,
                    font,
                    XBrushes.Black,
                    new XPoint(40, y));

                y += 20;

                // NEW PAGE

                if (y > 750)
                {
                    page = document.AddPage();

                    gfx = XGraphics
                        .FromPdfPage(page);

                    y = 40;
                }
            }

            using var stream = new MemoryStream();

            document.Save(stream);

            return File(
                stream.ToArray(),
                "application/pdf",
                "ManufacturingReport.pdf");
        }
        // =====================================================
        // MANUFACTURING WORD
        // =====================================================

        public async Task<IActionResult>
            ManufacturingWord()
        {
            var manufacturing =
                await _context.Product_Manufacturing
                    .Include(x => x.ProductModel)
                    .Include(x => x.Employees)
                    .OrderByDescending(x => x.Date)
                    .ToListAsync();

            using var stream = new MemoryStream();

            using (WordprocessingDocument doc =
                   WordprocessingDocument.Create(
                       stream,
                       DocumentFormat.OpenXml
                           .WordprocessingDocumentType
                           .Document,
                       true))
            {
                MainDocumentPart mainPart =
                    doc.AddMainDocumentPart();

                mainPart.Document =
                    new Document();

                Body body = new Body();

                // TITLE

                body.Append(
                    new Paragraph(
                        new Run(
                            new Text(
                                "Manufacturing Report"))));

                // TABLE

                Table table = new Table();

                // HEADER

                TableRow header =
                    new TableRow();

                header.Append(
                    new TableCell(
                        new Paragraph(
                            new Run(
                                new Text("Product")))));

                header.Append(
                    new TableCell(
                        new Paragraph(
                            new Run(
                                new Text("Employee")))));

                header.Append(
                    new TableCell(
                        new Paragraph(
                            new Run(
                                new Text("Quantity")))));

                header.Append(
                    new TableCell(
                        new Paragraph(
                            new Run(
                                new Text("Date")))));

                table.Append(header);

                // DATA

                foreach (var item in manufacturing)
                {
                    TableRow row =
                        new TableRow();

                    row.Append(
                        new TableCell(
                            new Paragraph(
                                new Run(
                                    new Text(
                                        item.ProductModel?.Name ??
                                        "No product")))));

                    row.Append(
                        new TableCell(
                            new Paragraph(
                                new Run(
                                    new Text(
                                        item.Employees?.Full_Name ??
                                        "No employee")))));

                    row.Append(
                        new TableCell(
                            new Paragraph(
                                new Run(
                                    new Text(
                                        item.Quantity.ToString())))));

                    row.Append(
                        new TableCell(
                            new Paragraph(
                                new Run(
                                    new Text(
                                        item.Date
                                            .ToString("dd.MM.yyyy"))))));

                    table.Append(row);
                }

                body.Append(table);

                mainPart.Document.Append(body);

                mainPart.Document.Save();
            }

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "ManufacturingReport.docx");
        }
        // =====================================================
        // PURCHASES REPORT
        // =====================================================

        public async Task<IActionResult>
            Purchases(ReportFilterVM filter)
        {
            var purchases =
                _context.Raw_Materials_Purchase
                    .Include(x => x.RawMaterialModel)
                    .Include(x => x.Employees)
                    .AsQueryable();

            if (filter.DateFrom.HasValue)
            {
                purchases = purchases.Where(x =>
                    x.Date >= filter.DateFrom.Value);
            }

            if (filter.DateTo.HasValue)
            {
                purchases = purchases.Where(x =>
                    x.Date <= filter.DateTo.Value);
            }

            if (filter.Employees_id.HasValue)
            {
                purchases = purchases.Where(x =>
                    x.Employees_id ==
                    filter.Employees_id.Value);
            }

            var vm = new PurchaseReportVM
            {
                Purchases = await purchases
                    .OrderByDescending(x => x.Date)
                    .ToListAsync(),

                Filter = filter
            };

            return View(vm);
        }
        // =====================================================
        // PURCHASES WORD
        // =====================================================

        public async Task<IActionResult>
            PurchasesWord()
        {
            var purchases =
                await _context.Raw_Materials_Purchase
                    .Include(x => x.RawMaterialModel)
                    .Include(x => x.Employees)
                    .OrderByDescending(x => x.Date)
                    .ToListAsync();

            using var stream = new MemoryStream();

            using (WordprocessingDocument doc =
                   WordprocessingDocument.Create(
                       stream,
                       DocumentFormat.OpenXml
                           .WordprocessingDocumentType
                           .Document,
                       true))
            {
                MainDocumentPart mainPart =
                    doc.AddMainDocumentPart();

                mainPart.Document =
                    new Document();

                Body body = new Body();

                // =====================================================
                // TITLE
                // =====================================================

                body.Append(
                    new Paragraph(
                        new Run(
                            new Text(
                                "Purchases Report"))));

                // =====================================================
                // MAIN TABLE
                // =====================================================

                Table table = new Table();

                // HEADER

                TableRow header =
                    new TableRow();

                header.Append(
                    CreateCell("Raw Material"));

                header.Append(
                    CreateCell("Employee"));

                header.Append(
                    CreateCell("Quantity"));

                header.Append(
                    CreateCell("Amount"));

                header.Append(
                    CreateCell("Average Price"));

                header.Append(
                    CreateCell("Date"));

                table.Append(header);

                // DATA

                foreach (var item in purchases)
                {
                    decimal avgPrice =
                        item.Quantity > 0
                            ? item.Amount / item.Quantity
                            : 0;

                    TableRow row =
                        new TableRow();

                    row.Append(
                        CreateCell(
                            item.RawMaterialModel?.Name ??
                            "No material"));

                    row.Append(
                        CreateCell(
                            item.Employees?.Full_Name ??
                            "No employee"));

                    row.Append(
                        CreateCell(
                            item.Quantity.ToString()));

                    row.Append(
                        CreateCell(
                            item.Amount.ToString()));

                    row.Append(
                        CreateCell(
                            avgPrice.ToString("0.00")));

                    row.Append(
                        CreateCell(
                            item.Date
                                .ToString("dd.MM.yyyy")));

                    table.Append(row);
                }

                // =====================================================
                // TOTALS
                // =====================================================

                decimal totalQuantity =
                    purchases.Sum(x => x.Quantity);

                decimal totalAmount =
                    purchases.Sum(x => x.Amount);

                decimal averagePrice =
                    totalQuantity > 0
                        ? totalAmount / totalQuantity
                        : 0;

                TableRow totalRow =
                    new TableRow();

                totalRow.Append(CreateCell(""));
                totalRow.Append(CreateCell("TOTAL"));

                totalRow.Append(
                    CreateCell(
                        totalQuantity.ToString()));

                totalRow.Append(
                    CreateCell(
                        totalAmount.ToString()));

                totalRow.Append(
                    CreateCell(
                        averagePrice.ToString("0.00")));

                totalRow.Append(CreateCell(""));

                table.Append(totalRow);

                body.Append(table);

                // =====================================================
                // GROUPING TABLE
                // =====================================================

                body.Append(
                    new Paragraph(
                        new Run(
                            new Text(
                                "Grouped By Raw Material"))));

                Table groupTable =
                    new Table();

                TableRow groupHeader =
                    new TableRow();

                groupHeader.Append(
                    CreateCell("Raw Material"));

                groupHeader.Append(
                    CreateCell("Total Quantity"));

                groupHeader.Append(
                    CreateCell("Total Amount"));

                groupHeader.Append(
                    CreateCell("Average Price"));

                groupTable.Append(groupHeader);

                var grouped =
                    purchases
                        .GroupBy(x =>
                            x.RawMaterialModel?.Name)

                        .Select(x => new
                        {
                            Material = x.Key,

                            Quantity =
                                x.Sum(y => y.Quantity),

                            Amount =
                                x.Sum(y => y.Amount)
                        })

                        .ToList();

                foreach (var item in grouped)
                {
                    decimal avg =
                        item.Quantity > 0
                            ? item.Amount / item.Quantity
                            : 0;

                    TableRow row =
                        new TableRow();

                    row.Append(
                        CreateCell(
                            item.Material ??
                            "No material"));

                    row.Append(
                        CreateCell(
                            item.Quantity.ToString()));

                    row.Append(
                        CreateCell(
                            item.Amount.ToString()));

                    row.Append(
                        CreateCell(
                            avg.ToString("0.00")));

                    groupTable.Append(row);
                }

                body.Append(groupTable);

                mainPart.Document.Append(body);

                mainPart.Document.Save();
            }

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "PurchasesReport.docx");
        }
        // =====================================================
        // PURCHASES EXCEL
        // =====================================================

        public async Task<IActionResult>
            PurchasesExcel()
        {
            var purchases =
                await _context.Raw_Materials_Purchase
                    .Include(x => x.RawMaterialModel)
                    .Include(x => x.Employees)
                    .OrderByDescending(x => x.Date)
                    .ToListAsync();

            using var workbook = new XLWorkbook();

            var sheet = workbook.Worksheets
                .Add("Purchases");

            // =====================================================
            // HEADERS
            // =====================================================

            sheet.Cell(1, 1).Value = "Raw Material";
            sheet.Cell(1, 2).Value = "Employee";
            sheet.Cell(1, 3).Value = "Quantity";
            sheet.Cell(1, 4).Value = "Amount";
            sheet.Cell(1, 5).Value = "Average Price";
            sheet.Cell(1, 6).Value = "Date";

            var headerRange =
                sheet.Range("A1:F1");

            headerRange.Style.Font.Bold = true;

            // =====================================================
            // DATA
            // =====================================================

            int row = 2;

            foreach (var item in purchases)
            {
                decimal avgPrice =
                    item.Quantity > 0
                        ? item.Amount / item.Quantity
                        : 0;

                sheet.Cell(row, 1).Value =
                    item.RawMaterialModel?.Name ??
                    "No material";

                sheet.Cell(row, 2).Value =
                    item.Employees?.Full_Name ??
                    "No employee";

                sheet.Cell(row, 3).Value =
                    item.Quantity;

                sheet.Cell(row, 4).Value =
                    item.Amount;

                sheet.Cell(row, 5).Value =
                    avgPrice;

                sheet.Cell(row, 6).Value =
                    item.Date.ToString("dd.MM.yyyy");

                row++;
            }

            // =====================================================
            // TOTALS
            // =====================================================

            decimal totalQuantity =
                purchases.Sum(x => x.Quantity);

            decimal totalAmount =
                purchases.Sum(x => x.Amount);

            decimal averagePrice =
                totalQuantity > 0
                    ? totalAmount / totalQuantity
                    : 0;

            sheet.Cell(row + 1, 2).Value =
                "TOTAL";

            sheet.Cell(row + 1, 3).Value =
                totalQuantity;

            sheet.Cell(row + 1, 4).Value =
                totalAmount;

            sheet.Cell(row + 1, 5).Value =
                averagePrice;

            var totalRange =
                sheet.Range($"B{row + 1}:E{row + 1}");

            totalRange.Style.Font.Bold = true;

            // =====================================================
            // GROUPING
            // =====================================================

            row += 4;

            sheet.Cell(row, 1).Value =
                "GROUPED BY RAW MATERIAL";

            sheet.Range($"A{row}:D{row}")
                .Style.Font.Bold = true;

            row++;

            sheet.Cell(row, 1).Value =
                "Raw Material";

            sheet.Cell(row, 2).Value =
                "Total Quantity";

            sheet.Cell(row, 3).Value =
                "Total Amount";

            sheet.Cell(row, 4).Value =
                "Average Price";

            var groupHeader =
                sheet.Range($"A{row}:D{row}");

            groupHeader.Style.Font.Bold = true;

            row++;

            var grouped =
                purchases
                    .GroupBy(x =>
                        x.RawMaterialModel?.Name)

                    .Select(x => new
                    {
                        Material = x.Key,

                        Quantity =
                            x.Sum(y => y.Quantity),

                        Amount =
                            x.Sum(y => y.Amount)
                    })

                    .ToList();

            foreach (var item in grouped)
            {
                decimal avg =
                    item.Quantity > 0
                        ? item.Amount / item.Quantity
                        : 0;

                sheet.Cell(row, 1).Value =
                    item.Material;

                sheet.Cell(row, 2).Value =
                    item.Quantity;

                sheet.Cell(row, 3).Value =
                    item.Amount;

                sheet.Cell(row, 4).Value =
                    avg;

                row++;
            }

            // =====================================================
            // STYLE
            // =====================================================

            sheet.Columns().AdjustToContents();

            sheet.RangeUsed().Style.Border
                .OutsideBorder =
                    XLBorderStyleValues.Thin;

            sheet.RangeUsed().Style.Border
                .InsideBorder =
                    XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PurchasesReport.xlsx");
        }
        // =====================================================
        // PURCHASES PDF
        // =====================================================

        public async Task<IActionResult>
            PurchasesPdf()
        {
            var purchases =
                await _context.Raw_Materials_Purchase
                    .Include(x => x.RawMaterialModel)
                    .Include(x => x.Employees)
                    .OrderByDescending(x => x.Date)
                    .ToListAsync();

            PdfDocument document =
                new PdfDocument();

            document.Info.Title =
                "Purchases Report";

            PdfPage page =
                document.AddPage();

            XGraphics gfx =
                XGraphics.FromPdfPage(page);

            XFont titleFont =
                new XFont("Verdana", 18);

            XFont headerFont =
                new XFont("Verdana", 9,
                    XFontStyle.Bold);

            XFont font =
                new XFont("Verdana", 8);

            // =====================================================
            // TITLE
            // =====================================================

            gfx.DrawString(
                "Purchases Report",
                titleFont,
                XBrushes.Black,
                new XRect(
                    0,
                    20,
                    page.Width,
                    page.Height),
                XStringFormats.TopCenter);

            int y = 70;

            // =====================================================
            // TABLE HEADER
            // =====================================================

            gfx.DrawRectangle(
                XPens.Black,
                20, y, 560, 25);

            gfx.DrawString(
                "Material",
                headerFont,
                XBrushes.Black,
                new XPoint(25, y + 15));

            gfx.DrawString(
                "Employee",
                headerFont,
                XBrushes.Black,
                new XPoint(140, y + 15));

            gfx.DrawString(
                "Qty",
                headerFont,
                XBrushes.Black,
                new XPoint(270, y + 15));

            gfx.DrawString(
                "Amount",
                headerFont,
                XBrushes.Black,
                new XPoint(340, y + 15));

            gfx.DrawString(
                "Avg Price",
                headerFont,
                XBrushes.Black,
                new XPoint(430, y + 15));

            gfx.DrawString(
                "Date",
                headerFont,
                XBrushes.Black,
                new XPoint(510, y + 15));

            y += 25;

            // =====================================================
            // DATA
            // =====================================================

            foreach (var item in purchases)
            {
                decimal avgPrice =
                    item.Quantity > 0
                        ? item.Amount / item.Quantity
                        : 0;

                gfx.DrawRectangle(
                    XPens.Black,
                    20, y, 560, 25);

                gfx.DrawString(
                    item.RawMaterialModel?.Name ??
                    "No material",
                    font,
                    XBrushes.Black,
                    new XPoint(25, y + 15));

                gfx.DrawString(
                    item.Employees?.Full_Name ??
                    "No employee",
                    font,
                    XBrushes.Black,
                    new XPoint(140, y + 15));

                gfx.DrawString(
                    item.Quantity.ToString(),
                    font,
                    XBrushes.Black,
                    new XPoint(270, y + 15));

                gfx.DrawString(
                    item.Amount.ToString(),
                    font,
                    XBrushes.Black,
                    new XPoint(340, y + 15));

                gfx.DrawString(
                    avgPrice.ToString("0.00"),
                    font,
                    XBrushes.Black,
                    new XPoint(430, y + 15));

                gfx.DrawString(
                    item.Date
                        .ToString("dd.MM.yyyy"),
                    font,
                    XBrushes.Black,
                    new XPoint(510, y + 15));

                y += 25;

                // =====================================================
                // NEW PAGE
                // =====================================================

                if (y > 750)
                {
                    page = document.AddPage();

                    gfx = XGraphics
                        .FromPdfPage(page);

                    y = 40;
                }
            }

            // =====================================================
            // TOTALS
            // =====================================================

            decimal totalQuantity =
                purchases.Sum(x => x.Quantity);

            decimal totalAmount =
                purchases.Sum(x => x.Amount);

            decimal averagePrice =
                totalQuantity > 0
                    ? totalAmount / totalQuantity
                    : 0;

            gfx.DrawRectangle(
                XPens.Black,
                20, y, 560, 30);

            gfx.DrawString(
                "TOTAL",
                headerFont,
                XBrushes.Black,
                new XPoint(140, y + 18));

            gfx.DrawString(
                totalQuantity.ToString(),
                headerFont,
                XBrushes.Black,
                new XPoint(270, y + 18));

            gfx.DrawString(
                totalAmount.ToString(),
                headerFont,
                XBrushes.Black,
                new XPoint(340, y + 18));

            gfx.DrawString(
                averagePrice.ToString("0.00"),
                headerFont,
                XBrushes.Black,
                new XPoint(430, y + 18));

            y += 50;

            // =====================================================
            // GROUPING TITLE
            // =====================================================

            gfx.DrawString(
                "Grouped By Raw Material",
                headerFont,
                XBrushes.Black,
                new XPoint(20, y));

            y += 20;

            // =====================================================
            // GROUP HEADER
            // =====================================================

            gfx.DrawRectangle(
                XPens.Black,
                20, y, 400, 25);

            gfx.DrawString(
                "Material",
                headerFont,
                XBrushes.Black,
                new XPoint(25, y + 15));

            gfx.DrawString(
                "Total Qty",
                headerFont,
                XBrushes.Black,
                new XPoint(180, y + 15));

            gfx.DrawString(
                "Total Amount",
                headerFont,
                XBrushes.Black,
                new XPoint(270, y + 15));

            gfx.DrawString(
                "Avg Price",
                headerFont,
                XBrushes.Black,
                new XPoint(360, y + 15));

            y += 25;

            // =====================================================
            // GROUP DATA
            // =====================================================

            var grouped =
                purchases
                    .GroupBy(x =>
                        x.RawMaterialModel?.Name)

                    .Select(x => new
                    {
                        Material = x.Key,

                        Quantity =
                            x.Sum(y => y.Quantity),

                        Amount =
                            x.Sum(y => y.Amount)
                    })

                    .ToList();

            foreach (var item in grouped)
            {
                decimal avg =
                    item.Quantity > 0
                        ? item.Amount / item.Quantity
                        : 0;

                gfx.DrawRectangle(
                    XPens.Black,
                    20, y, 400, 25);

                gfx.DrawString(
                    item.Material ??
                    "No material",
                    font,
                    XBrushes.Black,
                    new XPoint(25, y + 15));

                gfx.DrawString(
                    item.Quantity.ToString(),
                    font,
                    XBrushes.Black,
                    new XPoint(180, y + 15));

                gfx.DrawString(
                    item.Amount.ToString(),
                    font,
                    XBrushes.Black,
                    new XPoint(270, y + 15));

                gfx.DrawString(
                    avg.ToString("0.00"),
                    font,
                    XBrushes.Black,
                    new XPoint(360, y + 15));

                y += 25;
            }

            using var stream = new MemoryStream();

            document.Save(stream);

            return File(
                stream.ToArray(),
                "application/pdf",
                "PurchasesReport.pdf");
        }
        // =====================================================
        // SALARY EXCEL
        // =====================================================

        public async Task<IActionResult>
            SalaryExcel()
        {
            var salaries =
                await _context.Salary
                    .Include(x => x.Employees)
                    .OrderByDescending(x => x.Year)
                    .ThenByDescending(x => x.Month)
                    .ToListAsync();

            using var workbook =
                new XLWorkbook();

            var sheet =
                workbook.Worksheets
                    .Add("Salary Report");

            // HEADERS

            sheet.Cell(1, 1).Value = "Employee";
            sheet.Cell(1, 2).Value = "Purchases";
            sheet.Cell(1, 3).Value = "Production";
            sheet.Cell(1, 4).Value = "Sales";
            sheet.Cell(1, 5).Value = "Operations";
            sheet.Cell(1, 6).Value = "Salary";
            sheet.Cell(1, 7).Value = "Bonus";
            sheet.Cell(1, 8).Value = "Total";
            sheet.Cell(1, 9).Value = "Status";
            sheet.Cell(1, 10).Value = "Year";
            sheet.Cell(1, 11).Value = "Month";

            var header =
                sheet.Range("A1:K1");

            header.Style.Font.Bold = true;

            int row = 2;

            foreach (var item in salaries)
            {
                sheet.Cell(row, 1).Value =
                    item.Employees?.Full_Name ??
                    "No employee";

                sheet.Cell(row, 2).Value =
                    item.NumberOfPurchases ?? 0;

                sheet.Cell(row, 3).Value =
                    item.NumberOfProductions ?? 0;

                sheet.Cell(row, 4).Value =
                    item.NumberOfSales ?? 0;

                sheet.Cell(row, 5).Value =
                    item.Common ?? 0;

                sheet.Cell(row, 6).Value =
                    item.SalaryAmount;

                sheet.Cell(row, 7).Value =
                    item.Bonus ?? 0;

                sheet.Cell(row, 8).Value =
                    item.General ?? 0;

                sheet.Cell(row, 9).Value =
                    item.Issued
                        ? "Issued"
                        : "Not Issued";

                sheet.Cell(row, 10).Value =
                    item.Year;

                sheet.Cell(row, 11).Value =
                    CultureInfo
                        .GetCultureInfo("ru-RU")
                        .DateTimeFormat
                        .GetMonthName(item.Month);

                row++;
            }

            // TOTAL ROW

            sheet.Cell(row, 1).Value = "TOTAL";

            sheet.Cell(row, 2).Value =
                salaries.Sum(x =>
                    x.NumberOfPurchases ?? 0);

            sheet.Cell(row, 3).Value =
                salaries.Sum(x =>
                    x.NumberOfProductions ?? 0);

            sheet.Cell(row, 4).Value =
                salaries.Sum(x =>
                    x.NumberOfSales ?? 0);

            sheet.Cell(row, 5).Value =
                salaries.Sum(x =>
                    x.Common ?? 0);

            sheet.Cell(row, 6).Value =
                salaries.Sum(x =>
                    x.SalaryAmount);

            sheet.Cell(row, 7).Value =
                salaries.Sum(x =>
                    x.Bonus ?? 0);

            sheet.Cell(row, 8).Value =
                salaries.Sum(x =>
                    x.General ?? 0);

            sheet.Range($"A{row}:K{row}")
                .Style.Font.Bold = true;

            sheet.Columns()
                .AdjustToContents();

            sheet.RangeUsed()
                .Style.Border
                .OutsideBorder =
                    XLBorderStyleValues.Thin;

            sheet.RangeUsed()
                .Style.Border
                .InsideBorder =
                    XLBorderStyleValues.Thin;

            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SalaryReport.xlsx");
        }
        // =====================================================
        // SALARY PDF
        // =====================================================

        public async Task<IActionResult>
            SalaryPdf()
        {
            var salaries =
                await _context.Salary
                    .Include(x => x.Employees)
                    .OrderByDescending(x => x.Year)
                    .ThenByDescending(x => x.Month)
                    .ToListAsync();

            PdfDocument document =
                new PdfDocument();

            PdfPage page =
                document.AddPage();

            XGraphics gfx =
                XGraphics.FromPdfPage(page);

            XFont titleFont =
                new XFont("Verdana", 18);

            XFont font =
                new XFont("Verdana", 8);

            gfx.DrawString(
                "Salary Report",
                titleFont,
                XBrushes.Black,
                new XRect(0, 20,
                    page.Width,
                    page.Height),
                XStringFormats.TopCenter);

            int y = 60;

            foreach (var item in salaries)
            {
                string line =
                    $"{item.Employees?.Full_Name} | " +
                    $"Salary: {item.SalaryAmount:N2} | " +
                    $"Bonus: {(item.Bonus ?? 0):N2} | " +
                    $"Total: {(item.General ?? 0):N2} | " +
                    $"{(item.Issued ? "Issued" : "Not Issued")}";

                gfx.DrawString(
                    line,
                    font,
                    XBrushes.Black,
                    new XPoint(20, y));

                y += 20;

                if (y > 750)
                {
                    page = document.AddPage();

                    gfx = XGraphics
                        .FromPdfPage(page);

                    y = 40;
                }
            }

            // TOTAL

            y += 20;

            gfx.DrawString(
                $"TOTAL SALARY: {salaries.Sum(x => x.SalaryAmount):N2}",
                font,
                XBrushes.Black,
                new XPoint(20, y));

            y += 20;

            gfx.DrawString(
                $"TOTAL BONUS: {salaries.Sum(x => x.Bonus ?? 0):N2}",
                font,
                XBrushes.Black,
                new XPoint(20, y));

            y += 20;

            gfx.DrawString(
                $"TOTAL GENERAL: {salaries.Sum(x => x.General ?? 0):N2}",
                font,
                XBrushes.Black,
                new XPoint(20, y));

            using var stream =
                new MemoryStream();

            document.Save(stream);

            return File(
                stream.ToArray(),
                "application/pdf",
                "SalaryReport.pdf");
        }
        // =====================================================
        // SALARY REPORT
        // =====================================================

        public async Task<IActionResult>
      Salary(SalaryFilterVM filter)
        {
            var salaries =
                _context.Salary
                    .Include(x => x.Employees)
                    .AsQueryable();

            if (filter.YearFrom.HasValue)
            {
                salaries = salaries.Where(x =>
                    x.Year >= filter.YearFrom.Value);
            }

            if (filter.YearTo.HasValue)
            {
                salaries = salaries.Where(x =>
                    x.Year <= filter.YearTo.Value);
            }

            if (filter.MonthFrom.HasValue)
            {
                salaries = salaries.Where(x =>
                    x.Month >= filter.MonthFrom.Value);
            }

            if (filter.MonthTo.HasValue)
            {
                salaries = salaries.Where(x =>
                    x.Month <= filter.MonthTo.Value);
            }

            // EMPLOYEE

            if (filter.Employees_id.HasValue)
            {
                salaries = salaries.Where(x =>
                    x.Employees_id ==
                    filter.Employees_id.Value);
            }

            // STATUS

            if (filter.Issued.HasValue)
            {
                salaries = salaries.Where(x =>
                    x.Issued ==
                    filter.Issued.Value);
            }

            filter.Employees =
                await _context.Employees
                    .Select(x => new SelectListItem
                    {
                        Text = x.Full_Name,
                        Value = x.Id.ToString()
                    })
                    .ToListAsync();

            var vm = new SalaryReportVM
            {
                Salaries = await salaries
                    .OrderByDescending(x => x.Year)
                    .ThenByDescending(x => x.Month)
                    .ToListAsync(),

                Filter = filter
            };

            return View(vm);
        }
        // =====================================================
        // SALARY WORD
        // =====================================================

        public async Task<IActionResult>
            SalaryWord()
        {
            var salaries =
                await _context.Salary
                    .Include(x => x.Employees)
                    .OrderByDescending(x => x.Year)
                    .ThenByDescending(x => x.Month)
                    .ToListAsync();

            using var stream =
                new MemoryStream();

            using (WordprocessingDocument doc =
                   WordprocessingDocument.Create(
                       stream,
                       WordprocessingDocumentType.Document,
                       true))
            {
                MainDocumentPart mainPart =
                    doc.AddMainDocumentPart();

                mainPart.Document =
                    new Document();

                Body body =
                    new Body();

                body.Append(
                    new Paragraph(
                        new Run(
                            new Text("Salary Report"))));

                Table table =
                    new Table();

                // HEADER

                TableRow header =
                    new TableRow();

                string[] headers =
                {
            "Employee",
            "Purchases",
            "Production",
            "Sales",
            "Operations",
            "Salary",
            "Bonus",
            "Total",
            "Status",
            "Year",
            "Month"
        };

                foreach (var h in headers)
                {
                    header.Append(
                        CreateCell(h));
                }

                table.Append(header);

                // DATA

                foreach (var item in salaries)
                {
                    TableRow row =
                        new TableRow();

                    row.Append(
                        CreateCell(
                            item.Employees?.Full_Name ??
                            "No employee"));

                    row.Append(
                        CreateCell(
                            (item.NumberOfPurchases ?? 0)
                            .ToString()));

                    row.Append(
                        CreateCell(
                            (item.NumberOfProductions ?? 0)
                            .ToString()));

                    row.Append(
                        CreateCell(
                            (item.NumberOfSales ?? 0)
                            .ToString()));

                    row.Append(
                        CreateCell(
                            (item.Common ?? 0)
                            .ToString()));

                    row.Append(
                        CreateCell(
                            item.SalaryAmount
                                .ToString("N2")));

                    row.Append(
                        CreateCell(
                            (item.Bonus ?? 0)
                                .ToString("N2")));

                    row.Append(
                        CreateCell(
                            (item.General ?? 0)
                                .ToString("N2")));

                    row.Append(
                        CreateCell(
                            item.Issued
                                ? "Issued"
                                : "Not Issued"));

                    row.Append(
                        CreateCell(
                            item.Year.ToString()));

                    row.Append(
                        CreateCell(
                            CultureInfo
                                .GetCultureInfo("ru-RU")
                                .DateTimeFormat
                                .GetMonthName(item.Month)));

                    table.Append(row);
                }

                // TOTAL ROW

                TableRow total =
                    new TableRow();

                total.Append(CreateCell("TOTAL"));

                total.Append(
                    CreateCell(
                        salaries.Sum(x =>
                            x.NumberOfPurchases ?? 0)
                        .ToString()));

                total.Append(
                    CreateCell(
                        salaries.Sum(x =>
                            x.NumberOfProductions ?? 0)
                        .ToString()));

                total.Append(
                    CreateCell(
                        salaries.Sum(x =>
                            x.NumberOfSales ?? 0)
                        .ToString()));

                total.Append(
                    CreateCell(
                        salaries.Sum(x =>
                            x.Common ?? 0)
                        .ToString()));

                total.Append(
                    CreateCell(
                        salaries.Sum(x =>
                            x.SalaryAmount)
                        .ToString("N2")));

                total.Append(
                    CreateCell(
                        salaries.Sum(x =>
                            x.Bonus ?? 0)
                        .ToString("N2")));

                total.Append(
                    CreateCell(
                        salaries.Sum(x =>
                            x.General ?? 0)
                        .ToString("N2")));

                total.Append(CreateCell(""));

                total.Append(
                    CreateCell(
                        $"Records: {salaries.Count}"));

                total.Append(CreateCell(""));

                table.Append(total);

                body.Append(table);

                mainPart.Document.Append(body);

                mainPart.Document.Save();
            }

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "SalaryReport.docx");
        }
        // =====================================================
        // CREDIT REPORT
        // =====================================================

        public async Task<IActionResult>
            Credit()
        {
            var vm = new CreditReportVM
            {
                Credits = await _context.Credit
                    .OrderByDescending(x => x.StartDate)
                    .ToListAsync()
            };

            return View(vm);
        }
        // =====================================================
        // CREDIT EXCEL
        // =====================================================

        public async Task<IActionResult>
            CreditExcel()
        {
            var credits = await _context.Credit
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var sheet = workbook.Worksheets
                .Add("Credits");

            // HEADERS

            sheet.Cell(1, 1).Value = "ID";
            sheet.Cell(1, 2).Value = "Amount";
            sheet.Cell(1, 3).Value = "Interest Rate";
            sheet.Cell(1, 4).Value = "Payment Type";
            sheet.Cell(1, 5).Value = "Years";
            sheet.Cell(1, 6).Value = "Months";
            sheet.Cell(1, 7).Value = "Remaining";
            sheet.Cell(1, 8).Value = "Penalties";
            sheet.Cell(1, 9).Value = "Start Date";

            // DATA

            int row = 2;

            foreach (var item in credits)
            {
                sheet.Cell(row, 1).Value =
                    item.id;

                sheet.Cell(row, 2).Value =
                    item.Amount;

                sheet.Cell(row, 3).Value =
                    item.AnnualInterestRate;

                sheet.Cell(row, 4).Value =
                    item.PaymentType;

                sheet.Cell(row, 5).Value =
                    item.Years;

                sheet.Cell(row, 6).Value =
                    item.Months;

                sheet.Cell(row, 7).Value =
                    item.RemainingAmount ?? 0;

                sheet.Cell(row, 8).Value =
                    item.Penalties ?? 0;

                sheet.Cell(row, 9).Value =
                    item.StartDate
                        .ToString("dd.MM.yyyy");

                row++;
            }

            // STYLE

            sheet.Columns().AdjustToContents();

            sheet.RangeUsed().Style.Border
                .OutsideBorder =
                    XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "CreditReport.xlsx");
        }
        
        // =====================================================
        // CREDIT WORD
        // =====================================================

        public async Task<IActionResult>
            CreditWord()
        {
            var credits = await _context.Credit
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();

            using var stream = new MemoryStream();

            using (WordprocessingDocument doc =
                   WordprocessingDocument.Create(
                       stream,
                       DocumentFormat.OpenXml
                           .WordprocessingDocumentType
                           .Document,
                       true))
            {
                MainDocumentPart mainPart =
                    doc.AddMainDocumentPart();

                mainPart.Document =
                    new Document();

                Body body = new Body();

                // TITLE

                body.Append(
                    new Paragraph(
                        new Run(
                            new Text(
                                "Credit Report"))));

                // TABLE

                Table table = new Table();

                // HEADER

                TableRow header =
                    new TableRow();

                header.Append(
                    CreateCell("ID"));

                header.Append(
                    CreateCell("Amount"));

                header.Append(
                    CreateCell("Interest"));

                header.Append(
                    CreateCell("Payment Type"));

                header.Append(
                    CreateCell("Years"));

                header.Append(
                    CreateCell("Months"));

                header.Append(
                    CreateCell("Remaining"));

                header.Append(
                    CreateCell("Penalties"));

                header.Append(
                    CreateCell("Start Date"));

                table.Append(header);

                // DATA

                foreach (var item in credits)
                {
                    TableRow row =
                        new TableRow();

                    row.Append(
                        CreateCell(
                            item.id.ToString()));

                    row.Append(
                        CreateCell(
                            item.Amount.ToString()));

                    row.Append(
                        CreateCell(
                            item.AnnualInterestRate
                                .ToString()));

                    row.Append(
                        CreateCell(
                            item.PaymentType));

                    row.Append(
                        CreateCell(
                            item.Years.ToString()));

                    row.Append(
                        CreateCell(
                            item.Months.ToString()));

                    row.Append(
                        CreateCell(
                            (item.RemainingAmount ?? 0)
                                .ToString()));

                    row.Append(
                        CreateCell(
                            (item.Penalties ?? 0)
                                .ToString()));

                    row.Append(
                        CreateCell(
                            item.StartDate
                                .ToString("dd.MM.yyyy")));

                    table.Append(row);
                }

                body.Append(table);

                mainPart.Document.Append(body);

                mainPart.Document.Save();
            }

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "CreditReport.docx");
        }

        
        // =====================================================
        // CREDIT PDF
        // =====================================================

        public async Task<IActionResult>
            CreditPdf()
        {
            var credits = await _context.Credit
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();

            PdfDocument document =
                new PdfDocument();

            document.Info.Title =
                "Credit Report";

            PdfPage page =
                document.AddPage();

            XGraphics gfx =
                XGraphics.FromPdfPage(page);

            XFont titleFont =
                new XFont("Verdana", 18);

            XFont headerFont =
                new XFont("Verdana", 10,
                    XFontStyle.Bold);

            XFont font =
                new XFont("Verdana", 9);

            // TITLE

            gfx.DrawString(
                "Credit Report",
                titleFont,
                XBrushes.Black,
                new XRect(0, 20,
                    page.Width,
                    page.Height),
                XStringFormats.TopCenter);

            int y = 70;

            // TABLE HEADER

            gfx.DrawString(
                "ID",
                headerFont,
                XBrushes.Black,
                new XPoint(20, y));

            gfx.DrawString(
                "Amount",
                headerFont,
                XBrushes.Black,
                new XPoint(60, y));

            gfx.DrawString(
                "Interest",
                headerFont,
                XBrushes.Black,
                new XPoint(140, y));

            gfx.DrawString(
                "Years",
                headerFont,
                XBrushes.Black,
                new XPoint(220, y));

            gfx.DrawString(
                "Remaining",
                headerFont,
                XBrushes.Black,
                new XPoint(280, y));

            gfx.DrawString(
                "Date",
                headerFont,
                XBrushes.Black,
                new XPoint(380, y));

            y += 20;

            // DATA

            foreach (var item in credits)
            {
                gfx.DrawString(
                    item.id.ToString(),
                    font,
                    XBrushes.Black,
                    new XPoint(20, y));

                gfx.DrawString(
                    item.Amount.ToString(),
                    font,
                    XBrushes.Black,
                    new XPoint(60, y));

                gfx.DrawString(
                    item.AnnualInterestRate
                        .ToString(),
                    font,
                    XBrushes.Black,
                    new XPoint(140, y));

                gfx.DrawString(
                    item.Years.ToString(),
                    font,
                    XBrushes.Black,
                    new XPoint(220, y));

                gfx.DrawString(
                    (item.RemainingAmount ?? 0)
                        .ToString(),
                    font,
                    XBrushes.Black,
                    new XPoint(280, y));

                gfx.DrawString(
                    item.StartDate
                        .ToString("dd.MM.yyyy"),
                    font,
                    XBrushes.Black,
                    new XPoint(380, y));

                y += 20;

                // NEW PAGE

                if (y > 750)
                {
                    page = document.AddPage();

                    gfx = XGraphics
                        .FromPdfPage(page);

                    y = 40;
                }
            }

            using var stream = new MemoryStream();

            document.Save(stream);

            return File(
                stream.ToArray(),
                "application/pdf",
                "CreditReport.pdf");
        }
    }
}