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
using System.Data;
using Microsoft.AspNetCore.Http.HttpResults;

using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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

                // Проверка на будущий период (на стороне сервера)
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
                ViewBag.ErrorMessage = $"Ошибка: {ex.Message}";
                return View(new List<SalaryModel>());
            }
        }

        // Вызов хранимой процедуры SP_CalculateSalaries
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
                            Employees_id = reader.GetInt32(1), // short (возможно, лучше int)
                            Year = reader.GetInt32(3),
                            Month = reader.GetInt32(4),
                            NumberOfPurchases = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                            NumberOfProductions = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6),
                            NumberOfSales = reader.IsDBNull(7) ? null : (int?)reader.GetInt32(7),
                            Common = reader.IsDBNull(8) ? null : (int?)reader.GetInt32(8),
                            SalaryAmount = reader.IsDBNull(9) ? 0 : reader.GetDecimal(9),
                            Bonus = reader.IsDBNull(10) ? null : (decimal?)reader.GetDecimal(10),
                            General = reader.IsDBNull(11) ? null : (decimal?)reader.GetDecimal(11),
                            Issued = reader.GetBoolean(12),
                            Employees = new Employer { Full_Name = reader.GetString(2) }
                        };

                        result.Add(salary);
                    }
                }
            }

            return result;
        }

        // Обновление поля General через SP_UpdateSalaryGeneralAmount (AJAX)
        [HttpPost]
        public async Task<IActionResult> UpdateSalary(int id, double generalAmount)
        {
            // 1. Временное хранение в параметрах метода
            // 2. Обновление в контексте EF Core
            try
            {
                var successParam = new SqlParameter("@Success", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                var messageParam = new SqlParameter("@Message", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };

                var parameters = new[]
                {
                    new SqlParameter("@SalaryID", id),
                    new SqlParameter("@NewGeneralAmount", generalAmount),// Присваивание нового значения
                    successParam,
                    messageParam
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC SP_UpdateSalaryGeneralAmount @SalaryID, @NewGeneralAmount, @Success OUTPUT, @Message OUTPUT",
                    parameters);

                bool success = (bool)successParam.Value;
                string message = (string)messageParam.Value;

                return Json(new { success, message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        // Выплата зарплат за указанный период через SP_PaySalaries
        [HttpPost]
        public async Task<IActionResult> PaySalaries(int year, int month)
        {
            try
            {
                var successParam = new SqlParameter("@Success", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                var messageParam = new SqlParameter("@Message", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };

                var parameters = new[]
                {
                    new SqlParameter("@Year", year),
                    new SqlParameter("@Month", month),
                    successParam,
                    messageParam
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC SP_PaySalaries @Year, @Month, @Success OUTPUT, @Message OUTPUT",
                    parameters);

                bool success = (bool)successParam.Value;
                string message = (string)messageParam.Value;

                if (success)
                {
                    // Обновляем локально поле Issued для зарплат, чтобы данные актуализировать
                    var salariesToUpdate = await _context.Salary
                        .Where(s => s.Year == year && s.Month == month && !s.Issued)
                        .ToListAsync();

                    foreach (var salary in salariesToUpdate)
                    {
                        salary.Issued = true;
                    }

                    await _context.SaveChangesAsync();
                }

                return Json(new { success, message });
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
