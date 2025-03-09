using Microsoft.AspNetCore.Mvc;
using WebServiceCosmetics.Data;

using WebServiceCosmetics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebServiceCosmetics.Controllers
{

   // [Authorize]
    public class EmployerController : Controller

    {
        private readonly ApplicationDbContext _context;

        public EmployerController(ApplicationDbContext context)
        {
            _context = context;
        }
     

        // GET: RawMaterials/Create
        public IActionResult Create()
        {
            ViewBag.Positions = new SelectList(_context.Positions, "Id", "Name");
            return View(new Employer());
        }

        // POST: RawMaterials/Create


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employer employer)
        {
            // Проверка обязательности Unit_id
            if (employer.Position_id == null)
            {
                ModelState.AddModelError("Position_id", "Выберите единицу измерения");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Positions = new SelectList(_context.Positions, "Id", "Name", employer.Position_id);
                return View(employer); 
            }
            // Используем транзакцию для обеспечения атомарности операции
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Добавляем объект в контекст базы данных
                _context.Add(employer);

                // Сохраняем изменения в базе данных
                await _context.SaveChangesAsync();

                // Подтверждаем транзакцию
                await transaction.CommitAsync();

                // После успешного сохранения данных, перенаправляем на другую страницу (например, Index)
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // В случае ошибки откатываем транзакцию
                await transaction.RollbackAsync();

                // Логируем ошибку (можно выводить в консоль или в логи)
                Console.WriteLine($"Ошибка при сохранении данных: {ex.Message}");

                // Возвращаем представление с моделью и ошибками
                ModelState.AddModelError("", "Ошибка при сохранении данных. Пожалуйста, попробуйте снова.");
                return View(employer);
            }

           

        }
        // Действие для удаления сотрудника
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var employer = await _context.Employees.FindAsync(id);
            if (employer == null)
            {
                return NotFound();
            }

            _context.Employees.Remove(employer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Действие для обновления информации о сотруднике
        [HttpPost]
        public async Task<IActionResult> Update(int id, Employer updatedEmployer)
        {
            if (id != updatedEmployer.Id)
            {
                return BadRequest();
            }

            var employer = await _context.Employees.FindAsync(id);
            if (employer == null)
            {
                return NotFound();
            }

            employer.Full_Name = updatedEmployer.Full_Name;
            employer.Position_id = updatedEmployer.Position_id;
            employer.Salary = updatedEmployer.Salary;
            employer.Address = updatedEmployer.Address;
            employer.Phone = updatedEmployer.Phone;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        
    }
        // Метод для получения всех сотрудников
        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees.ToListAsync();
            return View(employees);
        }
    }
}

