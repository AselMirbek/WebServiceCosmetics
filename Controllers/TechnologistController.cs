using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebServiceCosmetics.Controllers
{
    public class TechnologistController : Controller
    {
        [Authorize(Roles = "Технолог")]

        public IActionResult Index()
        {
            return View();
        }
    }
}
