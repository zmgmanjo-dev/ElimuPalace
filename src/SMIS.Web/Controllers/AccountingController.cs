using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Accounting;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class AccountingController : BaseController
{
    private new readonly ApplicationDbContext _db;

    public AccountingController(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    // ── DASHBOARD ────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var totalAccounts = await _db.Accounts.CountAsync();
        var totalJournalEntries = await _db.JournalEntries.CountAsync();
        var postedEntries = await _db.JournalEntries
            .CountAsync(j => j.Status == JournalEntryStatus.Posted);
        var draftEntries = await _db.JournalEntries
            .CountAsync(j => j.Status == JournalEntryStatus.Draft);

        var recentEntries = await _db.JournalEntries
            .Include(j => j.Lines)
            .OrderByDescending(j => j.EntryDate)
            .Take(10).ToListAsync();

        ViewBag.TotalAccounts = totalAccounts;
        ViewBag.TotalJournalEntries = totalJournalEntries;
        ViewBag.PostedEntries = postedEntries;
        ViewBag.DraftEntries = draftEntries;
        ViewBag.RecentEntries = recentEntries;

        return View();
    }

    // ── CHART OF ACCOUNTS ─────────────────────────────────────────

    public async Task<IActionResult> ChartOfAccounts()
    {
        var accounts = await _db.Accounts
            .Include(a => a.ParentAccount)
            .Include(a => a.SubAccounts)
            .OrderBy(a => a.Code).ToListAsync();
        return View(accounts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccount(
        string code, string name, AccountType accountType,
        AccountCategory category, int? parentAccountId,
        bool allowDirectEntry, string? description,
        decimal openingBalance)
    {
        if (await _db.Accounts.AnyAsync(a => a.Code == code))
        {
            TempData["Error"] = $"Account code '{code}' already exists!";
            return RedirectToAction(nameof(ChartOfAccounts));
        }

        var normalBalance = accountType == AccountType.Asset ||
                            accountType == AccountType.Expense
            ? BalanceSide.Debit : BalanceSide.Credit;

        var account = new Account
        {
            Code = code,
            Name = name,
            AccountType = accountType,
            Category = category,
            ParentAccountId = parentAccountId,
            AllowDirectEntry = allowDirectEntry,
            Description = description,
            OpeningBalance = openingBalance,
            NormalBalance = normalBalance,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Account '{code} - {name}' created!";
        await LogAudit("Accounting", "CreateAccount", $"Created account '{code} - {name}'");
        return RedirectToAction(nameof(ChartOfAccounts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedDefaultAccounts()
    {
        if (await _db.Accounts.AnyAsync())
        {
            TempData["Error"] = "Accounts already exist!";
            return RedirectToAction(nameof(ChartOfAccounts));
        }

        var accounts = new List<Account>
        {
            new Account { Code = "1000", Name = "ASSETS",
                AccountType = AccountType.Asset,
                Category = AccountCategory.CurrentAsset,
                AllowDirectEntry = false,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1100", Name = "Current Assets",
                AccountType = AccountType.Asset,
                Category = AccountCategory.CurrentAsset,
                AllowDirectEntry = false,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1101", Name = "Cash in Hand",
                AccountType = AccountType.Asset,
                Category = AccountCategory.BankCash,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1102", Name = "Cash at Bank",
                AccountType = AccountType.Asset,
                Category = AccountCategory.BankCash,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1103", Name = "M-Pesa Account",
                AccountType = AccountType.Asset,
                Category = AccountCategory.BankCash,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1104", Name = "Petty Cash",
                AccountType = AccountType.Asset,
                Category = AccountCategory.BankCash,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
                new Account { Code = "1105", Name = "Equity Bank Account",
                AccountType = AccountType.Asset,
                Category = AccountCategory.BankCash,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1106", Name = "KCB Bank Account",
                AccountType = AccountType.Asset,
                Category = AccountCategory.BankCash,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1107", Name = "Cooperative Bank Account",
                AccountType = AccountType.Asset,
                Category = AccountCategory.BankCash,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1108", Name = "Other Bank Account",
                AccountType = AccountType.Asset,
                Category = AccountCategory.BankCash,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1200", Name = "Accounts Receivable",
                AccountType = AccountType.Asset,
                Category = AccountCategory.AccountsReceivable,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1201", Name = "Student Fee Debtors",
                AccountType = AccountType.Asset,
                Category = AccountCategory.AccountsReceivable,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1300", Name = "Fixed Assets",
                AccountType = AccountType.Asset,
                Category = AccountCategory.FixedAsset,
                AllowDirectEntry = false,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1301", Name = "Land & Buildings",
                AccountType = AccountType.Asset,
                Category = AccountCategory.FixedAsset,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1302", Name = "Furniture & Equipment",
                AccountType = AccountType.Asset,
                Category = AccountCategory.FixedAsset,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1303", Name = "Computers & ICT",
                AccountType = AccountType.Asset,
                Category = AccountCategory.FixedAsset,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "1304", Name = "Motor Vehicles",
                AccountType = AccountType.Asset,
                Category = AccountCategory.FixedAsset,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "2000", Name = "LIABILITIES",
                AccountType = AccountType.Liability,
                Category = AccountCategory.CurrentLiability,
                AllowDirectEntry = false,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "2100", Name = "Current Liabilities",
                AccountType = AccountType.Liability,
                Category = AccountCategory.CurrentLiability,
                AllowDirectEntry = false,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "2101", Name = "Accounts Payable",
                AccountType = AccountType.Liability,
                Category = AccountCategory.AccountsPayable,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "2102", Name = "Salaries Payable",
                AccountType = AccountType.Liability,
                Category = AccountCategory.CurrentLiability,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "2103", Name = "PAYE Payable",
                AccountType = AccountType.Liability,
                Category = AccountCategory.CurrentLiability,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "2104", Name = "SHA Payable",
                AccountType = AccountType.Liability,
                Category = AccountCategory.CurrentLiability,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "2105", Name = "NSSF Payable",
                AccountType = AccountType.Liability,
                Category = AccountCategory.CurrentLiability,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "2106", Name = "Fee Deposits",
                AccountType = AccountType.Liability,
                Category = AccountCategory.CurrentLiability,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "3000", Name = "EQUITY / FUND",
                AccountType = AccountType.Equity,
                Category = AccountCategory.Capital,
                AllowDirectEntry = false,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "3001", Name = "School Fund",
                AccountType = AccountType.Equity,
                Category = AccountCategory.Capital,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "3002", Name = "Retained Surplus",
                AccountType = AccountType.Equity,
                Category = AccountCategory.RetainedEarnings,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "3003", Name = "Development Fund",
                AccountType = AccountType.Equity,
                Category = AccountCategory.Capital,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "4000", Name = "INCOME",
                AccountType = AccountType.Income,
                Category = AccountCategory.FeeIncome,
                AllowDirectEntry = false,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "4100", Name = "School Fees Income",
                AccountType = AccountType.Income,
                Category = AccountCategory.FeeIncome,
                AllowDirectEntry = false,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "4101", Name = "Tuition Fees",
                AccountType = AccountType.Income,
                Category = AccountCategory.FeeIncome,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "4102", Name = "Boarding Fees",
                AccountType = AccountType.Income,
                Category = AccountCategory.FeeIncome,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "4103", Name = "Activity Fees",
                AccountType = AccountType.Income,
                Category = AccountCategory.FeeIncome,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "4104", Name = "Examination Fees",
                AccountType = AccountType.Income,
                Category = AccountCategory.FeeIncome,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "4200", Name = "Other Income",
                AccountType = AccountType.Income,
                Category = AccountCategory.OtherIncome,
                AllowDirectEntry = false,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "4201", Name = "Government Grants",
                AccountType = AccountType.Income,
                Category = AccountCategory.OtherIncome,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "4202", Name = "Donations",
                AccountType = AccountType.Income,
                Category = AccountCategory.OtherIncome,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "4203", Name = "Interest Income",
                AccountType = AccountType.Income,
                Category = AccountCategory.OtherIncome,
                NormalBalance = BalanceSide.Credit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5000", Name = "EXPENSES",
                AccountType = AccountType.Expense,
                Category = AccountCategory.StaffCosts,
                AllowDirectEntry = false,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5100", Name = "Staff Costs",
                AccountType = AccountType.Expense,
                Category = AccountCategory.StaffCosts,
                AllowDirectEntry = false,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5101", Name = "Salaries — Teaching Staff",
                AccountType = AccountType.Expense,
                Category = AccountCategory.StaffCosts,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5102", Name = "Salaries — Non-Teaching Staff",
                AccountType = AccountType.Expense,
                Category = AccountCategory.StaffCosts,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5103", Name = "NSSF Contribution",
                AccountType = AccountType.Expense,
                Category = AccountCategory.StaffCosts,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5104", Name = "SHA Contribution",
                AccountType = AccountType.Expense,
                Category = AccountCategory.StaffCosts,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5200", Name = "Operating Expenses",
                AccountType = AccountType.Expense,
                Category = AccountCategory.OperatingExpenses,
                AllowDirectEntry = false,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5201", Name = "Electricity & Water",
                AccountType = AccountType.Expense,
                Category = AccountCategory.OperatingExpenses,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5202", Name = "Telephone & Internet",
                AccountType = AccountType.Expense,
                Category = AccountCategory.OperatingExpenses,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5203", Name = "Repairs & Maintenance",
                AccountType = AccountType.Expense,
                Category = AccountCategory.OperatingExpenses,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5204", Name = "Stationery & Supplies",
                AccountType = AccountType.Expense,
                Category = AccountCategory.OperatingExpenses,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5205", Name = "Transport & Travel",
                AccountType = AccountType.Expense,
                Category = AccountCategory.OperatingExpenses,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5206", Name = "Food & Provisions",
                AccountType = AccountType.Expense,
                Category = AccountCategory.OperatingExpenses,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5207", Name = "Sports & Games",
                AccountType = AccountType.Expense,
                Category = AccountCategory.OperatingExpenses,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5208", Name = "Library Expenses",
                AccountType = AccountType.Expense,
                Category = AccountCategory.OperatingExpenses,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5300", Name = "Administrative Expenses",
                AccountType = AccountType.Expense,
                Category = AccountCategory.AdministrativeExpenses,
                AllowDirectEntry = false,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5301", Name = "Audit Fees",
                AccountType = AccountType.Expense,
                Category = AccountCategory.AdministrativeExpenses,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5302", Name = "Bank Charges",
                AccountType = AccountType.Expense,
                Category = AccountCategory.AdministrativeExpenses,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5303", Name = "Printing & Photocopying",
                AccountType = AccountType.Expense,
                Category = AccountCategory.AdministrativeExpenses,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5304", Name = "Postage & Communication",
                AccountType = AccountType.Expense,
                Category = AccountCategory.AdministrativeExpenses,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new Account { Code = "5305", Name = "Depreciation",
                AccountType = AccountType.Expense,
                Category = AccountCategory.AdministrativeExpenses,
                NormalBalance = BalanceSide.Debit,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
        };

        _db.Accounts.AddRange(accounts);
        await _db.SaveChangesAsync();
        TempData["Success"] =
            $"Default chart of accounts created — {accounts.Count} accounts!";
        return RedirectToAction(nameof(ChartOfAccounts));
    }

    // POST: /Accounting/EditAccount
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditAccount(int id, string code, string name,
    AccountType accountType, AccountCategory category, int? parentAccountId,
    bool allowDirectEntry, string? description, decimal openingBalance)
{
    var account = await _db.Accounts.FindAsync(id);
    if (account == null) return NotFound();

    if (await _db.Accounts.AnyAsync(a => a.Id != id && a.Code == code))
    {
        TempData["Error"] = $"Account code '{code}' is already used by another account.";
        return RedirectToAction(nameof(ChartOfAccounts));
    }

    if (parentAccountId == id)
    {
        TempData["Error"] = "An account cannot be its own parent.";
        return RedirectToAction(nameof(ChartOfAccounts));
    }

    var hasPostedActivity = await _db.JournalEntryLines.AnyAsync(l => l.AccountId == id);
    if (hasPostedActivity && account.AccountType != accountType)
    {
        TempData["Error"] = "This account already has journal entries against it, " +
            "so its Account Type can't be changed (it would distort past reports). " +
            "Name, category, parent and description can still be updated.";
        return RedirectToAction(nameof(ChartOfAccounts));
    }

    account.Code = code;
    account.Name = name;
    account.AccountType = accountType;
    account.Category = category;
    account.ParentAccountId = parentAccountId;
    account.AllowDirectEntry = allowDirectEntry;
    account.Description = description;
    account.OpeningBalance = openingBalance;
    account.NormalBalance = accountType == AccountType.Asset || accountType == AccountType.Expense
        ? BalanceSide.Debit : BalanceSide.Credit;
    account.UpdatedBy = User.Identity?.Name ?? "system";
    account.UpdatedAt = DateTime.UtcNow;

    await _db.SaveChangesAsync();
    TempData["Success"] = $"Account '{code} - {name}' updated.";
    await LogAudit("Accounting", "EditAccount", $"Edited account '{code} - {name}'");
    return RedirectToAction(nameof(ChartOfAccounts));
}

// POST: /Accounting/ToggleAccount
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleAccount(int id)
{
    var account = await _db.Accounts.FindAsync(id);
    if (account == null) return NotFound();

    account.IsActive = !account.IsActive;
    account.UpdatedBy = User.Identity?.Name ?? "system";
    account.UpdatedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();

    var state = account.IsActive ? "activated" : "deactivated";
    TempData["Success"] = $"'{account.Code} - {account.Name}' {state}.";
    await LogAudit("Accounting", "ToggleAccount", $"Account '{account.Code} - {account.Name}' {state}");
    return RedirectToAction(nameof(ChartOfAccounts));
}

// POST: /Accounting/DeleteAccount
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteAccount(int id)
{
    var account = await _db.Accounts.FindAsync(id);
    if (account == null) return NotFound();

    var usage = await GetAccountUsageAsync(id);
    if (usage.Any())
    {
        TempData["Error"] = $"'{account.Code} - {account.Name}' can't be deleted. " +
            $"It's used by: {string.Join(", ", usage)}. Deactivate it instead.";
        return RedirectToAction(nameof(ChartOfAccounts));
    }

    _db.Accounts.Remove(account);
    await _db.SaveChangesAsync();

    TempData["Success"] = $"'{account.Code} - {account.Name}' deleted.";
    await LogAudit("Accounting", "DeleteAccount", $"Deleted account '{account.Code} - {account.Name}'");
    return RedirectToAction(nameof(ChartOfAccounts));
}

private async Task<List<string>> GetAccountUsageAsync(int accountId)
{
    var usage = new List<string>();

    var journalLines = await _db.JournalEntryLines.CountAsync(l => l.AccountId == accountId);
    if (journalLines > 0) usage.Add($"{journalLines} journal entry line(s)");

    var cashbookAsCash = await _db.CashbookEntries.CountAsync(c => c.CashAccountId == accountId);
    if (cashbookAsCash > 0) usage.Add($"{cashbookAsCash} cashbook entry(s) as the cash account");

    var cashbookAsContra = await _db.CashbookEntries.CountAsync(c => c.ContraAccountId == accountId);
    if (cashbookAsContra > 0) usage.Add($"{cashbookAsContra} cashbook entry(s) as the contra account");

    var supplierInvoices = await _db.SupplierInvoices.CountAsync(i => i.ExpenseAccountId == accountId);
    if (supplierInvoices > 0) usage.Add($"{supplierInvoices} supplier invoice(s)");

    var supplierPayments = await _db.SupplierPayments.CountAsync(p => p.PaidFromAccountId == accountId);
    if (supplierPayments > 0) usage.Add($"{supplierPayments} supplier payment(s)");

    var expensesAsExpense = await _db.Expenses.CountAsync(e => e.ExpenseAccountId == accountId);
    if (expensesAsExpense > 0) usage.Add($"{expensesAsExpense} expense(s) recorded against it");

    var expensesAsPaidFrom = await _db.Expenses.CountAsync(e => e.PaidFromAccountId == accountId);
    if (expensesAsPaidFrom > 0) usage.Add($"{expensesAsPaidFrom} expense(s) paid from it");

    var feeCategories = await _db.FeeCategories.CountAsync(c => c.IncomeAccountId == accountId);
    if (feeCategories > 0) usage.Add($"{feeCategories} fee categor(y/ies) linked to it");

    var subAccounts = await _db.Accounts.CountAsync(a => a.ParentAccountId == accountId);
    if (subAccounts > 0) usage.Add($"{subAccounts} sub-account(s) under it");

    return usage;
}

    // ── JOURNAL ENTRIES ──────────────────────────────────────────

    public async Task<IActionResult> JournalEntries(
        string? status, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? DateTime.Today.AddDays(-30);
        var to = toDate ?? DateTime.Today;

        var query = _db.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.EntryDate.Date >= from.Date &&
                        j.EntryDate.Date <= to.Date);

        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<JournalEntryStatus>(status, out var s))
            query = query.Where(j => j.Status == s);

        var entries = await query
            .OrderByDescending(j => j.EntryDate).ToListAsync();

        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.Status = status;
        return View(entries);
    }

    public async Task<IActionResult> CreateJournalEntry()
    {
        var accounts = await _db.Accounts
            .Where(a => a.AllowDirectEntry && a.IsActive)
            .OrderBy(a => a.Code).ToListAsync();
        var terms = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .OrderByDescending(t => t.AcademicYear.Year).ToListAsync();

        ViewBag.Accounts = accounts;
        ViewBag.Terms = terms;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateJournalEntry(
        DateTime entryDate, string description,
        JournalEntryType entryType, string? reference,
        int? academicTermId,
        List<int> accountIds, List<decimal> debitAmounts,
        List<decimal> creditAmounts, List<string?> narrations)
    {
        var totalDebits = debitAmounts.Sum();
        var totalCredits = creditAmounts.Sum();

        if (totalDebits != totalCredits)
        {
            TempData["Error"] =
                $"Journal entry not balanced! " +
                $"Debits: KES {totalDebits:N0} | Credits: KES {totalCredits:N0}";
            return RedirectToAction(nameof(CreateJournalEntry));
        }

        if (totalDebits == 0)
        {
            TempData["Error"] = "Journal entry cannot have zero amounts!";
            return RedirectToAction(nameof(CreateJournalEntry));
        }

        var entryNumber = await GetNextJournalEntryNumber(DateTime.Now.Year);

        var entry = new JournalEntry
        {
            EntryNumber = entryNumber,
            EntryDate = entryDate,
            Description = description,
            EntryType = entryType,
            Reference = reference,
            AcademicTermId = academicTermId,
            Status = JournalEntryStatus.Draft,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        var lines = new List<JournalEntryLine>();
        for (int i = 0; i < accountIds.Count; i++)
        {
            if (debitAmounts[i] == 0 && creditAmounts[i] == 0) continue;
            lines.Add(new JournalEntryLine
            {
                AccountId = accountIds[i],
                DebitAmount = debitAmounts[i],
                CreditAmount = creditAmounts[i],
                Narration = narrations.Count > i ? narrations[i] : null,
                LineNumber = i + 1
            });
        }

        entry.Lines = lines;
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Journal entry {entryNumber} created as Draft!";
        return RedirectToAction(nameof(JournalEntryDetails),
            new { id = entry.Id });
    }

    public async Task<IActionResult> JournalEntryDetails(int id)
    {
        var entry = await _db.JournalEntries
            .Include(j => j.Lines).ThenInclude(l => l.Account)
            .Include(j => j.AcademicTerm).ThenInclude(t => t!.AcademicYear)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (entry == null) return NotFound();
        return View(entry);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostJournalEntry(int id)
    {
        var entry = await _db.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (entry == null) return NotFound();

        if (entry.Status != JournalEntryStatus.Draft)
        {
            TempData["Error"] = "Only Draft entries can be posted!";
            return RedirectToAction(nameof(JournalEntryDetails), new { id });
        }

        var totalDebits = entry.Lines.Sum(l => l.DebitAmount);
        var totalCredits = entry.Lines.Sum(l => l.CreditAmount);

        if (totalDebits != totalCredits)
        {
            TempData["Error"] = "Cannot post unbalanced entry!";
            return RedirectToAction(nameof(JournalEntryDetails), new { id });
        }

        entry.Status = JournalEntryStatus.Posted;
        entry.PostedBy = User.Identity?.Name;
        entry.PostedAt = DateTime.UtcNow;
        entry.UpdatedBy = User.Identity?.Name;
        entry.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] =
            $"Journal entry {entry.EntryNumber} posted successfully!";
            await LogAudit("Accounting", "PostJournalEntry", $"Posted journal entry {entry.EntryNumber}");
        return RedirectToAction(nameof(JournalEntryDetails), new { id });
    }

    // ── GENERAL LEDGER ────────────────────────────────────────────

    public async Task<IActionResult> GeneralLedger(
        int? accountId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? new DateTime(DateTime.Today.Year, 1, 1);
        var to = toDate ?? DateTime.Today;

        var accounts = await _db.Accounts
            .Where(a => a.AllowDirectEntry && a.IsActive)
            .OrderBy(a => a.Code).ToListAsync();

        Account? selectedAccount = null;
        List<JournalEntryLine> lines = new();

        if (accountId.HasValue)
        {
            selectedAccount = await _db.Accounts.FindAsync(accountId);
            lines = await _db.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Include(l => l.Account)
                .Where(l => l.AccountId == accountId &&
                            l.JournalEntry.Status == JournalEntryStatus.Posted &&
                            l.JournalEntry.EntryDate.Date >= from.Date &&
                            l.JournalEntry.EntryDate.Date <= to.Date)
                .OrderBy(l => l.JournalEntry.EntryDate)
                .ThenBy(l => l.JournalEntry.EntryNumber)
                .ToListAsync();
        }

        ViewBag.Accounts = accounts;
        ViewBag.SelectedAccount = selectedAccount;
        ViewBag.AccountId = accountId;
        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.OpeningBalance = selectedAccount?.OpeningBalance ?? 0;
        ViewBag.School = await _db.SchoolProfiles.FirstOrDefaultAsync();

        return View(lines);
    }

    // ── TRIAL BALANCE ─────────────────────────────────────────────

    public async Task<IActionResult> TrialBalance(
        DateTime? asAt, bool showSubAccounts = true)
    {
        var date = asAt ?? DateTime.Today;

        var accounts = await _db.Accounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.Code).ToListAsync();

        var filteredAccounts = showSubAccounts
            ? accounts.Where(a => a.AllowDirectEntry).ToList()
            : accounts.ToList();

        var ledgerBalances =
            new Dictionary<int, (decimal Debit, decimal Credit)>();

        foreach (var account in filteredAccounts)
        {
            var lines = await _db.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == account.Id &&
                            l.JournalEntry.Status == JournalEntryStatus.Posted &&
                            l.JournalEntry.EntryDate.Date <= date.Date)
                .ToListAsync();

            var totalDebit = account.OpeningBalance > 0 &&
                account.NormalBalance == BalanceSide.Debit
                    ? account.OpeningBalance : 0;
            var totalCredit = account.OpeningBalance > 0 &&
                account.NormalBalance == BalanceSide.Credit
                    ? account.OpeningBalance : 0;

            totalDebit += lines.Sum(l => l.DebitAmount);
            totalCredit += lines.Sum(l => l.CreditAmount);

            if (totalDebit > 0 || totalCredit > 0)
                ledgerBalances[account.Id] = (totalDebit, totalCredit);
        }

        ViewBag.AsAt = date;
        ViewBag.ShowSubAccounts = showSubAccounts;
        ViewBag.LedgerBalances = ledgerBalances;
        ViewBag.School = await _db.SchoolProfiles.FirstOrDefaultAsync();
        ViewBag.TotalDebits = ledgerBalances.Values.Sum(v => v.Debit);
        ViewBag.TotalCredits = ledgerBalances.Values.Sum(v => v.Credit);

        return View(filteredAccounts
            .Where(a => ledgerBalances.ContainsKey(a.Id)).ToList());
    }

        // ── CASHBOOK ──────────────────────────────────────────────────

    public async Task<IActionResult> Cashbook(
        int? cashAccountId, DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? new DateTime(DateTime.Today.Year, 1, 1);
        var to = toDate ?? DateTime.Today;

        var cashAccounts = await _db.Accounts
            .Where(a => a.Category == AccountCategory.BankCash && a.IsActive)
            .OrderBy(a => a.Code)
            .ToListAsync();

        ViewBag.CashAccounts = cashAccounts;
        ViewBag.School = await _db.SchoolProfiles.FirstOrDefaultAsync();

        if (!cashAccounts.Any())
        {
            ViewBag.SelectedAccount = null;
            return View(new List<CashbookEntry>());
        }

        var selectedAccount = cashAccountId.HasValue
            ? cashAccounts.FirstOrDefault(a => a.Id == cashAccountId) ?? cashAccounts.First()
            : cashAccounts.First();

        var entries = await _db.CashbookEntries
            .Include(e => e.ContraAccount)
            .Where(e => e.CashAccountId == selectedAccount.Id &&
                        e.EntryDate.Date >= from.Date &&
                        e.EntryDate.Date <= to.Date)
            .OrderBy(e => e.EntryDate)
            .ThenBy(e => e.EntryNumber)
            .ToListAsync();

        var accounts = await _db.Accounts
            .Where(a => a.AllowDirectEntry && a.IsActive)
            .OrderBy(a => a.Code).ToListAsync();

        var terms = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .OrderByDescending(t => t.AcademicYear.Year)
            .ToListAsync();

        var totalReceipts = entries.Where(e => e.EntryType == CashbookEntryType.Receipt).Sum(e => e.Amount);
        var totalPayments = entries.Where(e => e.EntryType == CashbookEntryType.Payment).Sum(e => e.Amount);
        var openingBalance = selectedAccount.OpeningBalance;

        var cashSummary = new List<object>();
        foreach (var acc in cashAccounts)
        {
            var allEntries = await _db.CashbookEntries
                .Where(e => e.CashAccountId == acc.Id).ToListAsync();
            var receipts = allEntries.Where(e => e.EntryType == CashbookEntryType.Receipt).Sum(e => e.Amount);
            var payments = allEntries.Where(e => e.EntryType == CashbookEntryType.Payment).Sum(e => e.Amount);
            cashSummary.Add(new
            {
                AccountId = acc.Id,
                AccountName = acc.Name,
                acc.BankName,
                acc.AccountNumber,
                OpeningBalance = acc.OpeningBalance,
                Receipts = receipts,
                Payments = payments,
                Balance = acc.OpeningBalance + receipts - payments
            });
        }

        ViewBag.SelectedAccount = selectedAccount;
        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.Accounts = accounts;
        ViewBag.Terms = terms;
        ViewBag.TotalReceipts = totalReceipts;
        ViewBag.TotalPayments = totalPayments;
        ViewBag.OpeningBalance = openingBalance;
        ViewBag.ClosingBalance = openingBalance + totalReceipts - totalPayments;
        ViewBag.CashSummary = cashSummary;
        ViewBag.ReportTitle = $"Cashbook — {selectedAccount.Name}";
        ViewBag.ReportSubtitle = $"Period: {from:dd MMM yyyy} — {to:dd MMM yyyy}";

        return View(entries);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCashAccount(
        string name, string? bankName, string? accountNumber,
        decimal openingBalance)
    {
        var count = await _db.Accounts
            .CountAsync(a => a.Category == AccountCategory.BankCash) + 1;
        var code = $"11{count:D2}";
        while (await _db.Accounts.AnyAsync(a => a.Code == code))
        {
            count++;
            code = $"11{count:D2}";
        }

        var account = new Account
        {
            Code = code,
            Name = name,
            AccountType = AccountType.Asset,
            Category = AccountCategory.BankCash,
            AllowDirectEntry = true,
            BankName = bankName,
            AccountNumber = accountNumber,
            OpeningBalance = openingBalance,
            NormalBalance = BalanceSide.Debit,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Cash/Bank account '{name}' created!";
        await LogAudit("Accounting", "CreateCashAccount", $"Created cash/bank account '{name}' ({code})");
        return RedirectToAction(nameof(Cashbook), new { cashAccountId = account.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCashbookEntry(
        DateTime entryDate, CashbookEntryType entryType,
        int cashAccountId, int accountId, decimal amount,
        string description, string? reference, string? payeePayer,
        int? academicTermId)
    {
        var entryNumber = await GetNextCashbookEntryNumber(DateTime.Now.Year);

        var entry = new CashbookEntry
        {
            EntryNumber = entryNumber,
            EntryDate = entryDate,
            EntryType = entryType,
            CashAccountId = cashAccountId,
            ContraAccountId = accountId,
            Amount = amount,
            Description = description,
            Reference = reference,
            PayeePayer = payeePayer,
            AcademicTermId = academicTermId,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        // Auto-create the matching journal entry
        var journalEntry = new JournalEntry
{
    EntryNumber = await GetNextJournalEntryNumber(DateTime.Now.Year),
            EntryDate = entryDate,
            Description = description,
            EntryType = JournalEntryType.General,
            Reference = reference,
            AcademicTermId = academicTermId,
            Status = JournalEntryStatus.Posted,
            PostedBy = User.Identity?.Name,
            PostedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        if (entryType == CashbookEntryType.Receipt)
        {
            journalEntry.Lines = new List<JournalEntryLine>
            {
                new JournalEntryLine { AccountId = cashAccountId, DebitAmount = amount, CreditAmount = 0, LineNumber = 1 },
                new JournalEntryLine { AccountId = accountId, DebitAmount = 0, CreditAmount = amount, LineNumber = 2 }
            };
        }
        else
        {
            journalEntry.Lines = new List<JournalEntryLine>
            {
                new JournalEntryLine { AccountId = accountId, DebitAmount = amount, CreditAmount = 0, LineNumber = 1 },
                new JournalEntryLine { AccountId = cashAccountId, DebitAmount = 0, CreditAmount = amount, LineNumber = 2 }
            };
        }

        _db.JournalEntries.Add(journalEntry);
        await _db.SaveChangesAsync();

        entry.JournalEntryId = journalEntry.Id;
        _db.CashbookEntries.Add(entry);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Cashbook entry {entryNumber} recorded!";
        return RedirectToAction(nameof(Cashbook), new { cashAccountId });
    }
    private async Task<string> GetNextCashbookEntryNumber(int year)
{
    var prefix = $"CB/{year}/";
    var existing = await _db.CashbookEntries
        .Where(c => c.EntryNumber.StartsWith(prefix))
        .Select(c => c.EntryNumber)
        .ToListAsync();

    int maxSeq = existing
        .Select(n => int.TryParse(n.Split('/').Last(), out var seq) ? seq : 0)
        .DefaultIfEmpty(0)
        .Max();

    return $"{prefix}{(maxSeq + 1):D5}";
}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCashOpeningBalance(
        int accountId, decimal openingBalance)
    {
        var account = await _db.Accounts.FindAsync(accountId);
        if (account == null)
        {
            TempData["Error"] = "Cash account not found!";
            return RedirectToAction(nameof(Cashbook));
        }

        account.OpeningBalance = openingBalance;
        account.UpdatedBy = User.Identity?.Name;
        account.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Opening balance for {account.Name} set!";
        return RedirectToAction(nameof(Cashbook), new { cashAccountId = accountId });
    }
    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> TransferFunds(
    DateTime transferDate, int fromAccountId, int toAccountId,
    decimal amount, string? description)
{
    if (fromAccountId == toAccountId)
    {
        TempData["Error"] = "From and To accounts must be different!";
        return RedirectToAction(nameof(Cashbook), new { cashAccountId = fromAccountId });
    }

    var fromAccount = await _db.Accounts.FindAsync(fromAccountId);
    var toAccount = await _db.Accounts.FindAsync(toAccountId);

    if (fromAccount == null || toAccount == null)
    {
        TempData["Error"] = "Invalid account selected!";
        return RedirectToAction(nameof(Cashbook));
    }

    var desc = string.IsNullOrEmpty(description)
        ? $"Transfer: {fromAccount.Name} → {toAccount.Name}"
        : description;

    // Journal entry: Dr To / Cr From
    var journalEntry = new JournalEntry
{
    EntryNumber = await GetNextJournalEntryNumber(DateTime.Now.Year),
        EntryDate = transferDate,
        Description = desc,
        EntryType = JournalEntryType.General,
        Status = JournalEntryStatus.Posted,
        PostedBy = User.Identity?.Name,
        PostedAt = DateTime.UtcNow,
        CreatedBy = User.Identity?.Name ?? "system",
        CreatedAt = DateTime.UtcNow,
        Lines = new List<JournalEntryLine>
        {
            new JournalEntryLine { AccountId = toAccountId, DebitAmount = amount, CreditAmount = 0, Narration = desc, LineNumber = 1 },
            new JournalEntryLine { AccountId = fromAccountId, DebitAmount = 0, CreditAmount = amount, Narration = desc, LineNumber = 2 }
        }
    };
    _db.JournalEntries.Add(journalEntry);
    await _db.SaveChangesAsync();

    var cbCount = await _db.CashbookEntries.CountAsync();

    // Payment out of "From" account
    _db.CashbookEntries.Add(new CashbookEntry
    {
        EntryNumber = await GetNextCashbookEntryNumber(DateTime.Now.Year),
        EntryDate = transferDate,
        EntryType = CashbookEntryType.Payment,
        CashAccountId = fromAccountId,
        ContraAccountId = toAccountId,
        Amount = amount,
        Description = desc,
        JournalEntryId = journalEntry.Id,
        CreatedBy = User.Identity?.Name ?? "system",
        CreatedAt = DateTime.UtcNow
    });

    await _db.SaveChangesAsync();

// Receipt into "To" account
_db.CashbookEntries.Add(new CashbookEntry
{
    EntryNumber = await GetNextCashbookEntryNumber(DateTime.Now.Year),
        EntryDate = transferDate,
        EntryType = CashbookEntryType.Receipt,
        CashAccountId = toAccountId,
        ContraAccountId = fromAccountId,
        Amount = amount,
        Description = desc,
        JournalEntryId = journalEntry.Id,
        CreatedBy = User.Identity?.Name ?? "system",
        CreatedAt = DateTime.UtcNow
    });

    await _db.SaveChangesAsync();

    TempData["Success"] =
        $"KES {amount:N0} transferred from {fromAccount.Name} to {toAccount.Name}!";
    await LogAudit("Accounting", "TransferFunds",
        $"Transferred KES {amount:N0} from {fromAccount.Name} to {toAccount.Name}");
    return RedirectToAction(nameof(Cashbook), new { cashAccountId = fromAccountId });
}

    // ── STUDENT LEDGER ────────────────────────────────────────────

    public async Task<IActionResult> StudentLedger(int? studentId)
    {
        var students = await _db.Students
            .Include(s => s.ClassLevel)
            .Where(s => s.Status == Domain.Enums.StudentStatus.Active)
            .OrderBy(s => s.LastName).ToListAsync();

        Domain.Entities.Students.Student? student = null;
        List<StudentLedgerEntry> ledgerEntries = new();
        decimal balance = 0;

        if (studentId.HasValue)
        {
            student = await _db.Students
                .Include(s => s.ClassLevel)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            var invoices = await _db.StudentInvoices
                .Include(i => i.AcademicYear)
                .Include(i => i.AcademicTerm)
                .Include(i => i.Items).ThenInclude(item => item.FeeCategory)
                .Where(i => i.StudentId == studentId)
                .OrderBy(i => i.InvoiceDate).ToListAsync();

            var payments = await _db.FeePayments
                .Include(p => p.StudentInvoice)
                .Where(p => p.StudentId == studentId && !p.IsReversed)
                .OrderBy(p => p.PaymentDate).ToListAsync();

            foreach (var inv in invoices)
            {
                ledgerEntries.Add(new StudentLedgerEntry
                {
                    Date = inv.InvoiceDate,
                    Reference = inv.InvoiceNumber,
                    Description = $"Fee Invoice — " +
                        $"{inv.AcademicYear.Name} {inv.AcademicTerm.Name}",
                    Debit = inv.TotalAmount,
                    Credit = 0,
                    Type = "Invoice"
                });
            }

            foreach (var pay in payments)
            {
                ledgerEntries.Add(new StudentLedgerEntry
                {
                    Date = pay.PaymentDate,
                    Reference = pay.ReceiptNumber,
                    Description = $"Payment — {pay.PaymentMethod}" +
                        (pay.ReferenceNumber != null
                            ? $" ({pay.ReferenceNumber})" : ""),
                    Debit = 0,
                    Credit = pay.Amount,
                    Type = "Payment"
                });
            }

            ledgerEntries = ledgerEntries
                .OrderBy(e => e.Date).ThenBy(e => e.Reference).ToList();

            decimal running = 0;
            foreach (var entry in ledgerEntries)
            {
                running += entry.Debit - entry.Credit;
                entry.Balance = running;
            }
            balance = running;
        }

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        ViewBag.Students = students;
        ViewBag.Student = student;
        ViewBag.StudentId = studentId;
        ViewBag.Balance = balance;
        ViewBag.School = school;
        ViewBag.ReportTitle = "Student Fee Ledger";
        ViewBag.ReportSubtitle = student != null
            ? $"Student: {student.FullName} | Adm: {student.AdmissionNumber}"
            : "";

        return View(ledgerEntries);
    }

    // ── INCOME & EXPENDITURE ──────────────────────────────────────

    public async Task<IActionResult> IncomeExpenditure(
        DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? new DateTime(DateTime.Today.Year, 1, 1);
        var to = toDate ?? DateTime.Today;

        var incomeAccounts = await _db.Accounts
            .Where(a => a.AccountType == AccountType.Income &&
                        a.AllowDirectEntry && a.IsActive)
            .OrderBy(a => a.Code).ToListAsync();

        var expenseAccounts = await _db.Accounts
            .Where(a => a.AccountType == AccountType.Expense &&
                        a.AllowDirectEntry && a.IsActive)
            .OrderBy(a => a.Code).ToListAsync();

        var incomeBalances = new Dictionary<int, decimal>();
        var expenseBalances = new Dictionary<int, decimal>();

        foreach (var acc in incomeAccounts)
        {
            var lines = await _db.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == acc.Id &&
                            l.JournalEntry.Status == JournalEntryStatus.Posted &&
                            l.JournalEntry.EntryDate.Date >= from.Date &&
                            l.JournalEntry.EntryDate.Date <= to.Date)
                .ToListAsync();

            var balance = (acc.OpeningBalance > 0 ? acc.OpeningBalance : 0) +
                lines.Sum(l => l.CreditAmount) - lines.Sum(l => l.DebitAmount);

            if (balance != 0) incomeBalances[acc.Id] = balance;
        }

        foreach (var acc in expenseAccounts)
        {
            var lines = await _db.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == acc.Id &&
                            l.JournalEntry.Status == JournalEntryStatus.Posted &&
                            l.JournalEntry.EntryDate.Date >= from.Date &&
                            l.JournalEntry.EntryDate.Date <= to.Date)
                .ToListAsync();

            var balance = (acc.OpeningBalance > 0 ? acc.OpeningBalance : 0) +
                lines.Sum(l => l.DebitAmount) - lines.Sum(l => l.CreditAmount);

            if (balance != 0) expenseBalances[acc.Id] = balance;
        }

        var totalIncome = incomeBalances.Values.Sum();
        var totalExpenses = expenseBalances.Values.Sum();
        var surplus = totalIncome - totalExpenses;

        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.IncomeAccounts = incomeAccounts
            .Where(a => incomeBalances.ContainsKey(a.Id)).ToList();
        ViewBag.ExpenseAccounts = expenseAccounts
            .Where(a => expenseBalances.ContainsKey(a.Id)).ToList();
        ViewBag.IncomeBalances = incomeBalances;
        ViewBag.ExpenseBalances = expenseBalances;
        ViewBag.TotalIncome = totalIncome;
        ViewBag.TotalExpenses = totalExpenses;
        ViewBag.Surplus = surplus;
        ViewBag.School = await _db.SchoolProfiles.FirstOrDefaultAsync();
        ViewBag.ReportTitle = "Income & Expenditure Statement";
        ViewBag.ReportSubtitle =
            $"Period: {from:dd MMM yyyy} — {to:dd MMM yyyy}";

        return View();
    }

    // ── BALANCE SHEET ─────────────────────────────────────────────

    public async Task<IActionResult> BalanceSheet(DateTime? asAt)
    {
        var date = asAt ?? DateTime.Today;

        var allAccounts = await _db.Accounts
            .Where(a => a.AllowDirectEntry && a.IsActive)
            .OrderBy(a => a.Code).ToListAsync();

        var balances = new Dictionary<int, decimal>();

        foreach (var acc in allAccounts)
        {
            var lines = await _db.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == acc.Id &&
                            l.JournalEntry.Status == JournalEntryStatus.Posted &&
                            l.JournalEntry.EntryDate.Date <= date.Date)
                .ToListAsync();

            decimal balance;
            if (acc.NormalBalance == BalanceSide.Debit)
                balance = (acc.OpeningBalance > 0 ? acc.OpeningBalance : 0) +
                    lines.Sum(l => l.DebitAmount) - lines.Sum(l => l.CreditAmount);
            else
                balance = (acc.OpeningBalance > 0 ? acc.OpeningBalance : 0) +
                    lines.Sum(l => l.CreditAmount) - lines.Sum(l => l.DebitAmount);

            if (balance != 0) balances[acc.Id] = balance;
        }

        var assets = allAccounts
            .Where(a => a.AccountType == AccountType.Asset &&
                        balances.ContainsKey(a.Id)).ToList();
        var liabilities = allAccounts
            .Where(a => a.AccountType == AccountType.Liability &&
                        balances.ContainsKey(a.Id)).ToList();
        var equity = allAccounts
            .Where(a => a.AccountType == AccountType.Equity &&
                        balances.ContainsKey(a.Id)).ToList();

        ViewBag.AsAt = date;
        ViewBag.Assets = assets;
        ViewBag.Liabilities = liabilities;
        ViewBag.Equity = equity;
        ViewBag.Balances = balances;
        ViewBag.TotalAssets = assets.Sum(a =>
            balances.GetValueOrDefault(a.Id, 0));
        ViewBag.TotalLiabilities = liabilities.Sum(a =>
            balances.GetValueOrDefault(a.Id, 0));
        ViewBag.TotalEquity = equity.Sum(a =>
            balances.GetValueOrDefault(a.Id, 0));
        ViewBag.School = await _db.SchoolProfiles.FirstOrDefaultAsync();
        ViewBag.ReportTitle = "Balance Sheet";
        ViewBag.ReportSubtitle = $"As at {date:dd MMM yyyy}";

        return View();
    }
    private async Task<string> GetNextJournalEntryNumber(int year)
{
    var prefix = $"JV/{year}/";
    var lastNumber = await _db.JournalEntries
        .Where(j => j.EntryNumber.StartsWith(prefix))
        .Select(j => j.EntryNumber)
        .ToListAsync();

    int maxSeq = lastNumber
        .Select(n => int.TryParse(n.Split('/').Last(), out var seq) ? seq : 0)
        .DefaultIfEmpty(0)
        .Max();

    return $"{prefix}{(maxSeq + 1):D5}";
}
}