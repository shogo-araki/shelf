using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace shelf_project.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        public string? CompanyName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        public virtual ICollection<Distributor>? Distributors { get; set; }

        public virtual ICollection<Manufacturer>? Manufacturers { get; set; }
    }

    public enum UserRole
    {
        Consumer = 0,
        Distributor = 1,
        Manufacturer = 2,
        Admin = 3
    }
}