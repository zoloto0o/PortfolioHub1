using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioHub.Data;
using PortfolioHub.Models;
using PortfolioHub.ViewModels;

namespace PortfolioHub.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var normalized = model.Username.Trim().ToLower();

        // проверка уникальности username
        bool usernameExists = await _db.Profiles.AnyAsync(p => p.NormalizedUsername == normalized);
        if (usernameExists)
        {
            ModelState.AddModelError("", "Этот username уже занят.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email, // Identity требует UserName, оставим email
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // создаем профиль автоматически
        var profile = new Profile
        {
            UserId = user.Id,
            Username = model.Username.Trim(),
            NormalizedUsername = normalized,
            DisplayName = model.Username.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Profiles.Add(profile);
        await _db.SaveChangesAsync();

        await _signInManager.SignInAsync(user, isPersistent: false);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        ApplicationUser? user = null;

        // если ввели email
        if (model.Login.Contains("@"))
            user = await _userManager.FindByEmailAsync(model.Login);
        else
            user = await _userManager.FindByNameAsync(model.Login);

        // ВАЖНО: так как UserName у нас = Email, FindByNameAsync по username не найдет.
        // Поэтому делаем поиск через Profile
        if (user == null && !model.Login.Contains("@"))
        {
            var normalized = model.Login.Trim().ToLower();
            var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.NormalizedUsername == normalized);

            if (profile != null)
                user = await _userManager.FindByIdAsync(profile.UserId);
        }

        if (user == null)
        {
            ModelState.AddModelError("", "Неверный логин или пароль.");
            return View(model);
        }

        var signInResult = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            ModelState.AddModelError("", "Неверный логин или пароль.");
            return View(model);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Denied()
    {
        return View();
    }
}
