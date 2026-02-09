using System.ComponentModel.DataAnnotations;

namespace PortfolioHub.Models;

public class Profile
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;

    [MaxLength(80)]
    public string DisplayName { get; set; } = "";

    [MaxLength(50)]
    public string Username { get; set; } = "";

    [MaxLength(50)]
    public string NormalizedUsername { get; set; } = "";

    [MaxLength(60)]
    public string Country { get; set; } = "";

    [MaxLength(60)]
    public string City { get; set; } = "";

    [MaxLength(800)]
    public string About { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? AvatarFileId { get; set; }
    public MediaFile? AvatarFile { get; set; }

    public int? CoverFileId { get; set; }
    public MediaFile? CoverFile { get; set; }

    [MaxLength(40)]
    public string ThemeKey { get; set; } = "default";
}
