﻿using System.ComponentModel.DataAnnotations;

namespace Odev.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;

        public ICollection<Appointment>? Appointments
        {
            get; set;
        }
    }
}
