using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Odev.Models;
using System.Reflection.Emit;

namespace Odev.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
           : base(options)
        {
        }

        // Veritabanı tabloları
        public DbSet<Service> Services { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentService> AppointmentServices { get; set; }
        public DbSet<EmployeeService> EmployeeServices { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // AppointmentService için composite key tanımlama
            builder.Entity<AppointmentService>()
                .HasKey(t => new { t.AppointmentId, t.ServiceId });

            // Appointment ve AppointmentService ilişkisi
            builder.Entity<AppointmentService>()
                .HasOne(pt => pt.Appointment)
                .WithMany(p => p.AppointmentServices)
                .HasForeignKey(pt => pt.AppointmentId);

            // Service ve AppointmentService ilişkisi
            builder.Entity<AppointmentService>()
                .HasOne(pt => pt.Service)
                .WithMany(t => t.AppointmentServices)
                .HasForeignKey(pt => pt.ServiceId);

            builder.Entity<EmployeeService>()
                .HasKey(es => new { es.EmployeeId, es.ServiceId });

            builder.Entity<EmployeeService>()
                .HasOne(es => es.Employee)
                .WithMany(e => e.EmployeeServices)
                .HasForeignKey(es => es.EmployeeId);

            builder.Entity<EmployeeService>()
                .HasOne(es => es.Service)
                .WithMany(s => s.EmployeeServices)
                .HasForeignKey(es => es.ServiceId);
        }
    
}
}
