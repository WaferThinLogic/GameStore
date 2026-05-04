using System.ComponentModel.DataAnnotations;

namespace GameStore.Models;

public class Supplier
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ContactName { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<Game>? Games { get; set; }

    public ICollection<PurchaseOrder>? PurchaseOrders { get; set; }
}
