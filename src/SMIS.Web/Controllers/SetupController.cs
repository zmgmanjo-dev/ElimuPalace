using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMIS.Infrastructure.Data;
using SMIS.Domain.Entities.School;
using SMIS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace SMIS.Web.Controllers;

/// <summary>
/// Handles all system setup pages - only for Administrators
/// </summary>
[Authorize(Roles = "Administrator")]
public class SetupController : Controller
{
    private readonly ApplicationDbContext _db;

    public SetupController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Setup/SchoolInfo
    public async Task<IActionResult> SchoolInfo()
    {
        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        return View(school);
    }

    // POST: /Setup/SchoolInfo
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SchoolInfo(SchoolProfile model, IFormFile? logoFile)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Handle logo upload
        if (logoFile != null && logoFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"school-logo{Path.GetExtension(logoFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await logoFile.CopyToAsync(stream);

            model.LogoPath = $"/uploads/logos/{fileName}";
        }

        var existing = await _db.SchoolProfiles.FirstOrDefaultAsync();

        if (existing == null)
        {
            // First time setup - create new record
            model.CreatedBy = User.Identity?.Name ?? "system";
            model.CreatedAt = DateTime.UtcNow;
            _db.SchoolProfiles.Add(model);
        }
        else
        {
            // Update existing record
            existing.Name = model.Name;
            existing.KemisCode = model.KemisCode;
            existing.Motto = model.Motto;
            existing.Level = model.Level;
            existing.Category = model.Category;
            existing.Type = model.Type;
            existing.County = model.County;
            existing.SubCounty = model.SubCounty;
            existing.Ward = model.Ward;
            existing.PhysicalAddress = model.PhysicalAddress;
            existing.PostalAddress = model.PostalAddress;
            existing.PhoneNumber = model.PhoneNumber;
            existing.AlternativePhone = model.AlternativePhone;
            existing.Email = model.Email;
            existing.Website = model.Website;
            existing.RegistrationNumber = model.RegistrationNumber;
            existing.Principal = model.Principal;
            existing.BoardChairperson = model.BoardChairperson;
            existing.EstablishedDate = model.EstablishedDate;
            existing.PrimaryColor = model.PrimaryColor;
            existing.SecondaryColor = model.SecondaryColor;
            if (model.LogoPath != null)
                existing.LogoPath = model.LogoPath;
            existing.UpdatedBy = User.Identity?.Name ?? "system";
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "School information saved successfully!";
        return RedirectToAction(nameof(SchoolInfo));
    }
// ── ACADEMIC YEARS ───────────────────────────────────────────

    // GET: /Setup/AcademicYears
    public async Task<IActionResult> AcademicYears()
    {
        var years = await _db.AcademicYears
            .Include(y => y.Terms)
            .OrderByDescending(y => y.Year)
            .ToListAsync();
        return View(years);
    }

