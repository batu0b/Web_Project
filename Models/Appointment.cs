using System.ComponentModel.DataAnnotations;
using WebProject.Models;
namespace WebProject.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Employee selection is required.")]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        [Required(ErrorMessage = "Service selection is required.")]
        public int ServiceId { get; set; }
        public Service Service { get; set; }

        [Required(ErrorMessage = "Date selection is required.")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Time selection is required.")]
        [DataType(DataType.Time)]
        public TimeSpan Time { get; set; }

        [Required(ErrorMessage = "User ID is required.")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
