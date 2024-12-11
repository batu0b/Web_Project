using System.ComponentModel.DataAnnotations;

namespace Odev.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        [Required]
        [DataType(DataType.DateTime)] // Doğru DataType kullanımı
        public DateTime? ApprovalDate { get; set; }

        public ICollection<AppointmentService>? AppointmentServices { get; set; }

        public bool IsApproved { get; set; } = false; // Varsayılan olarak onaysız.
    }
}
