namespace PortfolioHub.Models;

public class UserBadge
{
    public int Id { get; set; }

    public int BadgeId { get; set; }
    public Badge Badge { get; set; } = default!;

    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;

    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
