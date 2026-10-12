using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Students;
using SMIS.Domain.Enums;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

/// <summary>
/// Handles all student management pages
/// </summary>
[Authorize(Roles = "Administrator,Principal,Teacher")]
public class StudentsController : BaseController
{
    private new readonly ApplicationDbContext _db;

    public StudentsController(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    // GET: /Students
public async Task<IActionResult> Index(string? search, int? classId,
    int? streamId, Gender? gender, StudentStatus? status)
{
    var query = _db.Students
        .Include(s => s.ClassLevel)
        .Include(s => s.ClassStream)
        .Where(s => !s.IsDeleted);

    if (!string.IsNullOrEmpty(search))
        query = query.Where(s =>
            s.FirstName.Contains(search) ||
            s.LastName.Contains(search) ||
            s.AdmissionNumber.Contains(search));

    if (classId.HasValue)
        query = query.Where(s => s.ClassLevelId == classId);

    if (streamId.HasValue)
        query = query.Where(s => s.ClassStreamId == streamId);

    if (gender.HasValue)
        query = query.Where(s => s.Gender == gender);

    if (status.HasValue)
        query = query.Where(s => s.Status == status);

    var students = await query
        .OrderBy(s => s.ClassLevel!.OrderIndex)
        .ThenBy(s => s.LastName)
        .ToListAsync();

    ViewBag.Classes = await _db.ClassLevels
        .OrderBy(c => c.OrderIndex).ToListAsync();
    ViewBag.Streams = await _db.ClassStreams
        .Include(s => s.ClassLevel)
        .OrderBy(s => s.ClassLevel.OrderIndex)
        .ThenBy(s => s.Name)
        .ToListAsync();

    ViewBag.Search = search;
    ViewBag.ClassId = classId;
    ViewBag.StreamId = streamId;
    ViewBag.Gender = gender;
    ViewBag.Status = status;
    ViewBag.TotalStudents = students.Count;

    return View(students);
}

    // GET: /Students/Create
    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View();
    }

    // POST: /Students/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Student model, IFormFile? photoFile)
    {
        // Generate admission number if not provided
        if (string.IsNullOrEmpty(model.AdmissionNumber))
            model.AdmissionNumber = await GenerateAdmissionNumber();

        // Handle photo upload
        if (photoFile != null && photoFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", "uploads", "students");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = $"{model.AdmissionNumber}{Path.GetExtension(photoFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await photoFile.CopyToAsync(stream);
            model.PhotoPath = $"/uploads/students/{fileName}";
        }

        model.CreatedBy = User.Identity?.Name ?? "system";
        model.CreatedAt = DateTime.UtcNow;

        _db.Students.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Student {model.FullName} admitted successfully! " +
            $"Admission No: {model.AdmissionNumber}";
            await LogAudit("Students", "AdmitStudent",
    $"Admitted {model.FullName} — Admission No: {model.AdmissionNumber}");
        return RedirectToAction(nameof(Index));
    }

    // GET: /Students/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var student = await _db.Students
            .Include(s => s.ClassLevel)
            .Include(s => s.ClassStream)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null)
            return NotFound();

        return View(student);
    }

