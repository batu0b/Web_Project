using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Odev.Data;
using Odev.Models;
using Microsoft.EntityFrameworkCore;
using Odev.ViewModels;
using System.Net.Http.Headers;
using Newtonsoft.Json;


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

            // Salon çalışma saatleri kontrolü
            var salon = await _context.Salons.FirstOrDefaultAsync();
            if (salon == null)
            {
                ModelState.AddModelError("", "Salon bilgisi bulunamadı. Lütfen yöneticinize başvurun.");
                return View(viewModel);
            }

            var appointmentStartTime = viewModel.Date.TimeOfDay;
            var totalDuration = await _context.Services
                .Where(s => serviceIds.Contains(s.Id))
                .SumAsync(s => s.Duration);

            var appointmentEndTime = viewModel.Date.AddMinutes(totalDuration).TimeOfDay;

            if (appointmentStartTime < salon.StartTime || appointmentEndTime > salon.EndTime)
            {
                ModelState.AddModelError("", $"Randevu sadece salonun çalışma saatleri içinde yapılabilir: {salon.StartTime} - {salon.EndTime}");
                await FillViewDataAsync(null);
                return View(viewModel);
            }

            // Kullanıcı müsaitlik kontrolü
            var isUserAvailable = !await _context.Appointments
                .Where(a => a.UserId == user.Id)
                .AnyAsync(a =>
                    (viewModel.Date < a.AppointmentDate.AddMinutes(a.AppointmentServices.Sum(s => s.Service!.Duration)) &&
                     viewModel.Date.AddMinutes(totalDuration) > a.AppointmentDate));

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
                     viewModel.Date.AddMinutes(totalDuration) > a.AppointmentDate));

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
            // 1. `serviceIdArray`'yi oluştur ve kontrol et
            var serviceIdArray = serviceIds.Split(',').Select(int.Parse).ToArray();
            Console.WriteLine($"Service ID Array: {string.Join(",", serviceIdArray)}");

            // 2. Tüm çalışanları ve ilişkili servislerini getir
            var employees = await _context.Employees
                .Include(e => e.EmployeeServices)
                .ToListAsync();

            // Debug: Çalışanları ve servislerini yazdır
            foreach (var employee in employees)
            {
                var employeeServiceIds = employee.EmployeeServices.Select(es => es.ServiceId).ToList();
                Console.WriteLine($"Employee: {employee.Name}, Services: {string.Join(",", employeeServiceIds)}");
            }

            // 3. Çalışanları servis ID'lerine göre filtrele
            var filteredEmployees = employees
                .Where(e =>
                    serviceIdArray.All(serviceId => e.EmployeeServices.Any(es => es.ServiceId == serviceId))
                )
                .ToList();

            // Debug: Filtrelenmiş çalışanları yazdır
            foreach (var employee in filteredEmployees)
            {
                Console.WriteLine($"Filtered Employee: {employee.Name}");
            }

            // 4. Çalışanların uygunluk durumunu kontrol et
            var availableEmployees = filteredEmployees
                .Select(e => new
                {
                    id = e.Id,
                    name = e.Name,
                    isAvailable = !_context.Appointments
                        .Any(a => a.EmployeeId == e.Id &&
                                  date < a.AppointmentDate.AddMinutes(a.AppointmentServices.Sum(s => s.Service!.Duration)) &&
                                  a.AppointmentDate < date.AddMinutes(480))
                })
                .ToList();

            // Debug: Uygunluk durumlarını yazdır
            foreach (var employee in availableEmployees)
            {
                Console.WriteLine($"Employee: {employee.name}, Is Available: {employee.isAvailable}");
            }

            // 5. Sonucu JSON olarak döndür
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

        [HttpGet]
        public IActionResult UploadImage()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                ModelState.AddModelError("", "Lütfen bir resim yükleyin.");
                return View("UploadImage");
            }

            using var httpClient = new HttpClient();
            using var formData = new MultipartFormDataContent();

            // Attach image
            using var stream = new MemoryStream();
            await image.CopyToAsync(stream);
            var content = new ByteArrayContent(stream.ToArray());
            content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            formData.Add(content, "image", image.FileName);

            // Send API request
            var response = await httpClient.PostAsync("http://node-js-faceshape.onrender.com/api/ai", formData);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return View("Result", jsonResponse);
            }
            else
            {
                ModelState.AddModelError("", "API isteği başarısız oldu.");
                return View("UploadImage");
            }
        }
    }
}
