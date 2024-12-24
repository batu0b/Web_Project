using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Odev.Data;
using Odev.Models;
using Microsoft.EntityFrameworkCore;
using Odev.ViewModels;

namespace Odev.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Admin ana sayfası
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ManageSalon()
        {
            var salon = await _context.Salons.FirstOrDefaultAsync();
            if (salon == null)
            {
                salon = new Salon
                {
                    Name = "Varsayılan Salon",
                    StartTime = TimeSpan.FromHours(8),
                    EndTime = TimeSpan.FromHours(20)
                };
                _context.Salons.Add(salon);
                await _context.SaveChangesAsync();
            }
            return View(salon);
        }

        public async Task<IActionResult> EditSalon(int id)
        {
            var salon = await _context.Salons.FindAsync(id);
            if (salon == null)
            {
                return NotFound();
            }
            return PartialView("_EditSalonModal", salon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSalon(Salon salon)
        {
            if (ModelState.IsValid)
            {
                _context.Salons.Update(salon);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManageSalon");
            }
            return PartialView("_EditSalonModal", salon);
        }




        // Servis ekleme işlemleri
        public IActionResult AddService()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddService(Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Services.Add(service);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(service);
        }

        public IActionResult AddEmployee()
        {
            ViewData["Services"] = _context.Services.ToList();
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployee(Employee employee, int[] serviceIds)
        {
            if (ModelState.IsValid)
            {
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Çalışan ile seçilen servisleri ilişkilendirme
                foreach (var serviceId in serviceIds)
                {
                    var employeeService = new EmployeeService
                    {
                        EmployeeId = employee.Id,
                        ServiceId = serviceId
                    };
                    _context.EmployeeServices.Add(employeeService);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewData["Services"] = _context.Services.ToList();
            return View(employee);
        }
    

        // Randevuları onaylama işlemleri
        public async Task<IActionResult> ApproveAppointments()
        {
            var appointments = await _context.Appointments.Include(a => a.User).Include(a => a.Employee).ToListAsync();
            return View(appointments);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.IsApproved = true;
                appointment.ApprovalDate = DateTime.Today;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ApproveAppointments");
        }

        // Servisleri listeleme işlemi
        public async Task<IActionResult> ManageServices()
        {
            var services = await _context.Services.ToListAsync();
            return View(services);
        }

        // Servis güncelleme işlemi (Get)
        public async Task<IActionResult> EditService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            return PartialView("_EditServiceModal", service);
        }

        // Servis güncelleme işlemi (Post)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Services.Update(service);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManageServices");
            }
            return PartialView("_EditServiceModal", service);
        }

        // Servis silme işlemi
        [HttpPost]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();
            return RedirectToAction("ManageServices");
        }

        public async Task<IActionResult> ManageEmployees()
        {
            var employees = await _context.Employees
                .Include(e => e.EmployeeServices) // Çalışan ile ilişkilendirilmiş servisleri dahil et
                .ThenInclude(es => es.Service)   // Servis bilgilerini dahil et
                .ToListAsync();

            return View(employees);
        }


        // Çalışan güncelleme işlemi (Get)
        public async Task<IActionResult> EditEmployee(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.EmployeeServices)
                .ThenInclude(es => es.Service)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            ViewData["Services"] = await _context.Services.ToListAsync();
            return PartialView("_EditEmployeeModal", employee);
        }


        // Çalışan güncelleme işlemi (Post)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(Employee employee, int[] serviceIds)
        {
            if (ModelState.IsValid)
            {
                // Çalışan bilgilerini güncelle
                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();

                // Çalışanın servislerini güncelle
                var existingServices = await _context.EmployeeServices
                    .Where(es => es.EmployeeId == employee.Id)
                    .ToListAsync();

                // Var olan servislerden kaldırılacakları sil
                foreach (var existingService in existingServices)
                {
                    if (!serviceIds.Contains(existingService.ServiceId))
                    {
                        _context.EmployeeServices.Remove(existingService);
                    }
                }

                // Yeni servisleri ekle
                foreach (var serviceId in serviceIds)
                {
                    if (!existingServices.Any(es => es.ServiceId == serviceId))
                    {
                        var newEmployeeService = new EmployeeService
                        {
                            EmployeeId = employee.Id,
                            ServiceId = serviceId
                        };
                        _context.EmployeeServices.Add(newEmployeeService);
                    }
                }//jjjj

                await _context.SaveChangesAsync();
                return RedirectToAction("ManageEmployees");
            }

            ViewData["Services"] = await _context.Services.ToListAsync();
            return PartialView("_EditEmployeeModal", employee);
        }

        // Çalışan silme işlemi
        [HttpPost]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return RedirectToAction("ManageEmployees");
        }
        public async Task<IActionResult> EmployeeEarnings()//k
        {
            var employeeEarnings = await _context.Employees
                .Include(e => e.Appointments)
                .ThenInclude(a => a.AppointmentServices)
                .ThenInclude(a => a.Service)
                .Select(e => new
                {
                    EmployeeName = e.Name,
                    TotalEarnings = e.Appointments
                        .SelectMany(a => a.AppointmentServices)//j
                        .Sum(a => a.Service.Price)
                })
                .ToListAsync();

            var earningsViewModel = employeeEarnings.Select(e => new EmployeeEarningsViewModel
            {
                EmployeeName = e.EmployeeName,
                TotalEarnings = e.TotalEarnings
            }).ToList();

            return View(earningsViewModel);
        }


    }
}
