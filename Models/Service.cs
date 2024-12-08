using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
namespace WebProject.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Service name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Duration is required.")]
        [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes.")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Precision(18, 2)]
        [Range(0.0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal Price { get; set; }

        // The new property
        public string SpecializationRequired { get; set; }
    }
}
