using Microsoft.AspNetCore.Mvc;

namespace Project_Photo.Areas.Admin.Controllers
{
        [Area("Admin")]
    public class ManagementHomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