    // POST: /Setup/CreateAcademicYear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAcademicYear(string name, int year,
        DateTime startDate, DateTime endDate, bool isCurrent)
    {
        // If setting as current, unset all others first
        if (isCurrent)
        {
            var allYears = await _db.AcademicYears.ToListAsync();
            foreach (var y in allYears)
                y.IsCurrent = false;
        }

        var academicYear = new AcademicYear
        {
            Name = name,
            Year = year,
            StartDate = startDate,
            EndDate = endDate,
            IsCurrent = isCurrent,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        _db.AcademicYears.Add(academicYear);
        await _db.SaveChangesAsync();

        // Auto-create 3 terms for this year
        var terms = new List<AcademicTerm>
        {
            new AcademicTerm {
                AcademicYearId = academicYear.Id,
                TermNumber = Domain.Enums.TermNumber.Term1,
                Name = "Term 1",
                StartDate = startDate,
                EndDate = startDate.AddMonths(3),
                IsCurrent = isCurrent,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow
            },
            new AcademicTerm {
                AcademicYearId = academicYear.Id,
                TermNumber = Domain.Enums.TermNumber.Term2,
                Name = "Term 2",
                StartDate = startDate.AddMonths(4),
                EndDate = startDate.AddMonths(7),
                IsCurrent = false,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow
            },
            new AcademicTerm {
                AcademicYearId = academicYear.Id,
                TermNumber = Domain.Enums.TermNumber.Term3,
                Name = "Term 3",
                StartDate = startDate.AddMonths(8),
                EndDate = endDate,
                IsCurrent = false,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow
            }
        };

        _db.AcademicTerms.AddRange(terms);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Academic Year {name} created with 3 terms!";
        return RedirectToAction(nameof(AcademicYears));
    }

    // POST: /Setup/SetCurrentYear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCurrentYear(int id)
    {
        var allYears = await _db.AcademicYears.ToListAsync();
        foreach (var y in allYears)
            y.IsCurrent = (y.Id == id);

        await _db.SaveChangesAsync();
        TempData["Success"] = "Current academic year updated!";
        return RedirectToAction(nameof(AcademicYears));
    }
// POST: /Setup/UpdateTerm
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTerm(int id, DateTime startDate,
        DateTime endDate, bool isCurrent)
    {
        var term = await _db.AcademicTerms.FindAsync(id);
        if (term == null)
        {
            TempData["Error"] = "Term not found.";
            return RedirectToAction(nameof(AcademicYears));
        }

        // If setting this term as current, unset all others in same year
        if (isCurrent)
        {
            var siblings = await _db.AcademicTerms
                .Where(t => t.AcademicYearId == term.AcademicYearId)
                .ToListAsync();
            foreach (var t in siblings)
                t.IsCurrent = false;
        }

        term.StartDate = startDate;
        term.EndDate = endDate;
        term.IsCurrent = isCurrent;
        term.UpdatedBy = User.Identity?.Name ?? "system";
        term.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"{term.Name} updated successfully!";
        return RedirectToAction(nameof(AcademicYears));
    }
// ── CLASSES & STREAMS ────────────────────────────────────────

    // GET: /Setup/Classes
    public async Task<IActionResult> Classes()
    {
        var classes = await _db.ClassLevels
            .Include(c => c.Streams)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();
        return View(classes);
    }

    // POST: /Setup/CreateClass
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClass(string name, string shortName,
        SMIS.Domain.Enums.SchoolLevel level, int orderIndex)
    {
        var classLevel = new ClassLevel
        {
            Name = name,
            ShortName = shortName,
            Level = level,
            OrderIndex = orderIndex,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        _db.ClassLevels.Add(classLevel);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"{name} created successfully!";
        return RedirectToAction(nameof(Classes));
    }

    // POST: /Setup/CreateStream
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStream(int classLevelId, string name)
    {
        var classLevel = await _db.ClassLevels.FindAsync(classLevelId);
        if (classLevel == null)
        {
            TempData["Error"] = "Class not found.";
            return RedirectToAction(nameof(Classes));
        }

        var fullName = $"{classLevel.Name} {name}";

        var stream = new ClassStream
        {
            ClassLevelId = classLevelId,
            Name = name,
            FullName = fullName,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        _db.ClassStreams.Add(stream);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Stream {fullName} created successfully!";
        return RedirectToAction(nameof(Classes));
    }

    // POST: /Setup/DeleteStream
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStream(int id)
    {
        var stream = await _db.ClassStreams.FindAsync(id);
        if (stream != null)
        {
            _db.ClassStreams.Remove(stream);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Stream deleted successfully!";
        }
        return RedirectToAction(nameof(Classes));
    }

    // POST: /Setup/SeedDefaultClasses
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedDefaultClasses()
    {
        // Only seed if no classes exist
        if (await _db.ClassLevels.AnyAsync())
        {
            TempData["Error"] = "Classes already exist!";
            return RedirectToAction(nameof(Classes));
        }

        var defaultClasses = new List<ClassLevel>
        {
            new ClassLevel { Name = "PP1", ShortName = "PP1",
                Level = Domain.Enums.SchoolLevel.PrePrimary, OrderIndex = 1,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new ClassLevel { Name = "PP2", ShortName = "PP2",
                Level = Domain.Enums.SchoolLevel.PrePrimary, OrderIndex = 2,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new ClassLevel { Name = "Grade 1", ShortName = "G1",
                Level = Domain.Enums.SchoolLevel.LowerPrimary, OrderIndex = 3,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new ClassLevel { Name = "Grade 2", ShortName = "G2",
                Level = Domain.Enums.SchoolLevel.LowerPrimary, OrderIndex = 4,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new ClassLevel { Name = "Grade 3", ShortName = "G3",
                Level = Domain.Enums.SchoolLevel.LowerPrimary, OrderIndex = 5,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new ClassLevel { Name = "Grade 4", ShortName = "G4",
                Level = Domain.Enums.SchoolLevel.UpperPrimary, OrderIndex = 6,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new ClassLevel { Name = "Grade 5", ShortName = "G5",
                Level = Domain.Enums.SchoolLevel.UpperPrimary, OrderIndex = 7,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new ClassLevel { Name = "Grade 6", ShortName = "G6",
                Level = Domain.Enums.SchoolLevel.UpperPrimary, OrderIndex = 8,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new ClassLevel { Name = "Grade 7", ShortName = "G7",
                Level = Domain.Enums.SchoolLevel.JuniorSchool, OrderIndex = 9,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new ClassLevel { Name = "Grade 8", ShortName = "G8",
                Level = Domain.Enums.SchoolLevel.JuniorSchool, OrderIndex = 10,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new ClassLevel { Name = "Grade 9", ShortName = "G9",
                Level = Domain.Enums.SchoolLevel.JuniorSchool, OrderIndex = 11,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
        };

        _db.ClassLevels.AddRange(defaultClasses);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Default CBC classes created successfully!";
        return RedirectToAction(nameof(Classes));
    }
// ── SUBJECTS ─────────────────────────────────────────────────

    // GET: /Setup/Subjects
    public async Task<IActionResult> Subjects()
    {
        var subjects = await _db.Subjects
            .OrderBy(s => s.Level)
            .ThenBy(s => s.OrderIndex)
            .ToListAsync();
        return View(subjects);
    }

    // POST: /Setup/CreateSubject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubject(string name, string code,
        string? description, SchoolLevel level, bool isCompulsory)
    {
        var subject = new Subject
        {
            Name = name,
            Code = code,
            Description = description,
            Level = level,
            IsCompulsory = isCompulsory,
            IsCBCLearningArea = true,
            OrderIndex = await _db.Subjects.CountAsync() + 1,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Subject '{name}' created successfully!";
        return RedirectToAction(nameof(Subjects));
    }

    // POST: /Setup/SeedDefaultSubjects
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedDefaultSubjects()
    {
        if (await _db.Subjects.AnyAsync())
        {
            TempData["Error"] = "Subjects already exist!";
            return RedirectToAction(nameof(Subjects));
        }

        var subjects = new List<Subject>
        {
            // Pre-Primary
            new Subject { Name = "Language Activities", Code = "LANG",
                Level = SchoolLevel.PrePrimary, IsCompulsory = true,
                OrderIndex = 1, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Mathematical Activities", Code = "MATH-PP",
                Level = SchoolLevel.PrePrimary, IsCompulsory = true,
                OrderIndex = 2, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Environmental Activities", Code = "ENV-PP",
                Level = SchoolLevel.PrePrimary, IsCompulsory = true,
                OrderIndex = 3, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Psychomotor & Creative Activities", Code = "PSY",
                Level = SchoolLevel.PrePrimary, IsCompulsory = true,
                OrderIndex = 4, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },

            // Lower Primary (Grade 1-3)
            new Subject { Name = "Literacy", Code = "LIT",
                Level = SchoolLevel.LowerPrimary, IsCompulsory = true,
                OrderIndex = 1, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Kiswahili Language Activities", Code = "KSW-LP",
                Level = SchoolLevel.LowerPrimary, IsCompulsory = true,
                OrderIndex = 2, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Mathematical Activities", Code = "MATH-LP",
                Level = SchoolLevel.LowerPrimary, IsCompulsory = true,
                OrderIndex = 3, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Environmental Activities", Code = "ENV-LP",
                Level = SchoolLevel.LowerPrimary, IsCompulsory = true,
                OrderIndex = 4, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Creative Arts", Code = "CRE-LP",
                Level = SchoolLevel.LowerPrimary, IsCompulsory = true,
                OrderIndex = 5, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Religious Education", Code = "RE-LP",
                Level = SchoolLevel.LowerPrimary, IsCompulsory = true,
                OrderIndex = 6, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },

            // Upper Primary (Grade 4-6)
            new Subject { Name = "English", Code = "ENG",
                Level = SchoolLevel.UpperPrimary, IsCompulsory = true,
                OrderIndex = 1, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Kiswahili", Code = "KSW",
                Level = SchoolLevel.UpperPrimary, IsCompulsory = true,
                OrderIndex = 2, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Mathematics", Code = "MATH",
                Level = SchoolLevel.UpperPrimary, IsCompulsory = true,
                OrderIndex = 3, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Science & Technology", Code = "SCI",
                Level = SchoolLevel.UpperPrimary, IsCompulsory = true,
                OrderIndex = 4, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Social Studies", Code = "SST",
                Level = SchoolLevel.UpperPrimary, IsCompulsory = true,
                OrderIndex = 5, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Creative Arts & Sports", Code = "CAS",
                Level = SchoolLevel.UpperPrimary, IsCompulsory = true,
                OrderIndex = 6, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Religious Education", Code = "RE",
                Level = SchoolLevel.UpperPrimary, IsCompulsory = true,
                OrderIndex = 7, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },

            // Junior School (Grade 7-9)
            new Subject { Name = "English", Code = "ENG-JS",
                Level = SchoolLevel.JuniorSchool, IsCompulsory = true,
                OrderIndex = 1, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Kiswahili", Code = "KSW-JS",
                Level = SchoolLevel.JuniorSchool, IsCompulsory = true,
                OrderIndex = 2, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Mathematics", Code = "MATH-JS",
                Level = SchoolLevel.JuniorSchool, IsCompulsory = true,
                OrderIndex = 3, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Integrated Science", Code = "ISCI",
                Level = SchoolLevel.JuniorSchool, IsCompulsory = true,
                OrderIndex = 4, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Social Studies", Code = "SST-JS",
                Level = SchoolLevel.JuniorSchool, IsCompulsory = true,
                OrderIndex = 5, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Business Studies", Code = "BUS",
                Level = SchoolLevel.JuniorSchool, IsCompulsory = true,
                OrderIndex = 6, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Agriculture & Nutrition", Code = "AGR",
                Level = SchoolLevel.JuniorSchool, IsCompulsory = true,
                OrderIndex = 7, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Creative Arts & Sports", Code = "CAS-JS",
                Level = SchoolLevel.JuniorSchool, IsCompulsory = true,
                OrderIndex = 8, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Subject { Name = "Religious Education", Code = "RE-JS",
                Level = SchoolLevel.JuniorSchool, IsCompulsory = true,
                OrderIndex = 9, IsCBCLearningArea = true,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
        };

        _db.Subjects.AddRange(subjects);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Default CBC subjects created successfully!";
        return RedirectToAction(nameof(Subjects));
    }

    // POST: /Setup/ToggleSubject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSubject(int id)
    {
        var subject = await _db.Subjects.FindAsync(id);
        if (subject != null)
        {
            subject.IsActive = !subject.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{subject.Name} has been " +
                (subject.IsActive ? "activated" : "deactivated") + "!";
        }
        return RedirectToAction(nameof(Subjects));
    }
// POST: /Setup/ToggleCompulsory
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCompulsory(int id)
    {
        var subject = await _db.Subjects.FindAsync(id);
        if (subject != null)
        {
            subject.IsCompulsory = !subject.IsCompulsory;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{subject.Name} is now " +
                (subject.IsCompulsory ? "Compulsory" : "Optional") + "!";
        }
        return RedirectToAction(nameof(Subjects));
    }
}