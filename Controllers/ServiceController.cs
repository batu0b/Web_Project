using Microsoft.AspNetCore.Mvc;
using WebProject.Models;
using WebProject.DbRelated;
using System.Linq;

namespace WebProject.Controllers
{
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Service
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            var services = _context.Services.ToList();
            return View(services);
        }

        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            ViewBag.Employees = _context.Employees.ToList();
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Services.Add(service);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Employees = _context.Employees.ToList();
            return View(service);
        }


        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            var service = _context.Services.Find(id);
            if (service == null)
            {
                return NotFound();
            }

            ViewBag.Employees = _context.Employees.ToList();
            return View(service);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Service service)
        {
            if (id != service.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingService = _context.Services.Find(id);
                if (existingService != null)
                {
                    existingService.Name = service.Name;
                    existingService.Duration = service.Duration;
                    existingService.Price = service.Price;
                    existingService.SpecializationRequired = service.SpecializationRequired;

                    _context.SaveChanges();
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Employees = _context.Employees.ToList();
            return View(service);
        }


        // GET: Service/Delete/{id}
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            var service = _context.Services.Find(id);
            if (service == null)
            {
                return NotFound();
            }
            return View(service);
        }

        // POST: Service/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var service = _context.Services.Find(id);
            if (service != null)
            {
                _context.Services.Remove(service);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Service/Details/{id}
        public IActionResult Details(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            var service = _context.Services.Find(id);
            if (service == null)
            {
                return NotFound();
            }
            return View(service);
        }
    }
}
