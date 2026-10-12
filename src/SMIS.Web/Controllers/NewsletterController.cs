using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Communication;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

[Authorize(Roles = "Administrator,Principal")]
public class NewsletterController : Controller
{
    private readonly ApplicationDbContext _db;

    public NewsletterController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Newsletter
    public async Task<IActionResult> Index()
    {
        var newsletters = await _db.Newsletters
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return View(newsletters);
    }

    // GET: /Newsletter/Create
    public IActionResult Create() => View();

    // POST: /Newsletter/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, string content, bool publishNow)
    {
        var newsletter = new Newsletter
        {
            Title = title,
            Content = content,
            IsPublished = publishNow,
            PublishedAt = publishNow ? DateTime.UtcNow : null,
            PublishedBy = publishNow ? (User.Identity?.Name ?? "Admin") : null,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };
        _db.Newsletters.Add(newsletter);
        await _db.SaveChangesAsync();

        TempData["Success"] = publishNow
            ? "Newsletter published!"
            : "Newsletter saved as draft.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Newsletter/Publish/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        var newsletter = await _db.Newsletters.FindAsync(id);
        if (newsletter == null) return NotFound();

        newsletter.IsPublished = true;
        newsletter.PublishedAt = DateTime.UtcNow;
        newsletter.PublishedBy = User.Identity?.Name ?? "Admin";
        await _db.SaveChangesAsync();

        TempData["Success"] = "Newsletter published!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Newsletter/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var newsletter = await _db.Newsletters.FindAsync(id);
        if (newsletter == null) return NotFound();

        _db.Newsletters.Remove(newsletter);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Newsletter deleted.";
        return RedirectToAction(nameof(Index));
    }
}