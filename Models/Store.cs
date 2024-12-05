using System.ComponentModel.DataAnnotations;
using WebProject.Models;

namespace WebProject.DbRelated
{
    public class Store
    {
        public int StoreId { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "Store name cannot exceed 200 characters.")]
        public string Name { get; set; }

        [Required]
        [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
        public string Address { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Working hours format is invalid.")]
        public string WorkingHours { get; set; } // e.g., "09:00-18:00"

        // Navigation Properties
        public ICollection<Employee> Employees { get; set; }
        public ICollection<Service> Services { get; set; }
    }
}
