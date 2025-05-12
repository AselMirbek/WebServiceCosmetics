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
                    await UpdateBudgetAmount("Credit", newCredit.Amount);

                    TempData["Message"] = "Кредит успешно создан, и график платежей сгенерирован.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Message"] = $"Ошибка: {ex.Message}";
                }
            }

            return View(newCredit);
        }


      
        // Метод для обновления бюджета (увеличение или уменьшение суммы)
        private async Task UpdateBudgetAmount(string transactionType, decimal amount)
        {
            // Ищем текущую сумму в таблице Budget
            var budget = await _context.Budget.FirstOrDefaultAsync();
            if (budget == null)
            {
                throw new Exception("Budget not found!");
            }

            // В зависимости от типа транзакции увеличиваем или уменьшаем сумму
            if (transactionType == "Credit")
            {
                budget.Amount += amount; // Увеличиваем сумму при получении кредита
            }
            else if (transactionType == "Payment")
            {
                budget.Amount -= amount; // Уменьшаем сумму при выплате кредита
            }
            else
            {
                throw new ArgumentException("Invalid transaction type");
            }

            // Сохраняем изменения в базе данных
            _context.Budget.Update(budget);
            await _context.SaveChangesAsync();
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
                    await UpdateBudgetAmount("Payment", payment.PaymentAmount);
                    // Обновляем бюджет (уменьшаем на сумму платежа)

                }


                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = payment.Credit_id });
        }

    }
}

public class CreditDetailsViewModel
{
    public CreditModel Credit { get; set; }
    public List<PaymentsModel> Payment { get; set; }
}
