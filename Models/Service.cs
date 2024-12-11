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

        public ICollection<AppointmentService>? AppointmentServices { get; set; }
    }
}
