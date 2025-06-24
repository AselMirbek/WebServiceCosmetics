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
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Credit.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(c => c.StartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(c => c.StartDate <= endDate.Value);
            }

            // Сохраняем выбранные даты в ViewData, чтобы показать их в форме
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd") ?? "";
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd") ?? "";

            var credits = await query.ToListAsync();
            return View(credits);
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
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }


           
            return View();
        }

        // POST: CreditController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreditModel newCredit)
        {
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

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
                new SqlParameter("@StartDate", newCredit.StartDate),
                 new SqlParameter("@PaymentType", newCredit.PaymentType)
            };

                    await _context.Database.ExecuteSqlRawAsync("EXEC CreateCreditWithSchedule @CreditId, @LoanAmount, @AnnualInterestRate, @LoanTermYears, @StartDate,@PaymentType", parameters);

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





     
        [HttpGet("Credit/Details/{id}/{startDate}/{endDate}")]
        public IActionResult Details(int id, DateTime startDate, DateTime endDate)
        { 
           

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
        // оплата
        [HttpPost]
        public async Task<IActionResult> Pay(int paymentId )
        {
            if (User.IsInRole("Директор"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            try
            {
                var parameters = new[]
       {
            new SqlParameter("@PaymentId", paymentId),
        };

                await _context.Database.ExecuteSqlRawAsync("EXEC PayCreditPayment @PaymentId", parameters);

                TempData["Message"] = "Платеж успешно выполнен.";
            }
            catch (SqlException ex)
            {
                TempData["Message"] = $"Ошибка при оплате: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Произошла ошибка: {ex.Message}";
            }

            // Получаем Credit_id для редиректа
            var payment = await _context.Payment.FirstOrDefaultAsync(p => p.id == paymentId);
            if (payment == null)
                return NotFound();

            return RedirectToAction("Details", new { id = payment.Credit_id });
        }

    }
}

public class CreditDetailsViewModel
{
    public CreditModel Credit { get; set; }
    public List<PaymentsModel> Payment { get; set; }
}
