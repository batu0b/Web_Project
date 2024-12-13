using System.ComponentModel.DataAnnotations;

namespace Odev.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 480, ErrorMessage = "Süre 1 ile 480 dakika arasında olmalıdır.")]
        public int Duration { get; set; } // Süre dakikalar cinsinden

        public ICollection<AppointmentService>? AppointmentServices { get; set; }

        public ICollection<EmployeeService>? EmployeeServices { get; set; }
    }
}
