using System.ComponentModel.DataAnnotations;

namespace PortfolioHub.Models;

public class MediaFile
{
    public int Id { get; set; }

    [Required]
    public string OwnerUserId { get; set; } = default!;
    public ApplicationUser OwnerUser { get; set; } = default!;

    [Required, MaxLength(260)]
    public string StoredPath { get; set; } = default!;

    [MaxLength(120)]
    public string OriginalFileName { get; set; } = "";

    [MaxLength(80)]
    public string ContentType { get; set; } = "";

    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
