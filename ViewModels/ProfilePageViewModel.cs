using PortfolioHub.Models;

namespace PortfolioHub.ViewModels;

public class ProfilePageViewModel
{
    public Profile Profile { get; set; } = default!;
    public List<PortfolioItem> PinnedWorks { get; set; } = new();
    public List<PortfolioItem> Works { get; set; } = new();
    public List<UserBadge> Badges { get; set; } = new();

    public int YearsOnService
    {
        get
        {
            var now = DateTime.UtcNow;
            var years = now.Year - Profile.CreatedAt.Year;
            if (Profile.CreatedAt.Date > now.AddYears(-years)) years--;
            return Math.Max(0, years);
        }
    }
}
