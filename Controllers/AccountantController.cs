using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebServiceCosmetics.Controllers
{
    [Authorize(Roles = "Бухгалтер")]

    public class AccountantController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
