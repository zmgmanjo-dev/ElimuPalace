using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Academic;
using SMIS.Domain.Entities.Finance;
using SMIS.Domain.Entities.Library;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

[Authorize(Roles = "Administrator,Principal,Bursar")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
private readonly SMIS.Web.Services.PdfService _pdfService;

public ReportsController(ApplicationDbContext db, SMIS.Web.Services.PdfService pdfService)
{
    _db = db;
    _pdfService = pdfService;
}

    // GET: /Reports
    public IActionResult Index() => View();

    // ── FINANCIAL REPORTS ─────────────────────────────────────────

    public async Task<IActionResult> FeeCollection(
        DateTime? fromDate, DateTime? toDate,
        int? termId, int? studentId)
    {
        var from = fromDate ?? DateTime.Today.AddDays(-30);
        var to = toDate ?? DateTime.Today;

        var query = _db.FeePayments
            .Include(p => p.Student).ThenInclude(s => s.ClassLevel)
            .Include(p => p.StudentInvoice)
                .ThenInclude(i => i.AcademicTerm)
            .Where(p => p.PaymentDate.Date >= from.Date &&
                        p.PaymentDate.Date <= to.Date &&
                        !p.IsReversed);

        if (termId.HasValue)
            query = query.Where(p =>
                p.StudentInvoice.AcademicTermId == termId);
        if (studentId.HasValue)
            query = query.Where(p => p.StudentId == studentId);

        var payments = await query
            .OrderByDescending(p => p.PaymentDate).ToListAsync();

        await LoadCommonViewBags(termId, null, studentId);
        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.TermId = termId;
        ViewBag.StudentId = studentId;
        ViewBag.TotalCollected = payments.Sum(p => p.Amount);
        ViewBag.CashTotal = payments
            .Where(p => p.PaymentMethod == PaymentMethod.Cash)
            .Sum(p => p.Amount);
        ViewBag.MpesaTotal = payments
            .Where(p => p.PaymentMethod == PaymentMethod.MPesa)
            .Sum(p => p.Amount);
        ViewBag.BankTotal = payments
            .Where(p => p.PaymentMethod == PaymentMethod.BankTransfer)
            .Sum(p => p.Amount);

        return View(payments);
    }

    public async Task<IActionResult> OutstandingFees(
        int? termId, int? classId, int? studentId)
    {
        var currentTerm = await _db.AcademicTerms
            .FirstOrDefaultAsync(t => t.IsCurrent);

        var query = _db.StudentInvoices
            .Include(i => i.Student).ThenInclude(s => s.ClassLevel)
            .Include(i => i.AcademicTerm).ThenInclude(t => t.AcademicYear)
            .Where(i => i.Status != InvoiceStatus.Paid &&
                        i.TotalAmount > i.TotalPaid);

        if (termId.HasValue)
            query = query.Where(i => i.AcademicTermId == termId);
        else if (currentTerm != null)
            query = query.Where(i => i.AcademicTermId == currentTerm.Id);
        if (classId.HasValue)
            query = query.Where(i => i.Student.ClassLevelId == classId);
        if (studentId.HasValue)
            query = query.Where(i => i.StudentId == studentId);

        var invoices = await query
            .OrderBy(i => i.Student.ClassLevel!.OrderIndex)
            .ThenBy(i => i.Student.LastName).ToListAsync();

        await LoadCommonViewBags(termId ?? currentTerm?.Id, classId, studentId);
        ViewBag.TermId = termId ?? currentTerm?.Id;
        ViewBag.ClassId = classId;
        ViewBag.StudentId = studentId;
        ViewBag.TotalOutstanding = invoices.Sum(i => i.Balance);
        ViewBag.TotalInvoiced = invoices.Sum(i => i.TotalAmount);
        ViewBag.TotalPaid = invoices.Sum(i => i.TotalPaid);

        return View(invoices);
    }

    public async Task<IActionResult> DailyCollection(DateTime? date)
    {
        var reportDate = date ?? DateTime.Today;
        var payments = await _db.FeePayments
            .Include(p => p.Student).ThenInclude(s => s.ClassLevel)
            .Include(p => p.StudentInvoice)
            .Where(p => p.PaymentDate.Date == reportDate.Date &&
                        !p.IsReversed)
            .OrderBy(p => p.Student.LastName).ToListAsync();

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        ViewBag.ReportDate = reportDate;
        ViewBag.School = school;
        ViewBag.TotalCollected = payments.Sum(p => p.Amount);
        ViewBag.CashTotal = payments
            .Where(p => p.PaymentMethod == PaymentMethod.Cash)
            .Sum(p => p.Amount);
        ViewBag.MpesaTotal = payments
            .Where(p => p.PaymentMethod == PaymentMethod.MPesa)
            .Sum(p => p.Amount);
        ViewBag.BankTotal = payments
            .Where(p => p.PaymentMethod == PaymentMethod.BankTransfer)
            .Sum(p => p.Amount);

        return View(payments);
    }

    // ── ACADEMIC REPORTS ──────────────────────────────────────────

    public async Task<IActionResult> AttendanceSummary(
        int? classId, int? termId, int? studentId)
    {
        var currentTerm = await _db.AcademicTerms
            .FirstOrDefaultAsync(t => t.IsCurrent);
        var selectedTermId = termId ?? currentTerm?.Id;

        var query = _db.AttendanceRecords
            .Include(a => a.Student).ThenInclude(s => s.ClassLevel)
            .AsQueryable();

        if (selectedTermId.HasValue)
            query = query.Where(a => a.AcademicTermId == selectedTermId);
        if (classId.HasValue)
            query = query.Where(a => a.ClassLevelId == classId);
        if (studentId.HasValue)
            query = query.Where(a => a.StudentId == studentId);

        var records = await query.ToListAsync();

        var studentSummary = records.GroupBy(r => r.Student)
            .Select(g => new
            {
                Student = g.Key,
                Present = g.Count(r => r.Status == AttendanceStatus.Present),
                Absent = g.Count(r => r.Status == AttendanceStatus.Absent),
                Late = g.Count(r => r.Status == AttendanceStatus.Late),
                Excused = g.Count(r => r.Status == AttendanceStatus.Excused),
                Total = g.Count()
            })
            .OrderBy(s => s.Student.ClassLevel?.OrderIndex)
            .ThenBy(s => s.Student.LastName).ToList();

        await LoadCommonViewBags(selectedTermId, classId, studentId);
        ViewBag.StudentSummary = studentSummary;
        ViewBag.TermId = selectedTermId;
        ViewBag.ClassId = classId;
        ViewBag.StudentId = studentId;

        return View();
    }

    public async Task<IActionResult> CBCPerformance(
        int? classId, int? termId, int? studentId)
    {
        var currentTerm = await _db.AcademicTerms
            .FirstOrDefaultAsync(t => t.IsCurrent);
        var selectedTermId = termId ?? currentTerm?.Id;

        var query = _db.CBCAssessments
            .Include(a => a.Student).ThenInclude(s => s.ClassLevel)
            .Include(a => a.Subject)
            .Where(a => selectedTermId == null ||
                        a.AcademicTermId == selectedTermId)
            .Where(a => classId == null ||
                        a.ClassLevelId == classId);

        if (studentId.HasValue)
            query = query.Where(a => a.StudentId == studentId);

        var assessments = await query.ToListAsync();
        var subjects = assessments.Select(a => a.Subject)
            .Distinct().OrderBy(s => s.OrderIndex).ToList();

        var subjectSummary = subjects.Select(s => new
        {
            Subject = s,
            EE = assessments.Count(a => a.SubjectId == s.Id &&
                a.PerformanceLevel ==
                    Domain.Enums.PerformanceLevel.ExceedsExpectation),
            ME = assessments.Count(a => a.SubjectId == s.Id &&
                a.PerformanceLevel ==
                    Domain.Enums.PerformanceLevel.MeetExpectation),
            AE = assessments.Count(a => a.SubjectId == s.Id &&
                a.PerformanceLevel ==
                    Domain.Enums.PerformanceLevel.ApproachingExpectation),
            BE = assessments.Count(a => a.SubjectId == s.Id &&
                a.PerformanceLevel ==
                    Domain.Enums.PerformanceLevel.BelowExpectation),
            Total = assessments.Count(a => a.SubjectId == s.Id)
        }).ToList();

        await LoadCommonViewBags(selectedTermId, classId, studentId);
        ViewBag.SubjectSummary = subjectSummary;
        ViewBag.TermId = selectedTermId;
        ViewBag.ClassId = classId;
        ViewBag.StudentId = studentId;
        ViewBag.TotalStudents = assessments
            .Select(a => a.StudentId).Distinct().Count();
// ── CLASS PERFORMANCE TREND (last 6 terms) ──────────────────────
var trendTerms = await _db.AcademicTerms
    .Include(t => t.AcademicYear)
    .OrderByDescending(t => t.Id)   // assumes higher Id = more recent term
    .Take(6)
    .ToListAsync();
trendTerms.Reverse(); // chronological order, oldest → newest, for the chart

var trendTermIds = trendTerms.Select(t => t.Id).ToList();

var trendAssessments = await _db.CBCAssessments
    .Where(a => trendTermIds.Contains(a.AcademicTermId))
    .ToListAsync();

var trendClasses = await _db.ClassLevels
    .OrderBy(c => c.OrderIndex).ToListAsync();

// If a specific class is already selected in the filter, show only that
// class; otherwise show every class so they can be compared side by side.
if (classId.HasValue)
    trendClasses = trendClasses.Where(c => c.Id == classId).ToList();

var classTrendSeries = trendClasses.Select(cls => new
{
    ClassName = cls.Name,
    Values = trendTerms.Select(term =>
    {
        var termClassAssessments = trendAssessments
            .Where(a => a.AcademicTermId == term.Id && a.ClassLevelId == cls.Id)
            .ToList();
        if (!termClassAssessments.Any())
            return (double?)null;

        var meEeCount = termClassAssessments.Count(a =>
            a.PerformanceLevel == Domain.Enums.PerformanceLevel.MeetExpectation ||
            a.PerformanceLevel == Domain.Enums.PerformanceLevel.ExceedsExpectation);

        return (double?)Math.Round(
            (double)meEeCount / termClassAssessments.Count * 100, 1);
    }).ToList()
})
.Where(c => c.Values.Any(v => v.HasValue)) // drop classes with zero data
.ToList();

ViewBag.TrendTermLabels = trendTerms
    .Select(t => $"{t.AcademicYear?.Name} {t.Name}").ToList();
ViewBag.ClassTrendSeries = classTrendSeries;
        return View();
    }

    // ── STUDENT REPORTS ───────────────────────────────────────────

    public async Task<IActionResult> StudentEnrollment()
    {
        var students = await _db.Students
            .Include(s => s.ClassLevel)
            .Where(s => !s.IsDeleted).ToListAsync();

        var byClass = students.GroupBy(s => s.ClassLevel)
            .Select(g => new
            {
                Class = g.Key,
                Total = g.Count(),
                Boys = g.Count(s => s.Gender == Domain.Enums.Gender.Male),
                Girls = g.Count(s => s.Gender == Domain.Enums.Gender.Female),
                Active = g.Count(s =>
                    s.Status == Domain.Enums.StudentStatus.Active)
            })
            .OrderBy(g => g.Class?.OrderIndex).ToList();

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        ViewBag.ByClass = byClass;
        ViewBag.School = school;
        ViewBag.TotalStudents = students.Count;
        ViewBag.TotalBoys = students.Count(s =>
            s.Gender == Domain.Enums.Gender.Male);
        ViewBag.TotalGirls = students.Count(s =>
            s.Gender == Domain.Enums.Gender.Female);
        ViewBag.ActiveStudents = students.Count(s =>
            s.Status == Domain.Enums.StudentStatus.Active);

        return View();
    }

    public async Task<IActionResult> ClassList(int? classId)
    {
        var classes = await _db.ClassLevels
            .OrderBy(c => c.OrderIndex).ToListAsync();
        var selectedClass = classId.HasValue
            ? await _db.ClassLevels.FindAsync(classId)
            : classes.FirstOrDefault();

        var students = selectedClass != null
            ? await _db.Students
                .Include(s => s.ClassLevel)
                .Include(s => s.ClassStream)
                .Where(s => s.ClassLevelId == selectedClass.Id &&
                            s.Status == Domain.Enums.StudentStatus.Active)
                .OrderBy(s => s.LastName).ToListAsync()
            : new List<Domain.Entities.Students.Student>();

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        ViewBag.Classes = classes;
        ViewBag.SelectedClass = selectedClass;
        ViewBag.ClassId = classId ?? selectedClass?.Id;
        ViewBag.School = school;

        return View(students);
    }

    // ── STUDENT STATEMENT ─────────────────────────────────────────

    public async Task<IActionResult> StudentStatement(int? studentId)
    {
        var students = await _db.Students
            .Include(s => s.ClassLevel)
            .Where(s => s.Status == Domain.Enums.StudentStatus.Active)
            .OrderBy(s => s.LastName).ToListAsync();

        Domain.Entities.Students.Student? student = null;
        List<StudentInvoice> invoices = new();

        if (studentId.HasValue)
        {
            student = await _db.Students
                .Include(s => s.ClassLevel)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            invoices = await _db.StudentInvoices
                .Include(i => i.AcademicYear)
                .Include(i => i.AcademicTerm)
                .Include(i => i.Items).ThenInclude(item => item.FeeCategory)
                .Include(i => i.Payments)
                .Where(i => i.StudentId == studentId)
                .OrderByDescending(i => i.InvoiceDate).ToListAsync();
        }

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        ViewBag.Students = students;
        ViewBag.Student = student;
        ViewBag.School = school;
        ViewBag.StudentId = studentId;
        ViewBag.TotalInvoiced = invoices.Sum(i => i.TotalAmount);
        ViewBag.TotalPaid = invoices.Sum(i => i.TotalPaid);
        ViewBag.TotalBalance = invoices.Sum(i => i.Balance);
        ViewBag.Ledger = studentId.HasValue ? BuildStudentLedgerPublic(invoices) : new List<dynamic>();

        return View(invoices);
    }

    // ── AUDIT TRAIL ───────────────────────────────────────────────

    public async Task<IActionResult> AuditTrail(
        DateTime? fromDate, DateTime? toDate,
        string? userEmail, string? module)
    {
        var from = fromDate ?? DateTime.Today.AddDays(-7);
        var to = toDate ?? DateTime.Today;

        var query = _db.AuditLogs
            .Where(a => a.Timestamp.Date >= from.Date &&
                        a.Timestamp.Date <= to.Date);

        if (!string.IsNullOrEmpty(userEmail))
            query = query.Where(a => a.UserEmail.Contains(userEmail));
        if (!string.IsNullOrEmpty(module))
            query = query.Where(a => a.Module == module);

        var logs = await query
            .OrderByDescending(a => a.Timestamp).Take(500).ToListAsync();

        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.UserEmail = userEmail;
        ViewBag.Module = module;
        ViewBag.Modules = await _db.AuditLogs
            .Select(a => a.Module).Distinct().ToListAsync();

        return View(logs);
    }

    // ── LIBRARY REPORT ────────────────────────────────────────────

    public async Task<IActionResult> LibraryReport()
    {
        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        ViewBag.School = school;
        ViewBag.TotalBooks = await _db.Books.CountAsync();
        ViewBag.TotalCopies = await _db.Books.SumAsync(b => b.TotalCopies);
        ViewBag.AvailableCopies = await _db.Books
            .SumAsync(b => b.AvailableCopies);
        ViewBag.IssuedBooks = await _db.BookIssues.CountAsync(i =>
            i.Status == Domain.Entities.Library.BookIssueStatus.Issued);
        ViewBag.OverdueBooks = await _db.BookIssues.CountAsync(i =>
            i.Status == Domain.Entities.Library.BookIssueStatus.Issued &&
            i.DueDate < DateTime.Today);
        ViewBag.ReturnedThisMonth = await _db.BookIssues.CountAsync(i =>
            i.Status == Domain.Entities.Library.BookIssueStatus.Returned &&
            i.ReturnDate.HasValue &&
            i.ReturnDate.Value.Month == DateTime.Today.Month);
        ViewBag.BooksByCategory = await _db.BookCategories
            .Include(c => c.Books).OrderBy(c => c.OrderIndex).ToListAsync();
        ViewBag.RecentIssues = await _db.BookIssues
            .Include(i => i.Book).Include(i => i.Student)
            .OrderByDescending(i => i.IssueDate).Take(20).ToListAsync();

        return View();
    }

    // ── HELPER ────────────────────────────────────────────────────

    private async Task LoadCommonViewBags(
        int? termId, int? classId, int? studentId)
    {
        ViewBag.Terms = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .OrderByDescending(t => t.AcademicYear.Year).ToListAsync();
        ViewBag.Classes = await _db.ClassLevels
            .OrderBy(c => c.OrderIndex).ToListAsync();
        ViewBag.Students = await _db.Students
            .Include(s => s.ClassLevel)
            .Where(s => s.Status == Domain.Enums.StudentStatus.Active)
            .OrderBy(s => s.LastName).ToListAsync();
        ViewBag.School = await _db.SchoolProfiles.FirstOrDefaultAsync();
    }

    // ── HELPER: builds a chronological ledger with running balance ──
public static List<dynamic> BuildStudentLedgerPublic(List<StudentInvoice> invoices)
{
    var raw = new List<(DateTime Date, string Description, string Term, decimal Debit, decimal Credit)>();

    foreach (var inv in invoices)
    {
        var termLabel = $"{inv.AcademicYear?.Name} {inv.AcademicTerm?.Name}";
        raw.Add((inv.InvoiceDate, $"Invoice {inv.InvoiceNumber} issued", termLabel, inv.TotalAmount, 0));

        foreach (var p in inv.Payments)
        {
            raw.Add((p.PaymentDate,
                $"Payment received — {p.ReceiptNumber} ({p.PaymentMethod})",
                termLabel, 0, p.Amount));
        }
    }

    var ordered = raw.OrderBy(r => r.Date).ToList();

    decimal running = 0;
    var result = new List<dynamic>();
    foreach (var r in ordered)
    {
        running += r.Debit - r.Credit;
        result.Add(new
        {
            Date = r.Date,
            Description = r.Description,
            Term = r.Term,
            Debit = r.Debit,
            Credit = r.Credit,
            Balance = running
        });
    }
    return result;
}

    // GET: /Reports/ExportFeeCollection
    public async Task<IActionResult> ExportFeeCollection(
        DateTime? fromDate, DateTime? toDate, int? termId)
    {
        var from = fromDate ?? DateTime.Today.AddDays(-30);
        var to = toDate ?? DateTime.Today;

        var payments = await _db.FeePayments
            .Include(p => p.Student).ThenInclude(s => s.ClassLevel)
            .Include(p => p.StudentInvoice)
            .Where(p => p.PaymentDate.Date >= from.Date &&
                        p.PaymentDate.Date <= to.Date &&
                        !p.IsReversed)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Receipt No,Date,Student,Admission No,Class,Method,Reference,Amount");

        foreach (var p in payments)
        {
            csv.AppendLine($"{p.ReceiptNumber}," +
                $"{p.PaymentDate:dd/MM/yyyy}," +
                $"\"{p.Student.FullName}\"," +
                $"{p.Student.AdmissionNumber}," +
                $"{p.Student.ClassLevel?.Name ?? "-"}," +
                $"{p.PaymentMethod}," +
                $"{p.ReferenceNumber ?? "-"}," +
                $"{p.Amount}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv",
            $"FeeCollection_{from:yyyyMMdd}_{to:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> ExportStudentList(int? classId)
    {
        var query = _db.Students
            .Include(s => s.ClassLevel)
            .Include(s => s.ClassStream)
            .Where(s => s.Status == Domain.Enums.StudentStatus.Active);

        if (classId.HasValue)
            query = query.Where(s => s.ClassLevelId == classId);

        var students = await query.OrderBy(s => s.LastName).ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Adm No,Full Name,Class,Stream,Gender,Date of Birth,Father Phone,Mother Phone");

        foreach (var s in students)
        {
            csv.AppendLine($"{s.AdmissionNumber}," +
                $"\"{s.FullName}\"," +
                $"{s.ClassLevel?.Name ?? "-"}," +
                $"{s.ClassStream?.Name ?? "-"}," +
                $"{s.Gender}," +
                $"{s.DateOfBirth:dd/MM/yyyy}," +
                $"{s.FatherPhone ?? "-"}," +
                $"{s.MotherPhone ?? "-"}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"StudentList_{DateTime.Today:yyyyMMdd}.csv");
    }
    // ── PDF: FEE COLLECTION ──────────────────────────────────────
public async Task<IActionResult> FeeCollectionPdf(
    DateTime? fromDate, DateTime? toDate, int? termId, int? studentId,
    bool download = true)
{
    var from = fromDate ?? DateTime.Today.AddDays(-30);
    var to = toDate ?? DateTime.Today;

    var query = _db.FeePayments
        .Include(p => p.Student).ThenInclude(s => s.ClassLevel)
        .Include(p => p.StudentInvoice)
        .Where(p => p.PaymentDate.Date >= from.Date &&
                    p.PaymentDate.Date <= to.Date && !p.IsReversed);

    if (termId.HasValue)
        query = query.Where(p => p.StudentInvoice.AcademicTermId == termId);
    if (studentId.HasValue)
        query = query.Where(p => p.StudentId == studentId);

    var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
    var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

    var headers = new List<string> { "Receipt No", "Date", "Student", "Adm No", "Class", "Method", "Reference", "Amount (KES)" };
    var rows = payments.Select(p => new List<string>
    {
        p.ReceiptNumber,
        p.PaymentDate.ToString("dd MMM yyyy"),
        p.Student.FullName,
        p.Student.AdmissionNumber,
        p.Student.ClassLevel?.Name ?? "-",
        p.PaymentMethod.ToString(),
        p.ReferenceNumber ?? "-",
        p.Amount.ToString("N0")
    }).ToList();

    var stats = new List<(string, string)>
    {
        ("Total Collected", $"KES {payments.Sum(p => p.Amount):N0}"),
        ("Cash", $"KES {payments.Where(p => p.PaymentMethod == PaymentMethod.Cash).Sum(p => p.Amount):N0}"),
        ("M-Pesa", $"KES {payments.Where(p => p.PaymentMethod == PaymentMethod.MPesa).Sum(p => p.Amount):N0}"),
        ("Bank", $"KES {payments.Where(p => p.PaymentMethod == PaymentMethod.BankTransfer).Sum(p => p.Amount):N0}"),
    };

    var pdfBytes = _pdfService.GenerateTableReportPdf(
        "Fee Collection Report",
        $"Period: {from:dd MMM yyyy} — {to:dd MMM yyyy}",
        headers, rows, school, stats);

    var fileName = $"FeeCollection_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf";
    return download
        ? File(pdfBytes, "application/pdf", fileName)
        : File(pdfBytes, "application/pdf"); // inline preview, no filename forces browser viewer
}

// ── PDF: OUTSTANDING FEES ────────────────────────────────────
public async Task<IActionResult> OutstandingFeesPdf(
    int? termId, int? classId, int? studentId, bool download = true)
{
    var currentTerm = await _db.AcademicTerms.FirstOrDefaultAsync(t => t.IsCurrent);

    var query = _db.StudentInvoices
        .Include(i => i.Student).ThenInclude(s => s.ClassLevel)
        .Include(i => i.AcademicTerm).ThenInclude(t => t.AcademicYear)
        .Where(i => i.Status != InvoiceStatus.Paid && i.TotalAmount > i.TotalPaid);

    if (termId.HasValue)
        query = query.Where(i => i.AcademicTermId == termId);
    else if (currentTerm != null)
        query = query.Where(i => i.AcademicTermId == currentTerm.Id);
    if (classId.HasValue)
        query = query.Where(i => i.Student.ClassLevelId == classId);
    if (studentId.HasValue)
        query = query.Where(i => i.StudentId == studentId);

    var invoices = await query
        .OrderBy(i => i.Student.ClassLevel!.OrderIndex)
        .ThenBy(i => i.Student.LastName).ToListAsync();
    var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

    var headers = new List<string> { "Adm No", "Student", "Class", "Invoice No", "Invoiced", "Paid", "Balance" };
    var rows = invoices.Select(i => new List<string>
    {
        i.Student.AdmissionNumber,
        i.Student.FullName,
        i.Student.ClassLevel?.Name ?? "-",
        i.InvoiceNumber,
        i.TotalAmount.ToString("N0"),
        i.TotalPaid.ToString("N0"),
        i.Balance.ToString("N0")
    }).ToList();

    var stats = new List<(string, string)>
    {
        ("Total Outstanding", $"KES {invoices.Sum(i => i.Balance):N0}"),
        ("Total Invoiced", $"KES {invoices.Sum(i => i.TotalAmount):N0}"),
        ("Total Paid", $"KES {invoices.Sum(i => i.TotalPaid):N0}"),
    };

    var pdfBytes = _pdfService.GenerateTableReportPdf(
        "Outstanding Fees Report",
        $"As at {DateTime.Today:dd MMM yyyy}",
        headers, rows, school, stats);

    var fileName = $"OutstandingFees_{DateTime.Today:yyyyMMdd}.pdf";
    return download
        ? File(pdfBytes, "application/pdf", fileName)
        : File(pdfBytes, "application/pdf");
}

// ── PDF: DAILY COLLECTION ────────────────────────────────────
public async Task<IActionResult> DailyCollectionPdf(DateTime? date, bool download = true)
{
    var reportDate = date ?? DateTime.Today;
    var payments = await _db.FeePayments
        .Include(p => p.Student).ThenInclude(s => s.ClassLevel)
        .Include(p => p.StudentInvoice)
        .Where(p => p.PaymentDate.Date == reportDate.Date && !p.IsReversed)
        .OrderBy(p => p.Student.LastName).ToListAsync();
    var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

    var headers = new List<string> { "Receipt No", "Student", "Adm No", "Class", "Method", "Reference", "Amount (KES)" };
    var rows = payments.Select(p => new List<string>
    {
        p.ReceiptNumber,
        p.Student.FullName,
        p.Student.AdmissionNumber,
        p.Student.ClassLevel?.Name ?? "-",
        p.PaymentMethod.ToString(),
        p.ReferenceNumber ?? "-",
        p.Amount.ToString("N0")
    }).ToList();

    var stats = new List<(string, string)>
    {
        ("Total Collected", $"KES {payments.Sum(p => p.Amount):N0}"),
        ("Cash", $"KES {payments.Where(p => p.PaymentMethod == PaymentMethod.Cash).Sum(p => p.Amount):N0}"),
        ("M-Pesa", $"KES {payments.Where(p => p.PaymentMethod == PaymentMethod.MPesa).Sum(p => p.Amount):N0}"),
        ("Bank", $"KES {payments.Where(p => p.PaymentMethod == PaymentMethod.BankTransfer).Sum(p => p.Amount):N0}"),
    };

    var pdfBytes = _pdfService.GenerateTableReportPdf(
        "Daily Collection Report",
        $"Date: {reportDate:dd MMM yyyy}",
        headers, rows, school, stats);

    var fileName = $"DailyCollection_{reportDate:yyyyMMdd}.pdf";
    return download
        ? File(pdfBytes, "application/pdf", fileName)
        : File(pdfBytes, "application/pdf");
}
// ── PDF: ATTENDANCE SUMMARY ──────────────────────────────────
public async Task<IActionResult> AttendanceSummaryPdf(
    int? classId, int? termId, int? studentId, bool download = true)
{
    var currentTerm = await _db.AcademicTerms.FirstOrDefaultAsync(t => t.IsCurrent);
    var selectedTermId = termId ?? currentTerm?.Id;

    var query = _db.AttendanceRecords
        .Include(a => a.Student).ThenInclude(s => s.ClassLevel)
        .AsQueryable();

    if (selectedTermId.HasValue)
        query = query.Where(a => a.AcademicTermId == selectedTermId);
    if (classId.HasValue)
        query = query.Where(a => a.ClassLevelId == classId);
    if (studentId.HasValue)
        query = query.Where(a => a.StudentId == studentId);

    var records = await query.ToListAsync();

    var studentSummary = records.GroupBy(r => r.Student)
        .Select(g => new
        {
            Student = g.Key,
            Present = g.Count(r => r.Status == AttendanceStatus.Present),
            Absent = g.Count(r => r.Status == AttendanceStatus.Absent),
            Late = g.Count(r => r.Status == AttendanceStatus.Late),
            Excused = g.Count(r => r.Status == AttendanceStatus.Excused),
            Total = g.Count()
        })
        .OrderBy(s => s.Student.ClassLevel?.OrderIndex)
        .ThenBy(s => s.Student.LastName).ToList();

    var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

    var headers = new List<string> { "Adm No", "Student", "Class", "Present", "Absent", "Late", "Excused", "Total", "Rate %" };
    var rows = studentSummary.Select(s =>
    {
        var rate = s.Total > 0 ? Math.Round((double)s.Present / s.Total * 100, 1) : 0;
        return new List<string>
        {
            s.Student.AdmissionNumber,
            s.Student.FullName,
            s.Student.ClassLevel?.Name ?? "-",
            s.Present.ToString(),
            s.Absent.ToString(),
            s.Late.ToString(),
            s.Excused.ToString(),
            s.Total.ToString(),
            $"{rate}%"
        };
    }).ToList();

    var pdfBytes = _pdfService.GenerateTableReportPdf(
        "Attendance Summary Report",
        $"Generated: {DateTime.Now:dd MMM yyyy}",
        headers, rows, school);

    var fileName = $"AttendanceSummary_{DateTime.Today:yyyyMMdd}.pdf";
    return download
        ? File(pdfBytes, "application/pdf", fileName)
        : File(pdfBytes, "application/pdf");
}

// ── PDF: CBC PERFORMANCE ─────────────────────────────────────
public async Task<IActionResult> CBCPerformancePdf(
    int? classId, int? termId, int? studentId, bool download = true)
{
    var currentTerm = await _db.AcademicTerms.FirstOrDefaultAsync(t => t.IsCurrent);
    var selectedTermId = termId ?? currentTerm?.Id;

    var query = _db.CBCAssessments
        .Include(a => a.Student).ThenInclude(s => s.ClassLevel)
        .Include(a => a.Subject)
        .Where(a => selectedTermId == null || a.AcademicTermId == selectedTermId)
        .Where(a => classId == null || a.ClassLevelId == classId);

    if (studentId.HasValue)
        query = query.Where(a => a.StudentId == studentId);

    var assessments = await query.ToListAsync();
    var subjects = assessments.Select(a => a.Subject).Distinct()
        .OrderBy(s => s.OrderIndex).ToList();

    var subjectSummary = subjects.Select(s => new
    {
        Subject = s,
        EE = assessments.Count(a => a.SubjectId == s.Id && a.PerformanceLevel == Domain.Enums.PerformanceLevel.ExceedsExpectation),
        ME = assessments.Count(a => a.SubjectId == s.Id && a.PerformanceLevel == Domain.Enums.PerformanceLevel.MeetExpectation),
        AE = assessments.Count(a => a.SubjectId == s.Id && a.PerformanceLevel == Domain.Enums.PerformanceLevel.ApproachingExpectation),
        BE = assessments.Count(a => a.SubjectId == s.Id && a.PerformanceLevel == Domain.Enums.PerformanceLevel.BelowExpectation),
        Total = assessments.Count(a => a.SubjectId == s.Id)
    }).ToList();

    var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

    var headers = new List<string> { "Learning Area", "Total", "EE", "ME", "AE", "BE", "ME+EE %" };
    var rows = subjectSummary.Select(s =>
    {
        var rate = s.Total > 0 ? Math.Round((double)(s.ME + s.EE) / s.Total * 100, 1) : 0;
        return new List<string>
        {
            s.Subject.Name, s.Total.ToString(), s.EE.ToString(),
            s.ME.ToString(), s.AE.ToString(), s.BE.ToString(), $"{rate}%"
        };
    }).ToList();

    var stats = new List<(string, string)>
    {
        ("Students Assessed", assessments.Select(a => a.StudentId).Distinct().Count().ToString())
    };

    var pdfBytes = _pdfService.GenerateTableReportPdf(
        "CBC Performance Report",
        $"Generated: {DateTime.Now:dd MMM yyyy}",
        headers, rows, school, stats,
        "EE = Exceeds Expectation | ME = Meets Expectation | AE = Approaching Expectation | BE = Below Expectation");

    var fileName = $"CBCPerformance_{DateTime.Today:yyyyMMdd}.pdf";
    return download
        ? File(pdfBytes, "application/pdf", fileName)
        : File(pdfBytes, "application/pdf");
}

// ── PDF: STUDENT ENROLLMENT ──────────────────────────────────
public async Task<IActionResult> StudentEnrollmentPdf(bool download = true)
{
    var students = await _db.Students
        .Include(s => s.ClassLevel)
        .Where(s => !s.IsDeleted).ToListAsync();

    var byClass = students.GroupBy(s => s.ClassLevel)
        .Select(g => new
        {
            Class = g.Key,
            Total = g.Count(),
            Boys = g.Count(s => s.Gender == Domain.Enums.Gender.Male),
            Girls = g.Count(s => s.Gender == Domain.Enums.Gender.Female),
            Active = g.Count(s => s.Status == Domain.Enums.StudentStatus.Active)
        })
        .OrderBy(g => g.Class?.OrderIndex).ToList();

    var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

    var headers = new List<string> { "Class", "Total", "Boys", "Girls", "Active" };
    var rows = byClass.Select(c => new List<string>
    {
        c.Class?.Name ?? "No Class",
        c.Total.ToString(), c.Boys.ToString(), c.Girls.ToString(), c.Active.ToString()
    }).ToList();

    var stats = new List<(string, string)>
    {
        ("Total Students", students.Count.ToString()),
        ("Boys", students.Count(s => s.Gender == Domain.Enums.Gender.Male).ToString()),
        ("Girls", students.Count(s => s.Gender == Domain.Enums.Gender.Female).ToString()),
        ("Active", students.Count(s => s.Status == Domain.Enums.StudentStatus.Active).ToString()),
    };

    var pdfBytes = _pdfService.GenerateTableReportPdf(
        "Student Enrollment Report",
        $"Generated: {DateTime.Now:dd MMM yyyy}",
        headers, rows, school, stats);

    var fileName = $"StudentEnrollment_{DateTime.Today:yyyyMMdd}.pdf";
    return download
        ? File(pdfBytes, "application/pdf", fileName)
        : File(pdfBytes, "application/pdf");
}

// ── PDF: CLASS LIST ───────────────────────────────────────────
public async Task<IActionResult> ClassListPdf(int? classId, bool download = true)
{
    var classes = await _db.ClassLevels.OrderBy(c => c.OrderIndex).ToListAsync();
    var selectedClass = classId.HasValue
        ? await _db.ClassLevels.FindAsync(classId)
        : classes.FirstOrDefault();

    var students = selectedClass != null
        ? await _db.Students
            .Include(s => s.ClassLevel)
            .Include(s => s.ClassStream)
            .Where(s => s.ClassLevelId == selectedClass.Id &&
                        s.Status == Domain.Enums.StudentStatus.Active)
            .OrderBy(s => s.LastName).ToListAsync()
        : new List<Domain.Entities.Students.Student>();

    var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

    var headers = new List<string> { "Adm No", "Full Name", "Gender", "Stream", "Date of Birth", "Parent Phone" };
    var rows = students.Select(s => new List<string>
    {
        s.AdmissionNumber, s.FullName, s.Gender.ToString(),
        s.ClassStream?.Name ?? "-",
        s.DateOfBirth.ToString("dd MMM yyyy"),
        s.FatherPhone ?? s.MotherPhone ?? "-"
    }).ToList();

    var pdfBytes = _pdfService.GenerateTableReportPdf(
        $"Class List — {selectedClass?.Name ?? "N/A"}",
        $"Total: {students.Count} students",
        headers, rows, school);

    var fileName = $"ClassList_{selectedClass?.Name}_{DateTime.Today:yyyyMMdd}.pdf";
    return download
        ? File(pdfBytes, "application/pdf", fileName)
        : File(pdfBytes, "application/pdf");
}

// ── PDF: STUDENT STATEMENT ───────────────────────────────────
public async Task<IActionResult> StudentStatementPdf(int? studentId, bool download = true)
{
    if (!studentId.HasValue)
        return NotFound();

    var student = await _db.Students
        .Include(s => s.ClassLevel)
        .FirstOrDefaultAsync(s => s.Id == studentId);
    if (student == null) return NotFound();

    var invoices = await _db.StudentInvoices
        .Include(i => i.AcademicYear)
        .Include(i => i.AcademicTerm)
        .Include(i => i.Items).ThenInclude(item => item.FeeCategory)
        .Include(i => i.Payments)
        .Where(i => i.StudentId == studentId)
        .OrderByDescending(i => i.InvoiceDate).ToListAsync();

    var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

    // Ledger section (matches the on-screen statement)
var ledger = BuildStudentLedgerPublic(invoices);
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

// ── PDF: AUDIT TRAIL ──────────────────────────────────────────
public async Task<IActionResult> AuditTrailPdf(
    DateTime? fromDate, DateTime? toDate,
    string? userEmail, string? module, bool download = true)
{
    var from = fromDate ?? DateTime.Today.AddDays(-7);
    var to = toDate ?? DateTime.Today;

    var query = _db.AuditLogs
        .Where(a => a.Timestamp.Date >= from.Date && a.Timestamp.Date <= to.Date);

    if (!string.IsNullOrEmpty(userEmail))
        query = query.Where(a => a.UserEmail.Contains(userEmail));
    if (!string.IsNullOrEmpty(module))
        query = query.Where(a => a.Module == module);

    var logs = await query.OrderByDescending(a => a.Timestamp).Take(500).ToListAsync();
    var school = await _db.SchoolProfiles.FirstOrDefaultAsync();

    var headers = new List<string> { "Timestamp", "User", "Module", "Action", "Description", "Status" };
    var rows = logs.Select(log => new List<string>
    {
        log.Timestamp.ToString("dd MMM yyyy HH:mm"),
        log.UserEmail,
        log.Module,
        log.Action,
        log.Description ?? "-",
        log.IsSuccess ? "Success" : "Failed"
    }).ToList();

    var pdfBytes = _pdfService.GenerateTableReportPdf(
        "Audit Trail Report",
        $"Period: {from:dd MMM yyyy} — {to:dd MMM yyyy}",
        headers, rows, school);

    var fileName = $"AuditTrail_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf";
    return download
        ? File(pdfBytes, "application/pdf", fileName)
        : File(pdfBytes, "application/pdf");
}

// ── PDF: LIBRARY REPORT ──────────────────────────────────────
public async Task<IActionResult> LibraryReportPdf(bool download = true)
{
    var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
    var totalBooks = await _db.Books.CountAsync();
    var totalCopies = await _db.Books.SumAsync(b => b.TotalCopies);
    var availableCopies = await _db.Books.SumAsync(b => b.AvailableCopies);
    var issuedBooks = await _db.BookIssues.CountAsync(i =>
        i.Status == Domain.Entities.Library.BookIssueStatus.Issued);
    var overdueBooks = await _db.BookIssues.CountAsync(i =>
        i.Status == Domain.Entities.Library.BookIssueStatus.Issued &&
        i.DueDate < DateTime.Today);

    var booksByCategory = await _db.BookCategories
        .Include(c => c.Books).OrderBy(c => c.OrderIndex).ToListAsync();
    var recentIssues = await _db.BookIssues
        .Include(i => i.Book).Include(i => i.Student).Include(i => i.StaffPayroll)
        .OrderByDescending(i => i.IssueDate).Take(20).ToListAsync();

    var categoryHeaders = new List<string> { "Category", "Book Titles" };
    var categoryRows = booksByCategory.Select(c => new List<string>
    {
        c.Name, c.Books.Count.ToString()
    }).ToList();

    var issueHeaders = new List<string> { "Book", "Borrower", "Issue Date", "Due Date", "Status" };
var issueRows = recentIssues.Select(i => new List<string>
{
    i.Book?.Title ?? "-",
    i.BorrowerType == BorrowerType.Student ? (i.Student?.FullName ?? "-") :
    i.BorrowerType == BorrowerType.Staff ? (i.StaffPayroll?.FullName ?? i.StaffName ?? "-") :
    (i.VisitorName ?? "-"),
    i.IssueDate.ToString("dd MMM yyyy"),
    i.DueDate.ToString("dd MMM yyyy"),
    i.Status.ToString()
}).ToList();

    var stats = new List<(string, string)>
    {
        ("Book Titles", totalBooks.ToString()),
        ("Total Copies", totalCopies.ToString()),
        ("Available", availableCopies.ToString()),
        ("Issued", issuedBooks.ToString()),
        ("Overdue", overdueBooks.ToString()),
    };

    var sections = new List<(string, List<string>, List<List<string>>)>
    {
        ("Books by Category", categoryHeaders, categoryRows),
        ("Recent Book Issues", issueHeaders, issueRows)
    };

    var pdfBytes = _pdfService.GenerateMultiTableReportPdf(
        "Library Report",
        $"Generated: {DateTime.Now:dd MMM yyyy}",
        sections, school, stats);

    var fileName = $"LibraryReport_{DateTime.Today:yyyyMMdd}.pdf";
    return download
        ? File(pdfBytes, "application/pdf", fileName)
        : File(pdfBytes, "application/pdf");
}
}