using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Academic;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

[Authorize(Roles = "Administrator,Principal,ClassTeacher,Teacher")]
public class AttendanceController : BaseController
{
    private new readonly ApplicationDbContext _db;

    public AttendanceController(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    // GET: /Attendance
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

    // GET: /Attendance/TakeAttendance
    public async Task<IActionResult> TakeAttendance(int classLevelId,
        int? classStreamId, DateTime? date)
    {
        var attendanceDate = date ?? DateTime.Today;

        var classLevel = await _db.ClassLevels.FindAsync(classLevelId);
        if (classLevel == null) return NotFound();

        var currentTerm = await _db.AcademicTerms
            .FirstOrDefaultAsync(t => t.IsCurrent);

        // Get students in this class
        var studentsQuery = _db.Students
            .Where(s => s.ClassLevelId == classLevelId &&
                        s.Status == Domain.Enums.StudentStatus.Active);

        if (classStreamId.HasValue)
            studentsQuery = studentsQuery
                .Where(s => s.ClassStreamId == classStreamId);

        var students = await studentsQuery
            .OrderBy(s => s.LastName)
            .ToListAsync();

        // Get existing attendance for this date
        var existingAttendance = await _db.AttendanceRecords
            .Where(a => a.ClassLevelId == classLevelId &&
                        a.AttendanceDate.Date == attendanceDate.Date)
            .ToListAsync();

        ViewBag.ClassLevel = classLevel;
        ViewBag.ClassStreamId = classStreamId;
        ViewBag.AttendanceDate = attendanceDate;
        ViewBag.CurrentTerm = currentTerm;
        ViewBag.ExistingAttendance = existingAttendance;
        ViewBag.Streams = await _db.ClassStreams
            .Where(s => s.ClassLevelId == classLevelId)
            .ToListAsync();

        return View(students);
    }

    // POST: /Attendance/SaveAttendance
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAttendance(int classLevelId,
        int? classStreamId, DateTime attendanceDate, int academicTermId,
        List<int> studentIds, List<int> statuses, List<string?> excuseReasons)
    {
        // Delete existing attendance for this date and class
        var existing = await _db.AttendanceRecords
            .Where(a => a.ClassLevelId == classLevelId &&
                        a.AttendanceDate.Date == attendanceDate.Date)
            .ToListAsync();
        _db.AttendanceRecords.RemoveRange(existing);

        // Save new attendance
        for (int i = 0; i < studentIds.Count; i++)
        {
            var record = new AttendanceRecord
            {
                StudentId = studentIds[i],
                ClassLevelId = classLevelId,
                ClassStreamId = classStreamId,
                AcademicTermId = academicTermId,
                AttendanceDate = attendanceDate.Date,
                Status = statuses.Count > i
    ? (AttendanceStatus)statuses[i]
    : AttendanceStatus.Present,
                ExcuseReason = excuseReasons.Count > i ? excuseReasons[i] : null,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow
            };
            _db.AttendanceRecords.Add(record);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Attendance saved for " +
            $"{attendanceDate:dd MMM yyyy} — {studentIds.Count} students!";
            await LogAudit("Attendance", "SaveAttendance",
    $"Saved attendance for {studentIds.Count} students — {attendanceDate:dd MMM yyyy}");
        return RedirectToAction(nameof(TakeAttendance),
            new { classLevelId, classStreamId, date = attendanceDate });
    }

    // GET: /Attendance/Report
    public async Task<IActionResult> Report(int? classLevelId,
        int? studentId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? DateTime.Today.AddDays(-30);
        var to = toDate ?? DateTime.Today;

        var query = _db.AttendanceRecords
            .Include(a => a.Student)
            .Include(a => a.ClassLevel)
            .Where(a => a.AttendanceDate.Date >= from.Date &&
                        a.AttendanceDate.Date <= to.Date);

        if (classLevelId.HasValue)
            query = query.Where(a => a.ClassLevelId == classLevelId);

        if (studentId.HasValue)
            query = query.Where(a => a.StudentId == studentId);

        var records = await query
            .OrderByDescending(a => a.AttendanceDate)
            .ThenBy(a => a.Student.LastName)
            .ToListAsync();

        ViewBag.Classes = await _db.ClassLevels
            .OrderBy(c => c.OrderIndex).ToListAsync();
        ViewBag.Students = await _db.Students
            .OrderBy(s => s.LastName).ToListAsync();
        ViewBag.ClassLevelId = classLevelId;
        ViewBag.StudentId = studentId;
        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.TotalPresent = records.Count(r =>
            r.Status == AttendanceStatus.Present);
        ViewBag.TotalAbsent = records.Count(r =>
            r.Status == AttendanceStatus.Absent);
        ViewBag.TotalLate = records.Count(r =>
            r.Status == AttendanceStatus.Late);
        ViewBag.TotalExcused = records.Count(r =>
            r.Status == AttendanceStatus.Excused);

        return View(records);
    }
}