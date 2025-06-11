using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebServiceCosmetics.Controllers
{
    [Authorize(Roles = "Менеджер")]

    public class ManagerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
