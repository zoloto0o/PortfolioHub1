using System.ComponentModel.DataAnnotations;

namespace PortfolioHub.Models;

public enum Visibility
{
    Public = 0,
    Unlisted = 1,
    Private = 2
}

public class PortfolioItem
{
    public int Id { get; set; }

    [Required]
    public string OwnerUserId { get; set; } = default!;
    public ApplicationUser OwnerUser { get; set; } = default!;

    [Required, MaxLength(120)]
    public string Title { get; set; } = default!;

    [MaxLength(3000)]
    public string Description { get; set; } = "";

    public Visibility Visibility { get; set; } = Visibility.Public;

    public bool IsPinned { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // важно: всегда не null
    public List<PortfolioMedia> Media { get; set; } = new();
}
