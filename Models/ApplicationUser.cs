using Microsoft.AspNetCore.Identity;

namespace Odev.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}
