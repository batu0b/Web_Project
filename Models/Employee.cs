using System.ComponentModel.DataAnnotations;
using WebProject.Models;

namespace WebProject.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Specialization areas are required.")]
        [StringLength(200, ErrorMessage = "Specialization areas cannot exceed 200 characters.")]
        public string SpecializationAreas { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Range(1000000000, 999999999999, ErrorMessage = "Phone number must be a valid 10-12 digit number.")]
        public Int64 PhoneNumber { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
        public string Address { get; set; }
    }
}
