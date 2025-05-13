using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebServiceCosmetics.Models;

namespace WebServiceCosmetics.Controllers
{
    public class HomeController : Controller
    {


        public IActionResult Index()
        {

            return View();
        }
        [HttpPost]
        [HttpPost]
        public IActionResult SelectRole(string role)
        {
            TempData["SelectedRole"] = role;
            HttpContext.Session.SetString("UserRole", role); // ✅ Сохраняем в сессию

            return RedirectToAction("RolePanel");
        }

        public IActionResult RolePanel()
        {
            string role = TempData["SelectedRole"] as string;
            ViewBag.Role = role;
            return View();
        }

        public IActionResult AdminView()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin")
                return RedirectToAction("AccessDenied");
            return View();
        }

        public IActionResult DirectorView()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Director")
                return RedirectToAction("AccessDenied");
            return View();
        }

        public IActionResult ManagerView()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Manager")
                return RedirectToAction("AccessDenied");
            return View();
        }

        public IActionResult AccountantView()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Accountant")
                return RedirectToAction("AccessDenied");
            return View();
        }

        public IActionResult TechnologistView()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Technologist")
                return RedirectToAction("AccessDenied");
            return View();
        }
        public IActionResult AccessDenied()
        {
            return Content("Доступ запрещён.");
        }
    }


}