using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioHub.Data;
using PortfolioHub.Models;
using PortfolioHub.ViewModels;

namespace PortfolioHub.Controllers;

[Authorize]
public class WorksController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public WorksController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _env = env;
    }

    // /works
    [HttpGet("/works")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Challenge();

        var works = await _db.PortfolioItems
            .Include(w => w.Media).ThenInclude(m => m.MediaFile)
            .Where(w => w.OwnerUserId == user.Id)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        return View(works);
    }

    // /works/create
    [HttpGet("/works/create")]
    public IActionResult Create()
    {
        return View(new WorkCreateViewModel());
    }

    [HttpPost("/works/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (_env.WebRootPath == null)
        {
            ModelState.AddModelError("", "Ошибка сервера: папка wwwroot не найдена.");
            return View(model);
        }

        if (model.Images != null && model.Images.Count > 5)
        {
            ModelState.AddModelError("", "Можно загрузить максимум 5 изображений.");
            return View(model);
        }

        var work = new PortfolioItem
        {
            OwnerUserId = user.Id,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim() ?? "",
            Visibility = model.Visibility,
            IsPinned = model.IsPinned,
            CreatedAt = DateTime.UtcNow
        };

        _db.PortfolioItems.Add(work);
        await _db.SaveChangesAsync();

        if (model.Images != null && model.Images.Any())
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "works");
            Directory.CreateDirectory(uploadsDir);

            int order = 0;

            foreach (var file in model.Images)
            {
                if (file == null || file.Length == 0)
                    continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("", $"Формат {ext} не поддерживается.");
                    continue;
                }

                // максимум 10MB
                if (file.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("", $"Файл {file.FileName} слишком большой (макс 10MB).");
                    continue;
                }

                var safeName = $"{Guid.NewGuid():N}{ext}";
                var absPath = Path.Combine(uploadsDir, safeName);

                await using (var stream = new FileStream(absPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relPath = $"uploads/works/{safeName}";

                var mediaFile = new MediaFile
                {
                    OwnerUserId = user.Id,
                    StoredPath = relPath,
                    OriginalFileName = file.FileName,
                    ContentType = file.ContentType,
                    SizeBytes = file.Length,
                    CreatedAt = DateTime.UtcNow
                };

                _db.MediaFiles.Add(mediaFile);
                await _db.SaveChangesAsync();

                _db.PortfolioMedia.Add(new PortfolioMedia
                {
                    PortfolioItemId = work.Id,
                    MediaFileId = mediaFile.Id,
                    SortOrder = order++
                });

                await _db.SaveChangesAsync();
            }
        }

        if (!ModelState.IsValid)
            return View(model);

        return RedirectToAction(nameof(Index));
    }


    // /works/edit/{id}
    [HttpGet("/works/edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Challenge();

        var work = await _db.PortfolioItems.FirstOrDefaultAsync(w => w.Id == id && w.OwnerUserId == user.Id);
        if (work == null) return NotFound();

        var vm = new WorkEditViewModel
        {
            Id = work.Id,
            Title = work.Title,
            Description = work.Description,
            Visibility = work.Visibility,
            IsPinned = work.IsPinned
        };

        return View(vm);
    }

    [HttpPost("/works/edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WorkEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Challenge();

        var work = await _db.PortfolioItems.FirstOrDefaultAsync(w => w.Id == id && w.OwnerUserId == user.Id);
        if (work == null) return NotFound();

        work.Title = model.Title.Trim();
        work.Description = model.Description?.Trim() ?? "";
        work.Visibility = model.Visibility;
        work.IsPinned = model.IsPinned;
        work.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // /works/delete/{id}
    [HttpPost("/works/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Challenge();

        if (_env.WebRootPath == null)
            return RedirectToAction(nameof(Index));

        var work = await _db.PortfolioItems
            .Include(w => w.Media).ThenInclude(m => m.MediaFile)
            .FirstOrDefaultAsync(w => w.Id == id && w.OwnerUserId == user.Id);

        if (work == null) return NotFound();

        foreach (var pm in work.Media)
        {
            var path = pm.MediaFile.StoredPath.Replace("/", Path.DirectorySeparatorChar.ToString());
            var abs = Path.Combine(_env.WebRootPath, path);

            if (System.IO.File.Exists(abs))
                System.IO.File.Delete(abs);

            _db.MediaFiles.Remove(pm.MediaFile);
        }

        _db.PortfolioMedia.RemoveRange(work.Media);
        _db.PortfolioItems.Remove(work);

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
