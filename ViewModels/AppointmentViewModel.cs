using Odev.Models;
using System.ComponentModel.DataAnnotations;

namespace Odev.ViewModels
{
    public class AppointmentViewModel
    {
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        [Required]
        public int EmployeeId { get; set; }
    }
}
