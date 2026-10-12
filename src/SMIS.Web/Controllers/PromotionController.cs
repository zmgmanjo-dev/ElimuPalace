using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Students;
using SMIS.Domain.Enums;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

[Authorize(Roles = "Administrator,Principal,ClassTeacher")]
public class PromotionController : BaseController
{
    private new readonly ApplicationDbContext _db;

    public PromotionController(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    // GET: /Promotion
    public async Task<IActionResult> Index(int? classLevelId)
    {
        var classes = await _db.ClassLevels
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();

        ViewBag.Classes = classes;
        ViewBag.SelectedClassId = classLevelId;

        if (!classLevelId.HasValue)
            return View(new List<Student>());

        var currentClass = classes.FirstOrDefault(c => c.Id == classLevelId);
        if (currentClass == null) return NotFound();

        var nextClass = classes
            .Where(c => c.OrderIndex > currentClass.OrderIndex)
            .OrderBy(c => c.OrderIndex)
            .FirstOrDefault();

        var students = await _db.Students
            .Include(s => s.ClassLevel)
            .Include(s => s.ClassStream)
            .Where(s => s.ClassLevelId == classLevelId &&
                        s.Status == StudentStatus.Active)
            .OrderBy(s => s.LastName)
            .ToListAsync();

        var nextClassStreams = nextClass != null
            ? await _db.ClassStreams
                .Where(s => s.ClassLevelId == nextClass.Id)
                .ToListAsync()
            : new List<SMIS.Domain.Entities.School.ClassStream>();

        var currentYear = await _db.AcademicYears
            .FirstOrDefaultAsync(y => y.IsCurrent);

        ViewBag.CurrentClass = currentClass;
        ViewBag.NextClass = nextClass;
        ViewBag.NextClassStreams = nextClassStreams;
        ViewBag.CurrentYear = currentYear;

        return View(students);
    }

    // POST: /Promotion/Process
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(int academicYearId,
        int currentClassLevelId,
        List<int> studentIds,
        List<string> actions,
        List<int?> targetClassLevelIds,
        List<int?> targetClassStreamIds)
    {
        for (int i = 0; i < studentIds.Count; i++)
        {
            var student = await _db.Students.FindAsync(studentIds[i]);
            if (student == null) continue;

            var outcome = actions[i] switch
            {
                "promote" => PromotionOutcome.Promoted,
                "repeat" => PromotionOutcome.Repeated,
                "graduate" => PromotionOutcome.Graduated,
                _ => PromotionOutcome.Promoted
            };

            _db.StudentClassHistories.Add(new StudentClassHistory
            {
                StudentId = student.Id,
                AcademicYearId = academicYearId,
                ClassLevelId = currentClassLevelId,
                ClassStreamId = student.ClassStreamId,
                Outcome = outcome,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow
            });

            if (outcome == PromotionOutcome.Graduated)
            {
                student.Status = StudentStatus.Graduated;
            }
            else if (outcome == PromotionOutcome.Promoted)
            {
                student.ClassLevelId = targetClassLevelIds.Count > i
                    ? targetClassLevelIds[i] : null;
                student.ClassStreamId = targetClassStreamIds.Count > i
                    ? targetClassStreamIds[i] : null;
            }
            else if (outcome == PromotionOutcome.Repeated)
            {
                student.ClassStreamId = targetClassStreamIds.Count > i
                    ? targetClassStreamIds[i] : student.ClassStreamId;
            }

            student.UpdatedBy = User.Identity?.Name ?? "system";
            student.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Students processed successfully!";
        await LogAudit("Promotion", "ProcessPromotion",
    $"Processed {studentIds.Count} students from class #{currentClassLevelId}");
        return RedirectToAction(nameof(Index), new { classLevelId = currentClassLevelId });
    }
}