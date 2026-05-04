using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GameStore.Models;

public class ApplicationUser : IdentityUser
{
    [StringLength(100)]
    public string? FullName { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? City { get; set; }

    [StringLength(20)]
    public string? State { get; set; }

    [StringLength(10)]
    public string? PostalCode { get; set; }

    [StringLength(50)]
    public string? Country { get; set; }

    // Navigation properties
    public ICollection<Cart>? Carts { get; set; }
    
    public ICollection<Order>? Orders { get; set; }
}
