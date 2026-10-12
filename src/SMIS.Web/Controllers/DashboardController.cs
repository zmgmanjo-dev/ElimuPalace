using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Academic;
using SMIS.Domain.Entities.Finance;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        // Parents and students belong in the portal, not the staff dashboard
    if (User.IsInRole("Parent") || User.IsInRole("Student"))
        return RedirectToAction("Dashboard", "Portal");
        
        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        var currentTerm = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .FirstOrDefaultAsync(t => t.IsCurrent);

        ViewBag.School = school;
        ViewBag.CurrentTerm = currentTerm;

        try
        {
            if (User.IsInRole("Administrator") || User.IsInRole("Principal")
                || User.IsInRole("DeputyPrincipal"))
            {
                ViewBag.DashboardRole = "Admin";
                await LoadAdminDashboard(currentTerm);
            }
            else if (User.IsInRole("Bursar") || User.IsInRole("AccountsClerk"))
            {
                ViewBag.DashboardRole = "Bursar";
                await LoadBursarDashboard(currentTerm);
            }
            else if (User.IsInRole("Librarian"))
            {
                ViewBag.DashboardRole = "Librarian";
                await LoadLibrarianDashboard();
            }
            else if (User.IsInRole("Teacher") || User.IsInRole("ClassTeacher")
                || User.IsInRole("AcademicMaster"))
            {
                ViewBag.DashboardRole = "Teacher";
                await LoadTeacherDashboard(currentTerm);
            }
            else
            {
                ViewBag.DashboardRole = "Generic";
            }
        }
        catch (Exception ex)
        {
            ViewBag.DashboardRole = "Generic";
            ViewBag.Error = ex.Message;
        }

        return View();
    }

    // ── ADMINISTRATOR / PRINCIPAL ──────────────────────────────────
    private async Task LoadAdminDashboard(Domain.Entities.School.AcademicTerm? currentTerm)
    {
        var totalStudents = await _db.Students
            .CountAsync(s => s.Status == Domain.Enums.StudentStatus.Active);
        var totalBoys = await _db.Students
            .CountAsync(s => s.Status == Domain.Enums.StudentStatus.Active &&
                s.Gender == Domain.Enums.Gender.Male);
        var totalGirls = await _db.Students
            .CountAsync(s => s.Status == Domain.Enums.StudentStatus.Active &&
                s.Gender == Domain.Enums.Gender.Female);

        var todayCollection = await _db.FeePayments
            .Where(p => p.PaymentDate.Date == DateTime.Today && !p.IsReversed)
            .SumAsync(p => p.Amount);

        var termCollection = currentTerm != null
            ? await _db.FeePayments
                .Include(p => p.StudentInvoice)
                .Where(p => p.StudentInvoice.AcademicTermId == currentTerm.Id && !p.IsReversed)
                .SumAsync(p => p.Amount)
            : 0;

        var termOutstanding = currentTerm != null
            ? await _db.StudentInvoices
                .Where(i => i.AcademicTermId == currentTerm.Id && i.Status != InvoiceStatus.Paid)
                .SumAsync(i => i.TotalAmount - i.TotalPaid)
            : 0;

        var todayPresent = await _db.AttendanceRecords
            .CountAsync(a => a.AttendanceDate.Date == DateTime.Today &&
                a.Status == AttendanceStatus.Present);
        var todayAbsent = await _db.AttendanceRecords
            .CountAsync(a => a.AttendanceDate.Date == DateTime.Today &&
                a.Status == AttendanceStatus.Absent);
        var attendanceRate = totalStudents > 0
            ? Math.Round((double)todayPresent / totalStudents * 100, 1) : 0;

        var sixMonthsAgo = DateTime.Today.AddMonths(-5);
        var monthlyCollection = await _db.FeePayments
            .Where(p => p.PaymentDate >= sixMonthsAgo && !p.IsReversed)
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(p => p.Amount) })
            .OrderBy(g => g.Year).ThenBy(g => g.Month).ToListAsync();

        var studentsByClass = await _db.Students
            .Include(s => s.ClassLevel)
            .Where(s => s.Status == Domain.Enums.StudentStatus.Active && s.ClassLevelId != null)
            .GroupBy(s => s.ClassLevel!.Name)
            .Select(g => new { Class = g.Key, Count = g.Count() })
            .OrderBy(g => g.Class).ToListAsync();

        var recentPayments = await _db.FeePayments
            .Include(p => p.Student)
            .OrderByDescending(p => p.PaymentDate).Take(5).ToListAsync();

        var recentStudents = await _db.Students
            .Include(s => s.ClassLevel)
            .OrderByDescending(s => s.CreatedAt).Take(5).ToListAsync();

        ViewBag.TotalStudents = totalStudents;
        ViewBag.TotalBoys = totalBoys;
        ViewBag.TotalGirls = totalGirls;
        ViewBag.TodayCollection = todayCollection;
        ViewBag.TermCollection = termCollection;
        ViewBag.TermOutstanding = termOutstanding;
        ViewBag.TodayPresent = todayPresent;
        ViewBag.TodayAbsent = todayAbsent;
        ViewBag.AttendanceRate = attendanceRate;
        ViewBag.MonthlyCollection = monthlyCollection;
        ViewBag.StudentsByClass = studentsByClass;
        ViewBag.RecentPayments = recentPayments;
        ViewBag.RecentStudents = recentStudents;
    }

    // ── TEACHER ────────────────────────────────────────────────────
    private async Task LoadTeacherDashboard(Domain.Entities.School.AcademicTerm? currentTerm)
    {
        var totalStudents = await _db.Students
            .CountAsync(s => s.Status == Domain.Enums.StudentStatus.Active);

        var todayPresent = await _db.AttendanceRecords
            .CountAsync(a => a.AttendanceDate.Date == DateTime.Today &&
                a.Status == AttendanceStatus.Present);
        var todayAbsent = await _db.AttendanceRecords
            .CountAsync(a => a.AttendanceDate.Date == DateTime.Today &&
                a.Status == AttendanceStatus.Absent);
        var attendanceTaken = await _db.AttendanceRecords
            .Select(a => a.ClassLevelId).Distinct()
            .CountAsync(c => _db.AttendanceRecords
                .Any(a => a.ClassLevelId == c && a.AttendanceDate.Date == DateTime.Today));

        var recentAssessments = await _db.CBCAssessments
            .Include(a => a.Student)
            .Include(a => a.Subject)
            .OrderByDescending(a => a.CreatedAt)
            .Take(8).ToListAsync();

        var performanceSummary = currentTerm != null
            ? await _db.CBCAssessments
                .Where(a => a.AcademicTermId == currentTerm.Id)
                .GroupBy(a => a.PerformanceLevel)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .ToListAsync()
            : new();

        ViewBag.TotalStudents = totalStudents;
        ViewBag.TodayPresent = todayPresent;
        ViewBag.TodayAbsent = todayAbsent;
        ViewBag.ClassesWithAttendanceToday = attendanceTaken;
        ViewBag.RecentAssessments = recentAssessments;
        ViewBag.PerformanceSummary = performanceSummary;
    }

    // ── BURSAR ─────────────────────────────────────────────────────
    private async Task LoadBursarDashboard(Domain.Entities.School.AcademicTerm? currentTerm)
    {
        var todayCollection = await _db.FeePayments
            .Where(p => p.PaymentDate.Date == DateTime.Today && !p.IsReversed)
            .SumAsync(p => p.Amount);

        var termCollection = currentTerm != null
            ? await _db.FeePayments
                .Include(p => p.StudentInvoice)
                .Where(p => p.StudentInvoice.AcademicTermId == currentTerm.Id && !p.IsReversed)
                .SumAsync(p => p.Amount)
            : 0;

        var termOutstanding = currentTerm != null
            ? await _db.StudentInvoices
                .Where(i => i.AcademicTermId == currentTerm.Id && i.Status != InvoiceStatus.Paid)
                .SumAsync(i => i.TotalAmount - i.TotalPaid)
            : 0;

        var unpaidInvoiceCount = currentTerm != null
            ? await _db.StudentInvoices
                .CountAsync(i => i.AcademicTermId == currentTerm.Id && i.Status != InvoiceStatus.Paid)
            : 0;

        var recentPayments = await _db.FeePayments
            .Include(p => p.Student)
            .OrderByDescending(p => p.PaymentDate).Take(8).ToListAsync();

        var cashAccounts = await _db.Accounts
            .Where(a => a.Category == SMIS.Domain.Entities.Accounting.AccountCategory.BankCash && a.IsActive)
            .ToListAsync();

        ViewBag.TodayCollection = todayCollection;
        ViewBag.TermCollection = termCollection;
        ViewBag.TermOutstanding = termOutstanding;
        ViewBag.UnpaidInvoiceCount = unpaidInvoiceCount;
        ViewBag.RecentPayments = recentPayments;
        ViewBag.CashAccountsCount = cashAccounts.Count;
    }

    // ── LIBRARIAN ──────────────────────────────────────────────────
    private async Task LoadLibrarianDashboard()
    {
        var totalBooks = await _db.Books.CountAsync();
        var totalCopies = await _db.Books.SumAsync(b => b.TotalCopies);
        var availableCopies = await _db.Books.SumAsync(b => b.AvailableCopies);
        var issuedBooks = await _db.BookIssues
            .CountAsync(i => i.Status == Domain.Entities.Library.BookIssueStatus.Issued);
        var overdueBooks = await _db.BookIssues
            .CountAsync(i => i.Status == Domain.Entities.Library.BookIssueStatus.Issued &&
                i.DueDate < DateTime.Today);

        var recentIssues = await _db.BookIssues
            .Include(i => i.Book).Include(i => i.Student)
            .OrderByDescending(i => i.IssueDate).Take(8).ToListAsync();

        ViewBag.TotalBooks = totalBooks;
        ViewBag.TotalCopies = totalCopies;
        ViewBag.AvailableCopies = availableCopies;
        ViewBag.IssuedBooks = issuedBooks;
        ViewBag.OverdueBooks = overdueBooks;
        ViewBag.RecentIssues = recentIssues;
    }
}