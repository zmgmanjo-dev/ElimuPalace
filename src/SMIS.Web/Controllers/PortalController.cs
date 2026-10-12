using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Students;
using SMIS.Domain.Entities.Academic;
using SMIS.Infrastructure.Data;


namespace SMIS.Web.Controllers;

[Authorize(Roles = "Parent")]
public class PortalController : Controller
{
    private readonly ApplicationDbContext _db;
private readonly UserManager<IdentityUser> _userManager;
private readonly SMIS.Web.Services.PdfService _pdfService;

public PortalController(
    ApplicationDbContext db,
    UserManager<IdentityUser> userManager,
    SMIS.Web.Services.PdfService pdfService)
{
    _db = db;
    _userManager = userManager;
    _pdfService = pdfService;
}

    /// <summary>
    /// Returns the list of students linked to the currently logged-in parent.
    /// Every portal action must go through this — never trust a raw studentId
    /// from the query string without checking it against this list.
    /// </summary>
    private async Task<List<Student>> GetLinkedStudentsAsync()
    {
        var userId = _userManager.GetUserId(User);
        var links = await _db.ParentStudentLinks
            .Include(l => l.Student).ThenInclude(s => s.ClassLevel)
            .Include(l => l.Student).ThenInclude(s => s.ClassStream)
            .Where(l => l.ParentUserId == userId)
            .ToListAsync();

        return links.Select(l => l.Student).ToList();
    }

    private async Task<Student?> GetAuthorizedStudentAsync(int studentId)
    {
        var linked = await GetLinkedStudentsAsync();
        return linked.FirstOrDefault(s => s.Id == studentId);
    }

    // GET: /Portal/Dashboard
    public async Task<IActionResult> Dashboard(int? studentId)
    {
        var children = await GetLinkedStudentsAsync();

        if (!children.Any())
        {
            ViewBag.NoChildrenLinked = true;
            return View();
        }

        var selected = studentId.HasValue
            ? children.FirstOrDefault(s => s.Id == studentId)
            : children.First();

        if (selected == null)
        {
            TempData["Error"] = "You are not authorized to view that student.";
            return RedirectToAction(nameof(Dashboard));
        }

        var currentTerm = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .FirstOrDefaultAsync(t => t.IsCurrent);

        // Fee snapshot
        var invoices = await _db.StudentInvoices
            .Where(i => i.StudentId == selected.Id)
            .ToListAsync();
        var totalInvoiced = invoices.Sum(i => i.TotalAmount);
        var totalPaid = invoices.Sum(i => i.TotalPaid);
        var balance = invoices.Sum(i => i.Balance);

        // Attendance snapshot (current term)
        var attendanceQuery = _db.AttendanceRecords
            .Where(a => a.StudentId == selected.Id);
        if (currentTerm != null)
            attendanceQuery = attendanceQuery.Where(a => a.AcademicTermId == currentTerm.Id);
        var attendanceRecords = await attendanceQuery.ToListAsync();
        var present = attendanceRecords.Count(a => a.Status == Domain.Entities.Academic.AttendanceStatus.Present);
        var totalMarked = attendanceRecords.Count;
        var attendanceRate = totalMarked > 0 ? Math.Round((double)present / totalMarked * 100, 1) : 0;

        // Latest CBC assessments (current term)
        var assessmentsQuery = _db.CBCAssessments
            .Include(a => a.Subject)
            .Where(a => a.StudentId == selected.Id);
        if (currentTerm != null)
            assessmentsQuery = assessmentsQuery.Where(a => a.AcademicTermId == currentTerm.Id);
        var assessments = await assessmentsQuery
            .OrderBy(a => a.Subject.OrderIndex).ToListAsync();

        // Unread feedback count
        var unreadFeedback = await _db.StudentFeedbacks
            .CountAsync(f => f.StudentId == selected.Id && f.IsFromStaff && !f.IsRead);

        // Latest published newsletters
        var newsletters = await _db.Newsletters
            .Where(n => n.IsPublished)
            .OrderByDescending(n => n.PublishedAt)
            .Take(3).ToListAsync();

        ViewBag.Children = children;
        ViewBag.SelectedStudent = selected;
        ViewBag.CurrentTerm = currentTerm;
        ViewBag.TotalInvoiced = totalInvoiced;
        ViewBag.TotalPaid = totalPaid;
        ViewBag.Balance = balance;
        ViewBag.AttendanceRate = attendanceRate;
        ViewBag.Present = present;
        ViewBag.TotalMarked = totalMarked;
        ViewBag.Assessments = assessments;
        ViewBag.UnreadFeedback = unreadFeedback;
        ViewBag.Newsletters = newsletters;

        return View();
    }

