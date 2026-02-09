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
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<WorksController> _logger;

    public WorksController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env, ILogger<WorksController> logger)
    {
        _db = db;
        _userManager = userManager;
        _env = env;
        _logger = logger;
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

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "works");
        Directory.CreateDirectory(uploadsDir);

        var writtenFiles = new List<string>();

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
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

            if (model.Images != null && model.Images.Any())
            {
                int order = 0;

                foreach (var file in model.Images)
                {
                    if (file == null || file.Length == 0)
                        continue;

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                    if (!AllowedImageExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("", $"Формат {ext} не поддерживается.");
                        continue;
                    }

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

                    writtenFiles.Add(absPath);

                    var mediaFile = new MediaFile
                    {
                        OwnerUserId = user.Id,
                        StoredPath = $"uploads/works/{safeName}",
                        OriginalFileName = Clamp(file.FileName, 120),
                        ContentType = ResolveContentType(file.ContentType, ext),
                        SizeBytes = file.Length,
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.MediaFiles.Add(mediaFile);
                    _db.PortfolioMedia.Add(new PortfolioMedia
                    {
                        PortfolioItem = work,
                        MediaFile = mediaFile,
                        SortOrder = order++
                    });
                }
            }

            if (!ModelState.IsValid)
            {
                await tx.RollbackAsync();
                CleanupFiles(writtenFiles);
                return View(model);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            CleanupFiles(writtenFiles);
            _logger.LogError(ex, "Ошибка при создании работы с изображениями для пользователя {UserId}", user.Id);
            ModelState.AddModelError("", "Не удалось сохранить работу с изображениями. Проверьте файлы и попробуйте снова.");
            return View(model);
        }
    }


    private void CleanupFiles(IEnumerable<string> absolutePaths)
    {
        foreach (var path in absolutePaths)
        {
            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить временный файл {Path}", path);
            }
        }
    }

    private static string Clamp(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }

    private static string ResolveContentType(string? incomingContentType, string extension)
    {
        if (!string.IsNullOrWhiteSpace(incomingContentType))
            return Clamp(incomingContentType, 80);

        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
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
