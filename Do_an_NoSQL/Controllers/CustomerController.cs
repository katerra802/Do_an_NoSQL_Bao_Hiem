using Microsoft.AspNetCore.Mvc;

namespace Do_an_NoSQL.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
