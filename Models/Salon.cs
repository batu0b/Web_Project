using System.ComponentModel.DataAnnotations;

namespace Odev.Models
{
    public class Salon
    {
        public int Id { get; set; } 

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; 

        [Required]
        public TimeSpan StartTime { get; set; } 

        [Required]
        public TimeSpan EndTime { get; set; } 
    }
}
