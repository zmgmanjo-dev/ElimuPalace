using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Accounting;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class PayrollController : BaseController
{
    private new readonly ApplicationDbContext _db;

    public PayrollController(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    // ── STAFF SETUP ───────────────────────────────────────────────

    public async Task<IActionResult> Staff()
    {
        var staff = await _db.StaffPayrolls
            .OrderBy(s => s.FullName).ToListAsync();
        return View(staff);
    }

    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateStaff(
    string fullName, string? email, string? nationalId, string? kraPin,
    string? nssfNumber, string? shaNumber,
    string? bankName, string? bankAccount,
    StaffPayrollType staffType,
    Domain.Enums.EmploymentType employmentType,
    decimal basicSalary, decimal houseAllowance,
    decimal transportAllowance, decimal medicalAllowance,
    decimal otherAllowances, decimal saccoDeduction,
    decimal loanDeduction, decimal otherDeductions)
{
    var staffNumber = await GetNextStaffNumber();

    var staff = new StaffPayroll
    {
        StaffNumber = staffNumber,
        FullName = fullName,
        Email = email,
        NationalId = nationalId,
        KRAPin = kraPin,
        NSSFNumber = nssfNumber,
        shaNumber = shaNumber,
        BankName = bankName,
        BankAccount = bankAccount,
        StaffType = staffType,
        EmploymentType = employmentType,
        BasicSalary = basicSalary,
        HouseAllowance = houseAllowance,
        TransportAllowance = transportAllowance,
        MedicalAllowance = medicalAllowance,
        OtherAllowances = otherAllowances,
        SACCODeduction = saccoDeduction,
        LoanDeduction = loanDeduction,
        OtherDeductions = otherDeductions,
        CreatedBy = User.Identity?.Name ?? "system",
        CreatedAt = DateTime.UtcNow
    };

    _db.StaffPayrolls.Add(staff);
    await _db.SaveChangesAsync();
    TempData["Success"] =
        $"Staff member '{fullName}' added! No: {staffNumber}";
    return RedirectToAction(nameof(Staff));
}
    // GET: /Payroll/StaffDetails/5
    public async Task<IActionResult> StaffDetails(int id)
    {
        var staff = await _db.StaffPayrolls.FindAsync(id);
        if (staff == null) return NotFound();

        var payrollHistory = await _db.PayrollRunLines
            .Include(l => l.PayrollRun)
            .Where(l => l.StaffPayrollId == id)
            .OrderByDescending(l => l.PayrollRun.Year)
            .ThenByDescending(l => l.PayrollRun.Month)
            .ToListAsync();

        ViewBag.PayrollHistory = payrollHistory;
        return View(staff);
    }

    // GET: /Payroll/EditStaff/5
    public async Task<IActionResult> EditStaff(int id)
    {
        var staff = await _db.StaffPayrolls.FindAsync(id);
        if (staff == null) return NotFound();
        return View(staff);
    }

    // POST: /Payroll/EditStaff/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStaff(int id, string fullName,
        string? email, string? nationalId, string? kraPin,
        string? nssfNumber, string? shaNumber,
        string? bankName, string? bankAccount,
        StaffPayrollType staffType,
        Domain.Enums.EmploymentType employmentType,
        decimal basicSalary, decimal houseAllowance,
        decimal transportAllowance, decimal medicalAllowance,
        decimal otherAllowances, decimal saccoDeduction,
        decimal loanDeduction, decimal otherDeductions)
    {
        var staff = await _db.StaffPayrolls.FindAsync(id);
        if (staff == null) return NotFound();

        staff.FullName = fullName;
        staff.Email = email;
        staff.NationalId = nationalId;
        staff.KRAPin = kraPin;
        staff.NSSFNumber = nssfNumber;
        staff.shaNumber = shaNumber;
        staff.BankName = bankName;
        staff.BankAccount = bankAccount;
        staff.StaffType = staffType;
        staff.EmploymentType = employmentType;
        staff.BasicSalary = basicSalary;
        staff.HouseAllowance = houseAllowance;
        staff.TransportAllowance = transportAllowance;
        staff.MedicalAllowance = medicalAllowance;
        staff.OtherAllowances = otherAllowances;
        staff.SACCODeduction = saccoDeduction;
        staff.LoanDeduction = loanDeduction;
        staff.OtherDeductions = otherDeductions;
        staff.UpdatedBy = User.Identity?.Name ?? "system";
        staff.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Staff member '{fullName}' updated successfully!";
        await LogAudit("Payroll", "EditStaff", $"Updated staff details for {fullName}");
        return RedirectToAction(nameof(Staff));
    }

    // POST: /Payroll/ToggleStaffStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStaffStatus(int id)
    {
        var staff = await _db.StaffPayrolls.FindAsync(id);
        if (staff == null) return NotFound();

        staff.IsActive = !staff.IsActive;
        staff.UpdatedBy = User.Identity?.Name ?? "system";
        staff.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var statusText = staff.IsActive ? "reactivated" : "deactivated";
        TempData["Success"] = $"{staff.FullName} has been {statusText}!";
        await LogAudit("Payroll", "ToggleStaffStatus",
            $"{staff.FullName} was {statusText}");
        return RedirectToAction(nameof(Staff));
    }

    // ── PAYROLL RUNS ──────────────────────────────────────────────

    public async Task<IActionResult> Runs()
    {
        var runs = await _db.PayrollRuns
            .Include(r => r.Lines)
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Month)
            .ToListAsync();
        return View(runs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GeneratePayroll(int month, int year)
    {
        // Check if already exists
        if (await _db.PayrollRuns.AnyAsync(r =>
            r.Month == month && r.Year == year))
        {
            TempData["Error"] =
                $"Payroll for {new DateTime(year, month, 1):MMMM yyyy} already exists!";
            return RedirectToAction(nameof(Runs));
        }

        var activeStaff = await _db.StaffPayrolls
            .Where(s => s.IsActive).ToListAsync();

        if (!activeStaff.Any())
        {
            TempData["Error"] = "No active staff found!";
            return RedirectToAction(nameof(Runs));
        }

        var run = new PayrollRun
{
    RunNumber = await GetNextPayrollRunNumber(year, month),
            Month = month,
            Year = year,
            Status = PayrollRunStatus.Draft,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        var lines = new List<PayrollRunLine>();

        foreach (var staff in activeStaff)
        {
            var gross = staff.GrossSalary;

            // Kenya PAYE calculation (simplified 2024 rates)
            var paye = CalculatePAYE(gross);

            // NSSF (6% employee, 6% employer — capped at KES 2,160)
            var nssfEmployee = Math.Min(gross * 0.06m, 2160m);
            var nssfEmployer = Math.Min(gross * 0.06m, 2160m);

            // SHA (Social Health Authority — 2.75% of gross, no cap)
            var shaEmployee = Math.Round(gross * 0.0275m, 2);
            var shaEmployer = Math.Round(gross * 0.0275m, 2);

            lines.Add(new PayrollRunLine
            {
                StaffPayrollId = staff.Id,
                BasicSalary = staff.BasicSalary,
                HouseAllowance = staff.HouseAllowance,
                TransportAllowance = staff.TransportAllowance,
                MedicalAllowance = staff.MedicalAllowance,
                OtherAllowances = staff.OtherAllowances,
                GrossSalary = gross,
                PAYE = paye,
                NSSFEmployee = nssfEmployee,
                NSSFEmployer = nssfEmployer,
                SHAEmployee = shaEmployee,
                SHAEmployer = shaEmployer,
                SACCODeduction = staff.SACCODeduction,
                LoanDeduction = staff.LoanDeduction,
                OtherDeductions = staff.OtherDeductions
            });
        }
        run.Lines = lines;
        run.TotalGross = lines.Sum(l => l.GrossSalary);
        run.TotalPAYE = lines.Sum(l => l.PAYE);
        run.TotalNSSF = lines.Sum(l => l.NSSFEmployee);
        run.TotalSHA = lines.Sum(l => l.SHAEmployee);
        run.TotalDeductions = lines.Sum(l =>
            l.PAYE + l.NSSFEmployee + l.SHAEmployee +
            l.SACCODeduction + l.LoanDeduction + l.OtherDeductions);
        run.TotalNetPay = lines.Sum(l =>
            l.GrossSalary - (l.PAYE + l.NSSFEmployee +
            l.SHAEmployee + l.SACCODeduction +
            l.LoanDeduction + l.OtherDeductions));

        _db.PayrollRuns.Add(run);
        await _db.SaveChangesAsync();

        TempData["Success"] =
            $"Payroll for {new DateTime(year, month, 1):MMMM yyyy} " +
            $"generated for {activeStaff.Count} staff!";
        return RedirectToAction(nameof(PayrollDetails),
            new { id = run.Id });
    }

    public async Task<IActionResult> PayrollDetails(int id)
    {
        var run = await _db.PayrollRuns
            .Include(r => r.Lines)
                .ThenInclude(l => l.StaffPayroll)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (run == null) return NotFound();

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        ViewBag.School = school;
        ViewBag.CashAccounts = await _db.Accounts
    .Where(a => a.Category == AccountCategory.BankCash && a.IsActive)
    .OrderBy(a => a.Code).ToListAsync();
        ViewBag.ReportTitle =
            $"Payroll — {new DateTime(run.Year, run.Month, 1):MMMM yyyy}";

        return View(run);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApprovePayroll(int id)
    {
        var run = await _db.PayrollRuns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (run == null) return NotFound();

        // Create journal entry: Dr Salary Expense / Cr Salaries Payable
        var salaryExpAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == "5101");
        var salariesPayable = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == "2102");
        var payePayable = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == "2103");
        var shaPayable = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == "2104");
        var nssfPayable = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == "2105");

        if (salaryExpAccount != null && salariesPayable != null)
        {
            var journalEntry = new JournalEntry
            {
                EntryNumber = await GetNextJournalEntryNumber(DateTime.Now.Year),
                EntryDate = DateTime.Today,
                Description =
                    $"Payroll — {new DateTime(run.Year, run.Month, 1):MMMM yyyy}",
                EntryType = JournalEntryType.Payroll,
                Status = JournalEntryStatus.Posted,
                PostedBy = User.Identity?.Name,
                PostedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow,
                Lines = new List<JournalEntryLine>
                {
                    new JournalEntryLine
                    {
                        AccountId = salaryExpAccount.Id,
                        DebitAmount = run.TotalGross,
                        CreditAmount = 0,
                        Narration = "Gross salaries",
                        LineNumber = 1
                    },
                    new JournalEntryLine
                    {
                        AccountId = salariesPayable.Id,
                        DebitAmount = 0,
                        CreditAmount = run.TotalNetPay,
                        Narration = "Net pay to staff",
                        LineNumber = 2
                    }
                }
            };

            if (payePayable != null && run.TotalPAYE > 0)
                journalEntry.Lines.Add(new JournalEntryLine
                {
                    AccountId = payePayable.Id,
                    DebitAmount = 0,
                    CreditAmount = run.TotalPAYE,
                    Narration = "PAYE",
                    LineNumber = 3
                });

            if (shaPayable != null && run.TotalSHA > 0)
                journalEntry.Lines.Add(new JournalEntryLine
                {
                    AccountId = shaPayable.Id,
                    DebitAmount = 0,
                    CreditAmount = run.TotalSHA,
                    Narration = "SHA",
                    LineNumber = 4
                });

            if (nssfPayable != null && run.TotalNSSF > 0)
                journalEntry.Lines.Add(new JournalEntryLine
                {
                    AccountId = nssfPayable.Id,
                    DebitAmount = 0,
                    CreditAmount = run.TotalNSSF,
                    Narration = "NSSF",
                    LineNumber = 5
                });

var otherDeductionsPayable = await _db.Accounts
    .FirstOrDefaultAsync(a => a.Code == "2107");

var otherDeductionsTotal = run.Lines.Sum(l =>
    l.SACCODeduction + l.LoanDeduction + l.OtherDeductions);

if (otherDeductionsPayable != null && otherDeductionsTotal > 0)
    journalEntry.Lines.Add(new JournalEntryLine
    {
        AccountId = otherDeductionsPayable.Id,
        DebitAmount = 0,
        CreditAmount = otherDeductionsTotal,
        Narration = "SACCO/Loan/Other deductions",
        LineNumber = 6
    });
    var totalDebits = journalEntry.Lines.Sum(l => l.DebitAmount);
var totalCredits = journalEntry.Lines.Sum(l => l.CreditAmount);
if (totalDebits != totalCredits)
{
    TempData["Error"] =
        $"Cannot approve — payroll journal entry is unbalanced! " +
        $"Debits: {totalDebits:N2}, Credits: {totalCredits:N2}";
    return RedirectToAction(nameof(PayrollDetails), new { id });
}
            _db.JournalEntries.Add(journalEntry);
            await _db.SaveChangesAsync();
            run.JournalEntryId = journalEntry.Id;
        }

        run.Status = PayrollRunStatus.Approved;
        run.ApprovedBy = User.Identity?.Name;
        run.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Payroll approved and journal entry posted!";
        await LogAudit("Payroll", "ApprovePayroll",
    $"Approved payroll {run.RunNumber} — Net Pay KES {run.TotalNetPay:N0}");
        return RedirectToAction(nameof(PayrollDetails), new { id });
    }
    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ReverseApproval(int id)
{
    var run = await _db.PayrollRuns.FindAsync(id);
    if (run == null) return NotFound();

    if (run.Status == PayrollRunStatus.Paid)
    {
        TempData["Error"] = "Cannot reverse — salaries have already been paid out!";
        return RedirectToAction(nameof(PayrollDetails), new { id });
    }

    if (run.Status != PayrollRunStatus.Approved)
    {
        TempData["Error"] = "Only Approved runs can be reversed!";
        return RedirectToAction(nameof(PayrollDetails), new { id });
    }

    if (run.JournalEntryId.HasValue)
    {
        var journalEntry = await _db.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == run.JournalEntryId);
        if (journalEntry != null)
        {
            _db.JournalEntryLines.RemoveRange(journalEntry.Lines);
            _db.JournalEntries.Remove(journalEntry);
        }
    }

    run.JournalEntryId = null;
    run.Status = PayrollRunStatus.Draft;
    run.ApprovedBy = null;
    run.ApprovedAt = null;
    run.UpdatedBy = User.Identity?.Name ?? "system";
    run.UpdatedAt = DateTime.UtcNow;

    await _db.SaveChangesAsync();
    TempData["Success"] = "Approval reversed. Click 'Approve & Post' again to regenerate a correct journal entry.";
    await LogAudit("Payroll", "ReverseApproval", $"Reversed approval for payroll {run.RunNumber}");
    return RedirectToAction(nameof(PayrollDetails), new { id });
}
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PaySalaries(int runId, int cashAccountId)
{
    var run = await _db.PayrollRuns.FindAsync(runId);
    if (run == null) return NotFound();

    if (run.Status != PayrollRunStatus.Approved)
    {
        TempData["Error"] = "Only Approved payroll runs can be paid!";
        return RedirectToAction(nameof(PayrollDetails), new { id = runId });
    }

    var cashAccount = await _db.Accounts.FindAsync(cashAccountId);
    var salariesPayable = await _db.Accounts.FirstOrDefaultAsync(a => a.Code == "2102");

    if (cashAccount == null || salariesPayable == null)
    {
        TempData["Error"] = "Required accounts not found!";
        return RedirectToAction(nameof(PayrollDetails), new { id = runId });
    }

    var monthName = new DateTime(run.Year, run.Month, 1).ToString("MMMM yyyy");

    var journalEntry = new JournalEntry
    {
        EntryNumber = await GetNextJournalEntryNumber(DateTime.Now.Year),
        EntryDate = DateTime.Today,
        Description = $"Salary Payment — {monthName}",
        EntryType = JournalEntryType.Payroll,
        Status = JournalEntryStatus.Posted,
        PostedBy = User.Identity?.Name,
        PostedAt = DateTime.UtcNow,
        CreatedBy = User.Identity?.Name ?? "system",
        CreatedAt = DateTime.UtcNow,
        Lines = new List<JournalEntryLine>
        {
            new() { AccountId = salariesPayable.Id, DebitAmount = run.TotalNetPay, CreditAmount = 0,
                Narration = $"Salary payment — {monthName}", LineNumber = 1 },
            new() { AccountId = cashAccountId, DebitAmount = 0, CreditAmount = run.TotalNetPay,
                Narration = $"Salary payment — {monthName}", LineNumber = 2 }
        }
    };
    _db.JournalEntries.Add(journalEntry);
    await _db.SaveChangesAsync();

    _db.CashbookEntries.Add(new CashbookEntry
    {
        EntryNumber = await GetNextCashbookEntryNumber(DateTime.Now.Year),
        EntryDate = DateTime.Today,
        EntryType = CashbookEntryType.Payment,
        CashAccountId = cashAccountId,
        ContraAccountId = salariesPayable.Id,
        Amount = run.TotalNetPay,
        Description = $"Salary Payment — {monthName}",
        PayeePayer = "Staff Payroll",
        JournalEntryId = journalEntry.Id,
        CreatedBy = User.Identity?.Name ?? "system",
        CreatedAt = DateTime.UtcNow
    });

    run.Status = PayrollRunStatus.Paid;
    run.UpdatedBy = User.Identity?.Name ?? "system";
    run.UpdatedAt = DateTime.UtcNow;

    await _db.SaveChangesAsync();

    TempData["Success"] = $"Salaries of KES {run.TotalNetPay:N0} paid from {cashAccount.Name}!";
    await LogAudit("Payroll", "PaySalaries",
    $"Paid KES {run.TotalNetPay:N0} salaries for {run.RunNumber} from {cashAccount.Name}");
    return RedirectToAction(nameof(PayrollDetails), new { id = runId });
}
        // ── PAYSLIPS ──────────────────────────────────────────────────

    public async Task<IActionResult> DownloadPayslip(int lineId)
    {
        var line = await _db.PayrollRunLines
            .Include(l => l.StaffPayroll)
            .Include(l => l.PayrollRun)
            .FirstOrDefaultAsync(l => l.Id == lineId);

        if (line == null) return NotFound();

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        var pdfService = HttpContext.RequestServices
            .GetRequiredService<SMIS.Web.Services.PdfService>();

        var pdf = pdfService.GeneratePayslipPdf(
            line, line.PayrollRun.Month, line.PayrollRun.Year, school);

        var monthName = new DateTime(line.PayrollRun.Year, line.PayrollRun.Month, 1)
            .ToString("MMMyyyy");

        return File(pdf, "application/pdf",
            $"Payslip_{line.StaffPayroll.StaffNumber}_{monthName}.pdf");
    }

    public async Task<IActionResult> DownloadBulkPayslips(int runId)
    {
        var run = await _db.PayrollRuns
            .Include(r => r.Lines).ThenInclude(l => l.StaffPayroll)
            .FirstOrDefaultAsync(r => r.Id == runId);

        if (run == null) return NotFound();

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        var pdfService = HttpContext.RequestServices
            .GetRequiredService<SMIS.Web.Services.PdfService>();

        using var outputStream = new MemoryStream();
        foreach (var line in run.Lines)
        {
            var pdf = pdfService.GeneratePayslipPdf(
                line, run.Month, run.Year, school);
            outputStream.Write(pdf, 0, pdf.Length);
        }

        var monthName = new DateTime(run.Year, run.Month, 1).ToString("MMMyyyy");
        return File(outputStream.ToArray(), "application/pdf",
            $"Payslips_{monthName}.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmailPayslip(int lineId)
    {
        var line = await _db.PayrollRunLines
            .Include(l => l.StaffPayroll)
            .Include(l => l.PayrollRun)
            .FirstOrDefaultAsync(l => l.Id == lineId);

        if (line == null) return NotFound();

        if (string.IsNullOrEmpty(line.StaffPayroll.Email))
        {
            TempData["Error"] =
                $"{line.StaffPayroll.FullName} has no email address on file!";
            return RedirectToAction(nameof(PayrollDetails),
                new { id = line.PayrollRunId });
        }

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        var pdfService = HttpContext.RequestServices
            .GetRequiredService<SMIS.Web.Services.PdfService>();
        var emailService = HttpContext.RequestServices
            .GetRequiredService<SMIS.Web.Services.EmailService>();

        var pdf = pdfService.GeneratePayslipPdf(
            line, line.PayrollRun.Month, line.PayrollRun.Year, school);

        var monthName = new DateTime(line.PayrollRun.Year, line.PayrollRun.Month, 1)
            .ToString("MMMM yyyy");

        try
        {
            await emailService.SendEmailWithAttachmentAsync(
                line.StaffPayroll.Email, line.StaffPayroll.FullName,
                $"Your Payslip — {monthName}",
                $"<p>Dear {line.StaffPayroll.FullName},</p>" +
                $"<p>Please find attached your payslip for <strong>{monthName}</strong>.</p>" +
                $"<p>Net Pay: <strong>KES {line.NetPay:N2}</strong></p>" +
                $"<p>Regards,<br/>{school?.Name ?? "School Administration"}</p>",
                pdf, $"Payslip_{monthName.Replace(" ", "")}.pdf");

            TempData["Success"] =
                $"Payslip emailed to {line.StaffPayroll.FullName}!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to send email: {ex.Message}";
        }

        return RedirectToAction(nameof(PayrollDetails),
            new { id = line.PayrollRunId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmailAllPayslips(int runId)
    {
        var run = await _db.PayrollRuns
            .Include(r => r.Lines).ThenInclude(l => l.StaffPayroll)
            .FirstOrDefaultAsync(r => r.Id == runId);

        if (run == null) return NotFound();

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        var pdfService = HttpContext.RequestServices
            .GetRequiredService<SMIS.Web.Services.PdfService>();
        var emailService = HttpContext.RequestServices
            .GetRequiredService<SMIS.Web.Services.EmailService>();

        var monthName = new DateTime(run.Year, run.Month, 1).ToString("MMMM yyyy");
        int sent = 0, skipped = 0, failed = 0;

        foreach (var line in run.Lines)
        {
            if (string.IsNullOrEmpty(line.StaffPayroll.Email))
            {
                skipped++;
                continue;
            }

            try
            {
                var pdf = pdfService.GeneratePayslipPdf(line, run.Month, run.Year, school);
                await emailService.SendEmailWithAttachmentAsync(
                    line.StaffPayroll.Email, line.StaffPayroll.FullName,
                    $"Your Payslip — {monthName}",
                    $"<p>Dear {line.StaffPayroll.FullName},</p>" +
                    $"<p>Please find attached your payslip for <strong>{monthName}</strong>.</p>" +
                    $"<p>Net Pay: <strong>KES {line.NetPay:N2}</strong></p>" +
                    $"<p>Regards,<br/>{school?.Name ?? "School Administration"}</p>",
                    pdf, $"Payslip_{monthName.Replace(" ", "")}.pdf");
                sent++;
            }
            catch
            {
                failed++;
            }
        }

        TempData["Success"] =
            $"Payslips sent: {sent}. Skipped (no email): {skipped}. Failed: {failed}.";
        return RedirectToAction(nameof(PayrollDetails), new { id = runId });
    }

    // ── KENYA TAX HELPERS ─────────────────────────────────────────

    private static decimal CalculatePAYE(decimal grossMonthly)
    {
        // Kenya 2024 PAYE bands (monthly)
        decimal tax = 0;
        decimal taxable = grossMonthly;

        if (taxable <= 24000) tax = taxable * 0.10m;
        else if (taxable <= 32333)
            tax = 2400 + (taxable - 24000) * 0.25m;
        else if (taxable <= 500000)
            tax = 4483 + (taxable - 32333) * 0.30m;
        else if (taxable <= 800000)
            tax = 144603 + (taxable - 500000) * 0.325m;
        else
            tax = 242103 + (taxable - 800000) * 0.35m;

        // Personal relief KES 2,400/month
        tax = Math.Max(0, tax - 2400);
        return Math.Round(tax, 2);
    }

    private static decimal CalculateSHA(decimal grossMonthly)
    {
        // SHA (Social Health Authority) — 2.75% of gross salary, no cap
        return Math.Round(grossMonthly * 0.0275m, 2);
    }
    private async Task<string> GetNextStaffNumber()
{
    var existing = await _db.StaffPayrolls.Select(s => s.StaffNumber).ToListAsync();
    int maxSeq = existing.Select(c => int.TryParse(c.Replace("STAFF", ""), out var s) ? s : 0)
        .DefaultIfEmpty(0).Max();
    return $"STAFF{(maxSeq + 1):D4}";
}

private async Task<string> GetNextPayrollRunNumber(int year, int month)
{
    var prefix = $"PAY/{year}/{month:D2}/";
    var existing = await _db.PayrollRuns
        .Where(r => r.RunNumber.StartsWith(prefix))
        .Select(r => r.RunNumber).ToListAsync();
    int maxSeq = existing.Select(n => int.TryParse(n.Split('/').Last(), out var s) ? s : 0)
        .DefaultIfEmpty(0).Max();
    return $"{prefix}{(maxSeq + 1):D3}";
}

private async Task<string> GetNextJournalEntryNumber(int year)
{
    var prefix = $"JV/{year}/";
    var existing = await _db.JournalEntries
        .Where(j => j.EntryNumber.StartsWith(prefix))
        .Select(j => j.EntryNumber).ToListAsync();
    int maxSeq = existing.Select(n => int.TryParse(n.Split('/').Last(), out var s) ? s : 0)
        .DefaultIfEmpty(0).Max();
    return $"{prefix}{(maxSeq + 1):D5}";
}
private async Task<string> GetNextCashbookEntryNumber(int year)
{
    var prefix = $"CB/{year}/";
    var existing = await _db.CashbookEntries
        .Where(c => c.EntryNumber.StartsWith(prefix))
        .Select(c => c.EntryNumber).ToListAsync();
    int maxSeq = existing.Select(n => int.TryParse(n.Split('/').Last(), out var s) ? s : 0)
        .DefaultIfEmpty(0).Max();
    return $"{prefix}{(maxSeq + 1):D5}";
}
}