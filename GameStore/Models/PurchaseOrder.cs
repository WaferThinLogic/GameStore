using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.Models;

public class PurchaseOrder
{
    public int Id { get; set; }

    [Required]
    public string OrderNumber { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public DateTime? ExpectedDeliveryDate { get; set; }

    public DateTime? SubmittedDate { get; set; }

    public DateTime? ConfirmedDate { get; set; }

    public DateTime? ReceivedDate { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, Submitted, Confirmed, Received, Cancelled

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    public Supplier? Supplier { get; set; }

    public ICollection<PurchaseOrderItem>? PurchaseOrderItems { get; set; }
}

public class PurchaseOrderItem
{
    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }

    public int GameId { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; }

    public int? QuantityReceived { get; set; }

    // Navigation properties
    public PurchaseOrder? PurchaseOrder { get; set; }

    public Game? Game { get; set; }
}

public class PriceHistory
{
    public int Id { get; set; }

    public int GameId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SellingPrice { get; set; }

    public DateTime EffectiveDate { get; set; } = DateTime.Now;

    public string? ChangeReason { get; set; }

    // Navigation property
    public Game? Game { get; set; }
}

public class StockAlert
{
    public int Id { get; set; }

    public int GameId { get; set; }

    public int Threshold { get; set; } = 5;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation property
    public Game? Game { get; set; }
}