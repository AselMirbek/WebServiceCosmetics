using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebServiceCosmetics.Controllers
{
    [Authorize(Roles = "Директор")]

    public class DirectorController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
