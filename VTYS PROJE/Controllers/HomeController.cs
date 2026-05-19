using Microsoft.AspNetCore.Mvc;

namespace VTYS_PROJE.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
