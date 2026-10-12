using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Academic;
using SMIS.Domain.Enums;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

[Authorize(Roles = "Administrator,Principal,ClassTeacher,Teacher")]
public class CBCController : BaseController
{
    private new readonly ApplicationDbContext _db;

    public CBCController(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    // GET: /CBC
    public async Task<IActionResult> Index()
    {
        var classes = await _db.ClassLevels
            .Include(c => c.Streams)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();

        var currentTerm = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .FirstOrDefaultAsync(t => t.IsCurrent);

        ViewBag.CurrentTerm = currentTerm;
        return View(classes);
    }

    // GET: /CBC/EnterAssessment
    public async Task<IActionResult> EnterAssessment(int classLevelId,
        int? subjectId, int? termId)
    {
        var classLevel = await _db.ClassLevels.FindAsync(classLevelId);
        if (classLevel == null) return NotFound();

        var currentTerm = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .FirstOrDefaultAsync(t => t.IsCurrent);

        var selectedTerm = termId.HasValue
            ? await _db.AcademicTerms
                .Include(t => t.AcademicYear)
                .FirstOrDefaultAsync(t => t.Id == termId)
            : currentTerm;

        // Get subjects for this class level
        var subjects = await _db.Subjects
            .Where(s => s.Level == classLevel.Level && s.IsActive)
            .OrderBy(s => s.OrderIndex)
            .ToListAsync();

        // Get students in this class
        var students = await _db.Students
            .Where(s => s.ClassLevelId == classLevelId &&
                        s.Status == StudentStatus.Active)
            .OrderBy(s => s.LastName)
            .ToListAsync();

        // Get existing assessments
        var existingAssessments = selectedTerm != null
            ? await _db.CBCAssessments
                .Where(a => a.ClassLevelId == classLevelId &&
                            a.AcademicTermId == selectedTerm.Id)
                .ToListAsync()
            : new List<CBCAssessment>();

        ViewBag.ClassLevel = classLevel;
        ViewBag.Subjects = subjects;
        ViewBag.SelectedTerm = selectedTerm;
        ViewBag.CurrentTerm = currentTerm;
        ViewBag.SelectedSubjectId = subjectId ?? subjects.FirstOrDefault()?.Id;
        ViewBag.ExistingAssessments = existingAssessments;
        ViewBag.Terms = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .OrderByDescending(t => t.AcademicYear.Year)
            .ToListAsync();

        return View(students);
    }

    // POST: /CBC/SaveAssessment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAssessment(int classLevelId,
        int subjectId, int academicTermId, int academicYearId,
        List<int> studentIds, List<int> performanceLevels,
        List<string?> teacherRemarks, List<decimal?> scores)
    {
        for (int i = 0; i < studentIds.Count; i++)
        {
            // Check if assessment already exists
            var existing = await _db.CBCAssessments
                .FirstOrDefaultAsync(a =>
                    a.StudentId == studentIds[i] &&
                    a.SubjectId == subjectId &&
                    a.AcademicTermId == academicTermId);

            if (existing != null)
            {
                // Update existing
                existing.PerformanceLevel = performanceLevels.Count > i
    ? (PerformanceLevel)performanceLevels[i]
    : PerformanceLevel.MeetExpectation;
                existing.TeacherRemarks = teacherRemarks.Count > i
                    ? teacherRemarks[i] : null;
                existing.Score = scores.Count > i ? scores[i] : null;
                existing.UpdatedBy = User.Identity?.Name ?? "system";
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new
                var assessment = new CBCAssessment
                {
                    StudentId = studentIds[i],
                    SubjectId = subjectId,
                    AcademicTermId = academicTermId,
                    AcademicYearId = academicYearId,
                    ClassLevelId = classLevelId,
                    PerformanceLevel = performanceLevels.Count > i
    ? (PerformanceLevel)performanceLevels[i]
    : PerformanceLevel.MeetExpectation,
                    TeacherRemarks = teacherRemarks.Count > i
                        ? teacherRemarks[i] : null,
                    Score = scores.Count > i ? scores[i] : null,
                    AssessmentType = AssessmentType.Summative,
                    CreatedBy = User.Identity?.Name ?? "system",
                    CreatedAt = DateTime.UtcNow
                };
                _db.CBCAssessments.Add(assessment);
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Assessment saved successfully!";
        await LogAudit("CBC", "SaveAssessment",
    $"Saved assessment for {studentIds.Count} students — Subject #{subjectId}");
        return RedirectToAction(nameof(EnterAssessment),
            new { classLevelId, subjectId, termId = academicTermId });
    }

    // GET: /CBC/ReportCard/5
    public async Task<IActionResult> ReportCard(int studentId, int? termId)
    {
        var student = await _db.Students
            .Include(s => s.ClassLevel)
            .Include(s => s.ClassStream)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null) return NotFound();

        var currentTerm = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .FirstOrDefaultAsync(t => t.IsCurrent);

        var selectedTerm = termId.HasValue
            ? await _db.AcademicTerms
                .Include(t => t.AcademicYear)
                .FirstOrDefaultAsync(t => t.Id == termId)
            : currentTerm;

        // Get assessments for this student and term
        var assessments = selectedTerm != null
            ? await _db.CBCAssessments
                .Include(a => a.Subject)
                .Where(a => a.StudentId == studentId &&
                            a.AcademicTermId == selectedTerm.Id)
                .OrderBy(a => a.Subject.OrderIndex)
                .ToListAsync()
            : new List<CBCAssessment>();

        // Get attendance summary
        var attendance = selectedTerm != null
            ? await _db.AttendanceRecords
                .Where(a => a.StudentId == studentId &&
                            a.AcademicTermId == selectedTerm.Id)
                .ToListAsync()
            : new List<AttendanceRecord>();

        // Get or create report card
        var reportCard = selectedTerm != null
            ? await _db.CBCReportCards
                .FirstOrDefaultAsync(r =>
                    r.StudentId == studentId &&
                    r.AcademicTermId == selectedTerm.Id)
            : null;

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

        ViewBag.Student = student;
        ViewBag.SelectedTerm = selectedTerm;
        ViewBag.CurrentTerm = currentTerm;
        ViewBag.Assessments = assessments;
        ViewBag.Attendance = attendance;
        ViewBag.ReportCard = reportCard;
        ViewBag.School = school;
        ViewBag.Terms = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .OrderByDescending(t => t.AcademicYear.Year)
            .ToListAsync();
        ViewBag.DaysPresent = attendance.Count(a =>
            a.Status == AttendanceStatus.Present);
        ViewBag.DaysAbsent = attendance.Count(a =>
            a.Status == AttendanceStatus.Absent);

        return View();
    }

    // POST: /CBC/SaveReportCard
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveReportCard(int studentId,
        int academicTermId, int academicYearId, int classLevelId,
        string? classTeacherRemarks, string? principalRemarks,
        string? conduct, string? punctuality, string? neatness,
        string? sportActivities, string? clubActivities, string? talents,
        DateTime? nextTermBegins, bool isPromoted)
    {
        var existing = await _db.CBCReportCards
            .FirstOrDefaultAsync(r =>
                r.StudentId == studentId &&
                r.AcademicTermId == academicTermId);

        // Get attendance counts
        var attendance = await _db.AttendanceRecords
            .Where(a => a.StudentId == studentId &&
                        a.AcademicTermId == academicTermId)
            .ToListAsync();

        if (existing != null)
        {
            existing.ClassTeacherRemarks = classTeacherRemarks;
            existing.PrincipalRemarks = principalRemarks;
            existing.Conduct = conduct;
            existing.Punctuality = punctuality;
            existing.Neatness = neatness;
            existing.SportActivities = sportActivities;
            existing.ClubActivities = clubActivities;
            existing.Talents = talents;
            existing.NextTermBegins = nextTermBegins;
            existing.IsPromoted = isPromoted;
            existing.DaysPresent = attendance.Count(a =>
                a.Status == AttendanceStatus.Present);
            existing.DaysAbsent = attendance.Count(a =>
                a.Status == AttendanceStatus.Absent);
            existing.TotalSchoolDays = attendance.Count;
            existing.UpdatedBy = User.Identity?.Name ?? "system";
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var reportCard = new CBCReportCard
            {
                StudentId = studentId,
                AcademicTermId = academicTermId,
                AcademicYearId = academicYearId,
                ClassLevelId = classLevelId,
                ClassTeacherRemarks = classTeacherRemarks,
                PrincipalRemarks = principalRemarks,
                Conduct = conduct,
                Punctuality = punctuality,
                Neatness = neatness,
                SportActivities = sportActivities,
                ClubActivities = clubActivities,
                Talents = talents,
                NextTermBegins = nextTermBegins,
                IsPromoted = isPromoted,
                DaysPresent = attendance.Count(a =>
                    a.Status == AttendanceStatus.Present),
                DaysAbsent = attendance.Count(a =>
                    a.Status == AttendanceStatus.Absent),
                TotalSchoolDays = attendance.Count,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow
            };
            _db.CBCReportCards.Add(reportCard);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Report card saved successfully!";
        return RedirectToAction(nameof(ReportCard),
            new { studentId, termId = academicTermId });
    }

    // GET: /CBC/ClassReport
    public async Task<IActionResult> ClassReport(int classLevelId, int? termId)
    {
        var classLevel = await _db.ClassLevels.FindAsync(classLevelId);
        if (classLevel == null) return NotFound();

        var currentTerm = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .FirstOrDefaultAsync(t => t.IsCurrent);

        var selectedTerm = termId.HasValue
            ? await _db.AcademicTerms
                .Include(t => t.AcademicYear)
                .FirstOrDefaultAsync(t => t.Id == termId)
            : currentTerm;

        var students = await _db.Students
            .Where(s => s.ClassLevelId == classLevelId &&
                        s.Status == StudentStatus.Active)
            .OrderBy(s => s.LastName)
            .ToListAsync();

        var assessments = selectedTerm != null
            ? await _db.CBCAssessments
                .Include(a => a.Subject)
                .Include(a => a.Student)
                .Where(a => a.ClassLevelId == classLevelId &&
                            a.AcademicTermId == selectedTerm.Id)
                .ToListAsync()
            : new List<CBCAssessment>();

        var subjects = await _db.Subjects
            .Where(s => s.Level == classLevel.Level && s.IsActive)
            .OrderBy(s => s.OrderIndex)
            .ToListAsync();

        ViewBag.ClassLevel = classLevel;
        ViewBag.SelectedTerm = selectedTerm;
        ViewBag.Subjects = subjects;
        ViewBag.Assessments = assessments;
        ViewBag.Terms = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .OrderByDescending(t => t.AcademicYear.Year)
            .ToListAsync();
        ViewBag.Classes = await _db.ClassLevels
            .OrderBy(c => c.OrderIndex).ToListAsync();

        return View(students);
    }
// GET: /CBC/DownloadReportCard/5
    public async Task<IActionResult> DownloadReportCard(int studentId, int termId)
    {
        var student = await _db.Students
            .Include(s => s.ClassLevel)
            .Include(s => s.ClassStream)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null) return NotFound();

        var term = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .FirstOrDefaultAsync(t => t.Id == termId);

        var assessments = await _db.CBCAssessments
            .Include(a => a.Subject)
            .Where(a => a.StudentId == studentId && a.AcademicTermId == termId)
            .OrderBy(a => a.Subject.OrderIndex)
            .ToListAsync();

        var reportCard = await _db.CBCReportCards
            .FirstOrDefaultAsync(r =>
                r.StudentId == studentId && r.AcademicTermId == termId);

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

        var attendance = await _db.AttendanceRecords
            .Where(a => a.StudentId == studentId && a.AcademicTermId == termId)
            .ToListAsync();

        var daysPresent = attendance.Count(a =>
            a.Status == AttendanceStatus.Present);
        var daysAbsent = attendance.Count(a =>
            a.Status == AttendanceStatus.Absent);

        var pdfService = HttpContext.RequestServices
            .GetRequiredService<SMIS.Web.Services.PdfService>();

        var pdf = pdfService.GenerateReportCardPdf(
            student, term!, school, assessments,
            reportCard, daysPresent, daysAbsent);

        return File(pdf, "application/pdf",
            $"ReportCard_{student.AdmissionNumber}_{term?.Name}.pdf");
    }

    // GET: /CBC/DownloadBulkReportCards
    public async Task<IActionResult> DownloadBulkReportCards(
        int classLevelId, int termId)
    {
        var students = await _db.Students
            .Include(s => s.ClassLevel)
            .Include(s => s.ClassStream)
            .Where(s => s.ClassLevelId == classLevelId &&
                        s.Status == StudentStatus.Active)
            .OrderBy(s => s.LastName)
            .ToListAsync();

        if (!students.Any())
        {
            TempData["Error"] = "No students found for this class!";
            return RedirectToAction(nameof(ClassReport),
                new { classLevelId, termId });
        }

        var term = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .FirstOrDefaultAsync(t => t.Id == termId);

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

        var pdfService = HttpContext.RequestServices
            .GetRequiredService<SMIS.Web.Services.PdfService>();

        var allPdfs = new List<byte[]>();

        foreach (var student in students)
        {
            var assessments = await _db.CBCAssessments
                .Include(a => a.Subject)
                .Where(a => a.StudentId == student.Id &&
                            a.AcademicTermId == termId)
                .OrderBy(a => a.Subject.OrderIndex)
                .ToListAsync();

            var reportCard = await _db.CBCReportCards
                .FirstOrDefaultAsync(r =>
                    r.StudentId == student.Id &&
                    r.AcademicTermId == termId);

            var attendance = await _db.AttendanceRecords
                .Where(a => a.StudentId == student.Id &&
                            a.AcademicTermId == termId)
                .ToListAsync();

            var daysPresent = attendance.Count(a =>
                a.Status == AttendanceStatus.Present);
            var daysAbsent = attendance.Count(a =>
                a.Status == AttendanceStatus.Absent);

            var pdf = pdfService.GenerateReportCardPdf(
                student, term!, school, assessments,
                reportCard, daysPresent, daysAbsent);

            allPdfs.Add(pdf);
        }

        // Merge all PDFs
        using var outputStream = new MemoryStream();
        foreach (var pdf in allPdfs)
            outputStream.Write(pdf, 0, pdf.Length);

        var classLevel = await _db.ClassLevels.FindAsync(classLevelId);
        return File(outputStream.ToArray(), "application/pdf",
            $"ReportCards_{classLevel?.Name}_{term?.Name}.pdf");
    }
}