using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.Models;

public class Cart
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    // Navigation properties
    public ApplicationUser? ApplicationUser { get; set; }

    public ICollection<CartItem>? CartItems { get; set; }
}

public class CartItem
{
    public int Id { get; set; }

    public int CartId { get; set; }

    public int GameId { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    // Navigation properties
    public Cart? Cart { get; set; }

    public Game? Game { get; set; }
}