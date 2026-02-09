using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioHub.Data;
using PortfolioHub.Models;
using PortfolioHub.ViewModels;

namespace PortfolioHub.Controllers;

public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // /me -> редирект на /u/{username} текущего пользователя
    [Authorize]
    [HttpGet("/me")]
    public async Task<IActionResult> MyProfile()
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        if (currentUser == null) return Challenge();

        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
        if (profile == null) return NotFound();

        return Redirect($"/u/{profile.Username}");
    }

    // /u/{username}
    [HttpGet("/u/{username}")]
    public async Task<IActionResult> PublicProfile(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return NotFound();

        var normalized = username.Trim().ToLower();

        var profile = await _db.Profiles
            .Include(p => p.AvatarFile)
            .Include(p => p.CoverFile)
            .FirstOrDefaultAsync(p => p.NormalizedUsername == normalized);

        if (profile == null)
            return NotFound();

        var userBadges = await _db.UserBadges
            .Include(ub => ub.Badge)
            .Where(ub => ub.UserId == profile.UserId)
            .OrderByDescending(ub => ub.AwardedAt)
            .ToListAsync();

        var pinned = await _db.PortfolioItems
            .Include(w => w.Media)
                .ThenInclude(pm => pm.MediaFile)
            .Where(w =>
                w.OwnerUserId == profile.UserId &&
                w.Visibility == PortfolioHub.Models.Visibility.Public &&
                w.IsPinned)
            .OrderByDescending(w => w.CreatedAt)
            .Take(3)
            .ToListAsync();

        var works = await _db.PortfolioItems
            .Include(w => w.Media)
                .ThenInclude(pm => pm.MediaFile)
            .Where(w =>
                w.OwnerUserId == profile.UserId &&
                w.Visibility == PortfolioHub.Models.Visibility.Public)
            .OrderByDescending(w => w.CreatedAt)
            .Take(20)
            .ToListAsync();

        var vm = new ProfilePageViewModel
        {
            Profile = profile,
            Badges = userBadges,
            PinnedWorks = pinned,
            Works = works
        };

        // Вьюха та же: Views/Profile/User.cshtml
        return View("User", vm);
    }
}
