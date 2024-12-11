using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Odev.Data;
using Odev.Models;
using Microsoft.EntityFrameworkCore;
using Odev.ViewModels;

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
        public async Task<IActionResult> Create(AppointmentViewModel viewModel, int[] serviceIds)
        {
            if (ModelState.IsValid)
            {
                // Geçmiş tarih kontrolü
                if (viewModel.Date < DateTime.Now)
                {
                    ModelState.AddModelError("", "Geçmiş bir tarih için randevu oluşturamazsınız.");
                    await FillViewDataAsync(null);
                    return View(viewModel);
                }

                // Çalışan uygunluk kontrolü
                var isEmployeeAvailable = !await _context.Appointments
                    .AnyAsync(a => a.EmployeeId == viewModel.EmployeeId &&
                                   a.AppointmentDate == viewModel.Date);

                if (!isEmployeeAvailable)
                {
                    ModelState.AddModelError("", "Seçilen çalışan bu tarih ve saat için uygun değil.");
                    await FillViewDataAsync(null);
                    return View(viewModel);
                }

                // Kullanıcı bilgisi alınıyor
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    ModelState.AddModelError("", "Kullanıcı oturumu bulunamadı. Lütfen giriş yapın.");
                    await FillViewDataAsync(null);
                    return View(viewModel);
                }

                // Appointment nesnesini doldur
                var appointment = new Appointment
                {
                    AppointmentDate = viewModel.Date,
                    EmployeeId = viewModel.EmployeeId,
                    UserId = user.Id,
                    IsApproved = false, // Varsayılan olarak onaysız
                    ApprovalDate = null // İlk başta null
                };

                // Randevu kaydı
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

            // Hata mesajlarını logla
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
            }

            await FillViewDataAsync(null);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetApprovedAppointments()
        {
            // Oturum açmış kullanıcı bilgisi alınıyor
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("Kullanıcı oturumu bulunamadı. Lütfen giriş yapın.");
            }

            // Kullanıcıya ait onaylanmış randevuları getir
            var approvedAppointments = await _context.Appointments
                .Include(a => a.Employee) // Çalışan bilgilerini dahil et
                .Include(a => a.AppointmentServices)
                .ThenInclude(a => a.Service) // Servis bilgilerini dahil et
                .Where(a => a.UserId == user.Id && a.IsApproved)
                .ToListAsync();

            return View(approvedAppointments); // View'e yönlendir
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
