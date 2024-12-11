using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Odev.Data;
using Odev.Models;
using Microsoft.EntityFrameworkCore;

namespace Odev.Controllers
{
    [Authorize(Roles = "User")]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Randevu oluşturma sayfası
        public async Task<IActionResult> Create(DateTime? selectedDate)
        {
            await FillViewDataAsync(selectedDate);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment, int[] serviceIds)
        {
            if (ModelState.IsValid)
            {
                // Geçmiş tarih kontrolü
                if (appointment.AppointmentDate < DateTime.Now)
                {
                    ModelState.AddModelError("", "Geçmiş bir tarih için randevu oluşturamazsınız.");
                    await FillViewDataAsync(null);
                    return View(appointment);
                }

                // Çalışan uygunluk kontrolü
                var isEmployeeAvailable = !await _context.Appointments
                    .AnyAsync(a => a.EmployeeId == appointment.EmployeeId &&
                                   a.AppointmentDate == appointment.AppointmentDate);

                if (!isEmployeeAvailable)
                {
                    ModelState.AddModelError("", "Seçilen çalışan bu tarih ve saat için uygun değil.");
                    await FillViewDataAsync(null);
                    return View(appointment);
                }

                // Kullanıcı ve randevu kaydı
                var user = await _userManager.GetUserAsync(User);
                appointment.UserId = user.Id;

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                // Servislerin kaydedilmesi
                foreach (var serviceId in serviceIds)
                {
                    var appointmentService = new AppointmentService
                    {
                        AppointmentId = appointment.Id,
                        ServiceId = serviceId
                    };
                    _context.AppointmentServices.Add(appointmentService);
                }
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }

            await FillViewDataAsync(null);
            return View(appointment);
        }

        // ViewData'yı doldurmak için yardımcı metot
        private async Task FillViewDataAsync(DateTime? selectedDate)
        {
            ViewData["Services"] = await _context.Services.ToListAsync();

            if (selectedDate.HasValue)
            {
                var unavailableEmployeeIds = await _context.Appointments
                    .Where(a => a.AppointmentDate.Date == selectedDate.Value.Date &&
                                a.AppointmentDate.Hour == selectedDate.Value.Hour)
                    .Select(a => a.EmployeeId)
                    .ToListAsync();

                ViewData["Employees"] = await _context.Employees
                    .Where(e => !unavailableEmployeeIds.Contains(e.Id))
                    .ToListAsync();
            }
            else
            {
                ViewData["Employees"] = await _context.Employees.ToListAsync();
            }
        }
    }
}
