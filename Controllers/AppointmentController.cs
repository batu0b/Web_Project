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
            if (!ModelState.IsValid)
            {
                await FillViewDataAsync(null);
                return View(viewModel);
            }

            // Kullanıcı bilgisi
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError("", "Kullanıcı oturumu bulunamadı. Lütfen giriş yapın.");
                await FillViewDataAsync(null);
                return View(viewModel);
            }

            // Geçmiş tarih kontrolü
            if (viewModel.Date < DateTime.Now)
            {
                ModelState.AddModelError("", "Geçmiş bir tarih için randevu oluşturamazsınız.");
                await FillViewDataAsync(null);
                return View(viewModel);
            }

            // Seçilen servislerin toplam süresi hesaplanıyor
            var totalDuration = await _context.Services
                .Where(s => serviceIds.Contains(s.Id))
                .SumAsync(s => s.Duration);

            var appointmentEndTime = viewModel.Date.AddMinutes(totalDuration);

            // Kullanıcı musaitlik kontrolü
            var isUserAvailable = !await _context.Appointments
                .Where(a => a.UserId == user.Id)
                .AnyAsync(a =>
                    (viewModel.Date < a.AppointmentDate.AddMinutes(a.AppointmentServices.Sum(s => s.Service!.Duration)) &&
                     appointmentEndTime > a.AppointmentDate));

            if (!isUserAvailable)
            {
                ModelState.AddModelError("", "Seçilen tarih ve saat aralığında başka bir randevunuz bulunuyor.");
                await FillViewDataAsync(null);
                return View(viewModel);
            }

            // Çalışan uygunluk kontrolü
            var isEmployeeAvailable = !await _context.Appointments
                .Where(a => a.EmployeeId == viewModel.EmployeeId)
                .AnyAsync(a =>
                    (viewModel.Date < a.AppointmentDate.AddMinutes(a.AppointmentServices.Sum(a => a.Service!.Duration)) &&
                     appointmentEndTime > a.AppointmentDate));

            if (!isEmployeeAvailable)
            {
                ModelState.AddModelError("", "Seçilen çalışan bu tarih ve saat aralığında uygun değil.");
                await FillViewDataAsync(null);
                return View(viewModel);
            }

            // Yeni randevu oluştur
            var appointment = new Appointment
            {
                AppointmentDate = viewModel.Date,
                UserId = user.Id,
                EmployeeId = viewModel.EmployeeId,
                IsApproved = false // Varsayılan olarak onaysız
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Seçilen servisleri randevuya bağla
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

        [HttpGet]
        public async Task<IActionResult> GetAvailableEmployees(DateTime date, string serviceIds)
        {
            var serviceIdArray = serviceIds.Split(',').Select(int.Parse).ToArray();

            var availableEmployees = await _context.Employees
                .Include(e => e.EmployeeServices)
                .Where(e => e.EmployeeServices.Any(es => serviceIdArray.Contains(es.ServiceId)))
                .Select(e => new
                {
                    id = e.Id,
                    name = e.Name,
                    isAvailable = !_context.Appointments
                        .Any(a => a.EmployeeId == e.Id &&
                                  date < a.AppointmentDate.AddMinutes(a.AppointmentServices.Sum(a => a.Service!.Duration)) &&
                                  a.AppointmentDate < date.AddMinutes(480))
                })
                .ToListAsync();

            return Json(availableEmployees);
        }

        [HttpGet]
        public async Task<IActionResult> CheckUserAvailability(DateTime date, int[] serviceIds)
        {
            // Seçilen servislerin toplam süresi hesaplanıyor
            var totalDuration = await _context.Services
                .Where(s => serviceIds.Contains(s.Id))
                .SumAsync(s => s.Duration);

            var appointmentEndTime = date.AddMinutes(totalDuration);

            // Kullanıcının diğer randevularını kontrol et
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { isAvailable = false, message = "Kullanıcı bulunamadı." });
            }

            var isUserAvailable = !await _context.Appointments
                .Where(a => a.UserId == user.Id)
                .AnyAsync(a =>
                    (date < a.AppointmentDate.AddMinutes(a.AppointmentServices.Sum(s => s.Service!.Duration)) &&
                     appointmentEndTime > a.AppointmentDate));

            return Json(new { isAvailable = isUserAvailable });
        }


        private async Task FillViewDataAsync(DateTime? selectedDate, int[]? selectedServiceIds = null)
        {
            ViewData["Services"] = await _context.Services.ToListAsync();

            if (selectedServiceIds != null && selectedServiceIds.Any())
            {
                var availableEmployees = await _context.Employees
                    .Include(e => e.EmployeeServices)
                    .Where(e => e.EmployeeServices.Any(es => selectedServiceIds.Contains(es.ServiceId)))
                    .ToListAsync();

                if (selectedDate.HasValue)
                {
                    var unavailableEmployeeIds = await _context.Appointments
                        .Where(a =>
                            a.AppointmentDate < selectedDate.Value.AddMinutes(480) &&
                            selectedDate.Value < a.AppointmentDate.AddMinutes(a.AppointmentServices.Sum(a => a.Service.Duration)))
                        .Select(a => a.EmployeeId)
                        .ToListAsync();

                    ViewData["Employees"] = availableEmployees
                        .Where(e => !unavailableEmployeeIds.Contains(e.Id))
                        .ToList();
                }
                else
                {
                    ViewData["Employees"] = availableEmployees;
                }
            }
            else
            {
                ViewData["Employees"] = new List<Employee>();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments()
        {
            // Oturum açmış kullanıcı bilgisi alınıyor
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("Kullanıcı oturumu bulunamadı. Lütfen giriş yapın.");
            }

            // Kullanıcıya ait tüm randevuları getir
            var userAppointments = await _context.Appointments
                .Include(a => a.Employee) // Çalışan bilgilerini dahil et
                .Include(a => a.AppointmentServices)
                .ThenInclude(a => a.Service) // Servis bilgilerini dahil et
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            return View(userAppointments); // View'e yönlendir
        }

    }

}
