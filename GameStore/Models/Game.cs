using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.Models;

public class Game
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Required]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPrice { get; set; }

    public int MinStockThreshold { get; set; } = 5;

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public int CategoryId { get; set; }

    public DateTime ReleaseDate { get; set; }

    public int Stock { get; set; }

    public int? SupplierId { get; set; }

    // Navigation properties
    public Category? Category { get; set; }

    public Supplier? Supplier { get; set; }

    public ICollection<CartItem>? CartItems { get; set; }
    
    public ICollection<OrderDetail>? OrderDetails { get; set; }

    public ICollection<PurchaseOrderItem>? PurchaseOrderItems { get; set; }

    public ICollection<PriceHistory>? PriceHistories { get; set; }

    public ICollection<StockAlert>? StockAlerts { get; set; }
}
