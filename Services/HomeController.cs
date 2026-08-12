using Microsoft.AspNetCore.Mvc;

namespace MedVaultAPI.Services
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