    // GET: /Portal/FeeStatement?studentId=5
    public async Task<IActionResult> FeeStatement(int studentId)
    {
        var student = await GetAuthorizedStudentAsync(studentId);
        if (student == null)
        {
            TempData["Error"] = "You are not authorized to view that student's records.";
            return RedirectToAction(nameof(Dashboard));
        }

        var invoices = await _db.StudentInvoices
            .Include(i => i.AcademicYear)
            .Include(i => i.AcademicTerm)
            .Include(i => i.Items).ThenInclude(item => item.FeeCategory)
            .Include(i => i.Payments)
            .Where(i => i.StudentId == studentId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        var ledger = ReportsController.BuildStudentLedgerPublic(invoices);

        ViewBag.Student = student;
        ViewBag.Ledger = ledger;
        ViewBag.TotalInvoiced = invoices.Sum(i => i.TotalAmount);
        ViewBag.TotalPaid = invoices.Sum(i => i.TotalPaid);
        ViewBag.TotalBalance = invoices.Sum(i => i.Balance);
        ViewBag.Children = await GetLinkedStudentsAsync();

        return View(invoices);
    }

    // GET: /Portal/Attendance?studentId=5
    public async Task<IActionResult> Attendance(int studentId, int? termId)
    {
        var student = await GetAuthorizedStudentAsync(studentId);
        if (student == null)
        {
            TempData["Error"] = "You are not authorized to view that student's records.";
            return RedirectToAction(nameof(Dashboard));
        }

        var terms = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .OrderByDescending(t => t.Id).ToListAsync();
        var currentTerm = terms.FirstOrDefault(t => t.IsCurrent);
        var selectedTermId = termId ?? currentTerm?.Id;

        var records = await _db.AttendanceRecords
            .Where(a => a.StudentId == studentId &&
                        (selectedTermId == null || a.AcademicTermId == selectedTermId))
            .OrderByDescending(a => a.AttendanceDate)
            .ToListAsync();

        ViewBag.Student = student;
        ViewBag.Terms = terms;
        ViewBag.SelectedTermId = selectedTermId;
        ViewBag.Present = records.Count(r => r.Status == Domain.Entities.Academic.AttendanceStatus.Present);
        ViewBag.Absent = records.Count(r => r.Status == Domain.Entities.Academic.AttendanceStatus.Absent);
        ViewBag.Late = records.Count(r => r.Status == Domain.Entities.Academic.AttendanceStatus.Late);
        ViewBag.Excused = records.Count(r => r.Status == Domain.Entities.Academic.AttendanceStatus.Excused);
        ViewBag.Children = await GetLinkedStudentsAsync();

        return View(records);
    }

    // GET: /Portal/ReportCard?studentId=5
    public async Task<IActionResult> ReportCard(int studentId, int? termId)
    {
        var student = await GetAuthorizedStudentAsync(studentId);
        if (student == null)
        {
            TempData["Error"] = "You are not authorized to view that student's records.";
            return RedirectToAction(nameof(Dashboard));
        }

        var terms = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .Where(t => _db.CBCAssessments.Any(a => a.StudentId == studentId && a.AcademicTermId == t.Id))
            .OrderByDescending(t => t.Id).ToListAsync();
        var selectedTermId = termId ?? terms.FirstOrDefault()?.Id;

        var assessments = await _db.CBCAssessments
            .Include(a => a.Subject)
            .Where(a => a.StudentId == studentId &&
                        (selectedTermId == null || a.AcademicTermId == selectedTermId))
            .OrderBy(a => a.Subject.OrderIndex)
            .ToListAsync();

        var reportCard = selectedTermId.HasValue
            ? await _db.CBCReportCards
                .FirstOrDefaultAsync(r => r.StudentId == studentId && r.AcademicTermId == selectedTermId)
            : null;

        ViewBag.Student = student;
        ViewBag.Terms = terms;
        ViewBag.SelectedTermId = selectedTermId;
        ViewBag.ReportCard = reportCard;
        ViewBag.Children = await GetLinkedStudentsAsync();

        return View(assessments);
    }
    // GET: /Portal/Feedback?studentId=5
public async Task<IActionResult> Feedback(int studentId)
{
    var student = await GetAuthorizedStudentAsync(studentId);
    if (student == null)
    {
        TempData["Error"] = "You are not authorized to view that student's records.";
        return RedirectToAction(nameof(Dashboard));
    }

    var messages = await _db.StudentFeedbacks
        .Where(f => f.StudentId == studentId)
        .OrderBy(f => f.CreatedAt)
        .ToListAsync();

    // Mark staff messages as read now that the parent is viewing them
    var unread = messages.Where(m => m.IsFromStaff && !m.IsRead).ToList();
    if (unread.Any())
    {
        foreach (var m in unread) m.IsRead = true;
        await _db.SaveChangesAsync();
    }

    ViewBag.Student = student;
    ViewBag.Children = await GetLinkedStudentsAsync();
    return View(messages);
}

// POST: /Portal/SendFeedback
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SendFeedback(int studentId, string message)
{
    var student = await GetAuthorizedStudentAsync(studentId);
    if (student == null)
    {
        TempData["Error"] = "You are not authorized to view that student's records.";
        return RedirectToAction(nameof(Dashboard));
    }

    if (string.IsNullOrWhiteSpace(message))
    {
        TempData["Error"] = "Message cannot be empty.";
        return RedirectToAction(nameof(Feedback), new { studentId });
    }

    _db.StudentFeedbacks.Add(new SMIS.Domain.Entities.Communication.StudentFeedback
    {
        StudentId = studentId,
        SenderUserId = _userManager.GetUserId(User)!,
        IsFromStaff = false,
        Message = message.Trim(),
        IsRead = false,
        CreatedBy = User.Identity?.Name ?? "parent",
        CreatedAt = DateTime.UtcNow
    });
    await _db.SaveChangesAsync();

    return RedirectToAction(nameof(Feedback), new { studentId });
}
// GET: /Portal/Newsletter
public async Task<IActionResult> Newsletter()
{
    var newsletters = await _db.Newsletters
        .Where(n => n.IsPublished)
        .OrderByDescending(n => n.PublishedAt)
        .ToListAsync();

    ViewBag.Children = await GetLinkedStudentsAsync();
    return View(newsletters);
}
// ── PDF: FEE STATEMENT ───────────────────────────────────────
public async Task<IActionResult> FeeStatementPdf(int studentId, bool download = true)
{
    var student = await GetAuthorizedStudentAsync(studentId);
    if (student == null) 
    {
        TempData["Error"] = "You are not authorized to view that student's records.";
        return RedirectToAction(nameof(Dashboard));
    }

    var invoices = await _db.StudentInvoices
        .Include(i => i.AcademicYear)
        .Include(i => i.AcademicTerm)
        .Include(i => i.Items).ThenInclude(item => item.FeeCategory)
        .Include(i => i.Payments)
        .Where(i => i.StudentId == studentId)
        .OrderByDescending(i => i.InvoiceDate)
        .ToListAsync();

    var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
    var ledger = ReportsController.BuildStudentLedgerPublic(invoices);

    var ledgerHeaders = new List<string> { "Date", "Description", "Term", "Invoiced", "Paid", "Balance" };
    var ledgerRows = ledger.Select(l => new List<string>
    {
        ((DateTime)l.Date).ToString("dd MMM yyyy"),
        (string)l.Description,
        (string)l.Term,
        ((decimal)l.Debit) > 0 ? ((decimal)l.Debit).ToString("N0") : "-",
        ((decimal)l.Credit) > 0 ? ((decimal)l.Credit).ToString("N0") : "-",
        ((decimal)l.Balance).ToString("N0")
    }).ToList();

    var invoiceHeaders = new List<string> { "Invoice No", "Term", "Description", "Amount", "Discount", "Net", "Status" };
    var invoiceRows = new List<List<string>>();
    foreach (var inv in invoices)
    {
        foreach (var item in inv.Items)
        {
            invoiceRows.Add(new List<string>
            {
                inv.InvoiceNumber,
                $"{inv.AcademicYear?.Name} {inv.AcademicTerm?.Name}",
                item.Description,
                item.Amount.ToString("N0"),
                item.Discount > 0 ? item.Discount.ToString("N0") : "-",
                item.NetAmount.ToString("N0"),
                inv.Status.ToString()
            });
        }
    }

    var stats = new List<(string, string)>
    {
        ("Total Invoiced", $"KES {invoices.Sum(i => i.TotalAmount):N0}"),
        ("Total Paid", $"KES {invoices.Sum(i => i.TotalPaid):N0}"),
        ("Current Balance", $"KES {invoices.Sum(i => i.Balance):N0}"),
    };

    var sections = new List<(string, List<string>, List<List<string>>)>
    {
        ("Statement Ledger (Running Balance)", ledgerHeaders, ledgerRows),
        ("Invoice Items", invoiceHeaders, invoiceRows)
    };

    var pdfBytes = _pdfService.GenerateMultiTableReportPdf(
        "Student Fee Statement",
        $"{student.FullName} | Adm: {student.AdmissionNumber} | Class: {student.ClassLevel?.Name ?? "-"}",
        sections, school, stats);

    var fileName = $"Statement_{student.AdmissionNumber?.Replace("/", "-")}.pdf";
    return download
        ? File(pdfBytes, "application/pdf", fileName)
        : File(pdfBytes, "application/pdf");
}

// ── PDF: ATTENDANCE ──────────────────────────────────────────
public async Task<IActionResult> AttendancePdf(int studentId, int? termId, bool download = true)
{
    var student = await GetAuthorizedStudentAsync(studentId);
    if (student == null)
    {
        TempData["Error"] = "You are not authorized to view that student's records.";
        return RedirectToAction(nameof(Dashboard));
    }


    var terms = await _db.AcademicTerms
        .Include(t => t.AcademicYear)
        .OrderByDescending(t => t.Id).ToListAsync();
    var currentTerm = terms.FirstOrDefault(t => t.IsCurrent);
    var selectedTermId = termId ?? currentTerm?.Id;
    var selectedTerm = terms.FirstOrDefault(t => t.Id == selectedTermId);

    var records = await _db.AttendanceRecords
        .Where(a => a.StudentId == studentId &&
                    (selectedTermId == null || a.AcademicTermId == selectedTermId))
        .OrderByDescending(a => a.AttendanceDate)
        .ToListAsync();

    var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

    var headers = new List<string> { "Date", "Day", "Status" };
    var rows = records.Select(r => new List<string>
    {
        r.AttendanceDate.ToString("dd MMM yyyy"),
        r.AttendanceDate.ToString("dddd"),
        r.Status.ToString()
    }).ToList();

    var present = records.Count(r => r.Status == AttendanceStatus.Present);
    var total = records.Count;
    var rate = total > 0 ? Math.Round((double)present / total * 100, 1) : 0;

    var stats = new List<(string, string)>
    {
        ("Present", present.ToString()),
        ("Absent", records.Count(r => r.Status == AttendanceStatus.Absent).ToString()),
        ("Late", records.Count(r => r.Status == AttendanceStatus.Late).ToString()),
        ("Excused", records.Count(r => r.Status == AttendanceStatus.Excused).ToString()),
        ("Attendance Rate", $"{rate}%"),
    };

    var subtitle = selectedTerm != null
        ? $"{student.FullName} | Adm: {student.AdmissionNumber} | {selectedTerm.AcademicYear?.Name} {selectedTerm.Name}"
        : $"{student.FullName} | Adm: {student.AdmissionNumber}";

    var pdfBytes = _pdfService.GenerateTableReportPdf(
        "Attendance Report", subtitle, headers, rows, school, stats);

    var fileName = $"Attendance_{student.AdmissionNumber?.Replace("/", "-")}.pdf";
    return download
        ? File(pdfBytes, "application/pdf", fileName)
        : File(pdfBytes, "application/pdf");
}

// ── PDF: REPORT CARD ─────────────────────────────────────────
public async Task<IActionResult> ReportCardPdf(int studentId, int? termId, bool download = true)
{
    var student = await GetAuthorizedStudentAsync(studentId);
    if (student == null)
    {
        TempData["Error"] = "You are not authorized to view that student's records.";
        return RedirectToAction(nameof(Dashboard));
    }

    var terms = await _db.AcademicTerms
        .Include(t => t.AcademicYear)
        .Where(t => _db.CBCAssessments.Any(a => a.StudentId == studentId && a.AcademicTermId == t.Id))
        .OrderByDescending(t => t.Id).ToListAsync();
    var selectedTermId = termId ?? terms.FirstOrDefault()?.Id;
    var term = terms.FirstOrDefault(t => t.Id == selectedTermId);

    if (term == null)
    {
        TempData["Error"] = "No assessments available to generate a report card.";
        return RedirectToAction(nameof(ReportCard), new { studentId });
    }

    var assessments = await _db.CBCAssessments
        .Include(a => a.Subject)
        .Where(a => a.StudentId == studentId && a.AcademicTermId == selectedTermId)
        .OrderBy(a => a.Subject.OrderIndex)
        .ToListAsync();

    var reportCard = await _db.CBCReportCards
        .FirstOrDefaultAsync(r => r.StudentId == studentId && r.AcademicTermId == selectedTermId);

    var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

    var daysPresent = await _db.AttendanceRecords
        .CountAsync(a => a.StudentId == studentId &&
                         a.AcademicTermId == selectedTermId &&
                         a.Status == AttendanceStatus.Present);
    var daysAbsent = await _db.AttendanceRecords
        .CountAsync(a => a.StudentId == studentId &&
                         a.AcademicTermId == selectedTermId &&
                         a.Status == AttendanceStatus.Absent);

    var pdfBytes = _pdfService.GenerateReportCardPdf(
        student, term, school, assessments, reportCard, daysPresent, daysAbsent);

    var fileName = $"ReportCard_{student.AdmissionNumber?.Replace("/", "-")}_{term.Name}.pdf";
    return download
        ? File(pdfBytes, "application/pdf", fileName)
        : File(pdfBytes, "application/pdf");
}
}