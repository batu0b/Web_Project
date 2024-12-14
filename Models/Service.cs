using System.ComponentModel.DataAnnotations;

namespace Odev.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 99999.99, ErrorMessage = "Fiyat 0.01 ile 99999.99 arasında olmalıdır.")]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 480, ErrorMessage = "Süre 1 ile 480 dakika arasında olmalıdır.")]
        public int Duration { get; set; } // Süre dakikalar cinsinden

        public ICollection<AppointmentService>? AppointmentServices { get; set; }

        public ICollection<EmployeeService>? EmployeeServices { get; set; }
    }
}
