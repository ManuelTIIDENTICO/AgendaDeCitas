using Microsoft.AspNetCore.Mvc;

namespace AgendaDeCitas.Controllers
{
    public class CiudadesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult SeleccionarFecha(string ciudad)
        {
            ViewBag.Ciudad = ciudad;
            return View();
        }
    }
}
