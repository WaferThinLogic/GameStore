using System.ComponentModel.DataAnnotations;

namespace GameStore.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    // Navigation property
    public ICollection<Game>? Games { get; set; }
}