    // GET: /Students/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var student = await _db.Students.FindAsync(id);
        if (student == null) return NotFound();
        await LoadDropdowns();
        return View(student);
    }

    // POST: /Students/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Student model, IFormFile? photoFile)
    {
        var student = await _db.Students.FindAsync(id);
        if (student == null) return NotFound();

        // Handle photo upload
        if (photoFile != null && photoFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", "uploads", "students");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = $"{student.AdmissionNumber}{Path.GetExtension(photoFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await photoFile.CopyToAsync(stream);
            student.PhotoPath = $"/uploads/students/{fileName}";
        }

        // Update fields
        student.FirstName = model.FirstName;
        student.MiddleName = model.MiddleName;
        student.LastName = model.LastName;
        student.DateOfBirth = model.DateOfBirth;
        student.Gender = model.Gender;
        student.AdmissionDate = model.AdmissionDate;
        student.ClassLevelId = model.ClassLevelId;
        student.ClassStreamId = model.ClassStreamId;
        student.PhoneNumber = model.PhoneNumber;
        student.Email = model.Email;
        student.County = model.County;
        student.SubCounty = model.SubCounty;
        student.Nationality = model.Nationality;
        student.Religion = model.Religion;
        student.BirthCertificateNumber = model.BirthCertificateNumber;
        student.FatherName = model.FatherName;
        student.FatherPhone = model.FatherPhone;
        student.FatherEmail = model.FatherEmail;
        student.FatherOccupation = model.FatherOccupation;
        student.MotherName = model.MotherName;
        student.MotherPhone = model.MotherPhone;
        student.MotherEmail = model.MotherEmail;
        student.MotherOccupation = model.MotherOccupation;
        student.GuardianName = model.GuardianName;
        student.GuardianPhone = model.GuardianPhone;
        student.GuardianRelationship = model.GuardianRelationship;
        student.BloodGroup = model.BloodGroup;
        student.MedicalConditions = model.MedicalConditions;
        student.Allergies = model.Allergies;
        student.HasSpecialNeeds = model.HasSpecialNeeds;
        student.SpecialNeedsDescription = model.SpecialNeedsDescription;
        student.PreviousSchool = model.PreviousSchool;
        student.PreviousClass = model.PreviousClass;
        student.Status = model.Status;
        student.UpdatedBy = User.Identity?.Name ?? "system";
        student.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Student {student.FullName} updated successfully!";
        await LogAudit("Students", "EditStudent", $"Updated details for {student.FullName}");
        return RedirectToAction(nameof(Index));
    }

    // GET: /Students/ChangeStatus/5
    public async Task<IActionResult> ChangeStatus(int id)
    {
        var student = await _db.Students
            .Include(s => s.ClassLevel)
            .Include(s => s.ClassStream)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null) return NotFound();

        return View(student);
    }

    // POST: /Students/ChangeStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id,
        SMIS.Domain.Enums.StudentStatus status, string? leaveReason)
    {
        var student = await _db.Students.FindAsync(id);
        if (student == null) return NotFound();

        student.Status = status;
        student.LeaveReason = leaveReason;
        student.UpdatedBy = User.Identity?.Name ?? "system";
        student.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"{student.FullName}'s status updated to {status}.";
        await LogAudit("Students", "ChangeStatus",
    $"Changed {student.FullName}'s status to {status}" +
    (string.IsNullOrEmpty(leaveReason) ? "" : $" — {leaveReason}"));
        return RedirectToAction(nameof(Index));
    }
    // GET: /Students/Feedback/5
public async Task<IActionResult> Feedback(int id)
{
    var student = await _db.Students.FindAsync(id);
    if (student == null) return NotFound();

    var messages = await _db.StudentFeedbacks
        .Where(f => f.StudentId == id)
        .OrderBy(f => f.CreatedAt)
        .ToListAsync();

    var unread = messages.Where(m => !m.IsFromStaff && !m.IsRead).ToList();
    if (unread.Any())
    {
        foreach (var m in unread) m.IsRead = true;
        await _db.SaveChangesAsync();
    }

    ViewBag.Student = student;
    return View(messages);
}

// POST: /Students/SendStaffFeedback
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SendStaffFeedback(int studentId, string message)
{
    if (string.IsNullOrWhiteSpace(message))
    {
        TempData["Error"] = "Message cannot be empty.";
        return RedirectToAction(nameof(Feedback), new { id = studentId });
    }

    _db.StudentFeedbacks.Add(new SMIS.Domain.Entities.Communication.StudentFeedback
    {
        StudentId = studentId,
        SenderUserId = User.Identity?.Name ?? "staff",
        IsFromStaff = true,
        Message = message.Trim(),
        IsRead = false,
        CreatedBy = User.Identity?.Name ?? "system",
        CreatedAt = DateTime.UtcNow
    });
    await _db.SaveChangesAsync();

    return RedirectToAction(nameof(Feedback), new { id = studentId });
}

    // ── Helpers ───────────────────────────────────────────────────
    private async Task LoadDropdowns()
    {
        ViewBag.Classes = await _db.ClassLevels
            .OrderBy(c => c.OrderIndex).ToListAsync();
        ViewBag.Streams = await _db.ClassStreams
            .Include(s => s.ClassLevel)
            .OrderBy(s => s.ClassLevel.OrderIndex)
            .ToListAsync();
    }

    private async Task<string> GenerateAdmissionNumber()
    {
        var year = DateTime.Now.Year.ToString();
        var count = await _db.Students.CountAsync() + 1;
        return $"ADM/{year}/{count:D4}";
    }
}