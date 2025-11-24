using Microsoft.AspNetCore.Mvc;

namespace Project_Photo.Areas.Seller.Controllers
{        
    [Area("Seller")]
    public class SalesHomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
