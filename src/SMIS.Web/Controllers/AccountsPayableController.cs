using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Accounting;
using SMIS.Domain.Entities.Finance;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

[Authorize(Roles = "Administrator,Bursar")]
public class AccountsPayableController : BaseController
{
    private new readonly ApplicationDbContext _db;

    public AccountsPayableController(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    // ── DASHBOARD ─────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var totalSuppliers = await _db.Suppliers.CountAsync();
        var unpaidInvoices = await _db.SupplierInvoices
            .CountAsync(i => i.Status != SupplierInvoiceStatus.Paid &&
                             i.Status != SupplierInvoiceStatus.Cancelled);
        var totalOwing = await _db.SupplierInvoices
            .Where(i => i.Status != SupplierInvoiceStatus.Paid &&
                        i.Status != SupplierInvoiceStatus.Cancelled)
            .SumAsync(i => i.TotalAmount - i.TotalPaid);
        var overdueInvoices = await _db.SupplierInvoices
            .CountAsync(i => i.Status != SupplierInvoiceStatus.Paid &&
                             i.DueDate < DateTime.Today);
        var totalExpenses = await _db.Expenses
            .Where(e => e.ExpenseDate.Year == DateTime.Today.Year)
            .SumAsync(e => e.Amount);

        var recentInvoices = await _db.SupplierInvoices
            .Include(i => i.Supplier)
            .OrderByDescending(i => i.InvoiceDate)
            .Take(10).ToListAsync();

        ViewBag.TotalSuppliers = totalSuppliers;
        ViewBag.UnpaidInvoices = unpaidInvoices;
        ViewBag.TotalOwing = totalOwing;
        ViewBag.OverdueInvoices = overdueInvoices;
        ViewBag.TotalExpenses = totalExpenses;
        ViewBag.RecentInvoices = recentInvoices;

        return View();
    }

    // ── SUPPLIERS ─────────────────────────────────────────────────

    public async Task<IActionResult> Suppliers()
    {
        var suppliers = await _db.Suppliers
            .Include(s => s.Invoices)
            .OrderBy(s => s.Name).ToListAsync();
        return View(suppliers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupplier(
        string name, string? contactPerson, string? phone,
        string? email, string? address, string? kraPin,
        SupplierCategory category, int paymentTermsDays,
        decimal creditLimit)
    {
        var code = await GetNextSupplierCode();

        var supplier = new Supplier
        {
            Code = code,
            Name = name,
            ContactPerson = contactPerson,
            Phone = phone,
            Email = email,
            Address = address,
            KRAPin = kraPin,
            Category = category,
            PaymentTermsDays = paymentTermsDays,
            CreditLimit = creditLimit,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Supplier '{name}' created! Code: {code}";
        await LogAudit("AccountsPayable", "CreateSupplier", $"Created supplier '{name}' — Code {code}");
        return RedirectToAction(nameof(Suppliers));
    }

    // ── SUPPLIER INVOICES ─────────────────────────────────────────

    public async Task<IActionResult> SupplierInvoices(string? status)
    {
        var query = _db.SupplierInvoices
            .Include(i => i.Supplier)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<SupplierInvoiceStatus>(status, out var s))
            query = query.Where(i => i.Status == s);

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate).ToListAsync();

        var suppliers = await _db.Suppliers
            .OrderBy(s => s.Name).ToListAsync();
        var accounts = await _db.Accounts
            .Where(a => a.AllowDirectEntry && a.IsActive &&
                        (a.AccountType == AccountType.Expense ||
                         a.AccountType == AccountType.Asset))
            .OrderBy(a => a.Code).ToListAsync();

        ViewBag.Status = status;
        ViewBag.Suppliers = suppliers;
        ViewBag.Accounts = accounts;
        var cashAccounts = await _db.Accounts
    .Where(a => a.Category == AccountCategory.BankCash && a.IsActive)
    .OrderBy(a => a.Code).ToListAsync();
ViewBag.CashAccounts = cashAccounts;
        ViewBag.TotalUnpaid = invoices
            .Where(i => i.Status != SupplierInvoiceStatus.Paid &&
                        i.Status != SupplierInvoiceStatus.Cancelled)
            .Sum(i => i.Balance);

        return View(invoices);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupplierInvoice(
        int supplierId, string supplierInvoiceRef,
        DateTime invoiceDate, DateTime dueDate,
        decimal totalAmount, string? description,
        int? expenseAccountId)
    {
        var invoiceNumber = await GetNextSupplierInvoiceNumber(DateTime.Now.Year);

        // Create journal entry: Dr Expense / Cr Accounts Payable
        JournalEntry? journalEntry = null;
        var apAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == "2101");
        var expAccount = expenseAccountId.HasValue
            ? await _db.Accounts.FindAsync(expenseAccountId)
            : null;

        if (apAccount != null && expAccount != null)
        {
            var supplier = await _db.Suppliers.FindAsync(supplierId);
            journalEntry = new JournalEntry
            {
                EntryNumber = await GetNextJournalEntryNumber(DateTime.Now.Year),
                EntryDate = invoiceDate,
                Description = $"Supplier Invoice — {supplier?.Name} — {supplierInvoiceRef}",
                EntryType = JournalEntryType.General,
                Reference = supplierInvoiceRef,
                Status = JournalEntryStatus.Posted,
                PostedBy = User.Identity?.Name,
                PostedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow,
                Lines = new List<JournalEntryLine>
                {
                    new JournalEntryLine
                    {
                        AccountId = expAccount.Id,
                        DebitAmount = totalAmount,
                        CreditAmount = 0,
                        Narration = description,
                        LineNumber = 1
                    },
                    new JournalEntryLine
                    {
                        AccountId = apAccount.Id,
                        DebitAmount = 0,
                        CreditAmount = totalAmount,
                        Narration = description,
                        LineNumber = 2
                    }
                }
            };
            _db.JournalEntries.Add(journalEntry);
            await _db.SaveChangesAsync();
        }

        var invoice = new SupplierInvoice
        {
            InvoiceNumber = invoiceNumber,
            SupplierInvoiceRef = supplierInvoiceRef,
            SupplierId = supplierId,
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            TotalAmount = totalAmount,
            TotalPaid = 0,
            Description = description,
            ExpenseAccountId = expenseAccountId,
            JournalEntryId = journalEntry?.Id,
            Status = SupplierInvoiceStatus.Unpaid,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        _db.SupplierInvoices.Add(invoice);
        await _db.SaveChangesAsync();
        TempData["Success"] =
            $"Supplier invoice {invoiceNumber} created!";
            await LogAudit("AccountsPayable", "CreateSupplierInvoice",
    $"Created supplier invoice {invoiceNumber} for KES {totalAmount:N0}");
        return RedirectToAction(nameof(SupplierInvoices));
    }

    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PaySupplierInvoice(
    int invoiceId, decimal amount,
    PaymentMethod paymentMethod,
    int paidFromAccountId,
    string? referenceNumber, string? notes)
{
    var invoice = await _db.SupplierInvoices
        .Include(i => i.Supplier)
        .FirstOrDefaultAsync(i => i.Id == invoiceId);

    if (invoice == null)
    {
        TempData["Error"] = "Invoice not found!";
        return RedirectToAction(nameof(SupplierInvoices));
    }

    var cashAccount = await _db.Accounts.FindAsync(paidFromAccountId);
    var apAccount = await _db.Accounts
        .FirstOrDefaultAsync(a => a.Code == "2101");

        // Create journal: Dr AP / Cr Cash
        JournalEntry? journalEntry = null;
        if (cashAccount != null && apAccount != null)
        {
            journalEntry = new JournalEntry
            {
                EntryNumber = await GetNextJournalEntryNumber(DateTime.Now.Year),
                EntryDate = DateTime.Today,
                Description = $"Payment to {invoice.Supplier.Name} — {invoice.InvoiceNumber}",
                EntryType = JournalEntryType.General,
                Reference = referenceNumber,
                Status = JournalEntryStatus.Posted,
                PostedBy = User.Identity?.Name,
                PostedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow,
                Lines = new List<JournalEntryLine>
                {
                    new JournalEntryLine
                    {
                        AccountId = apAccount.Id,
                        DebitAmount = amount,
                        CreditAmount = 0,
                        Narration = $"Payment — {invoice.InvoiceNumber}",
                        LineNumber = 1
                    },
                    new JournalEntryLine
                    {
                        AccountId = cashAccount.Id,
                        DebitAmount = 0,
                        CreditAmount = amount,
                        Narration = $"Payment — {invoice.InvoiceNumber}",
                        LineNumber = 2
                    }
                }
            };
            _db.JournalEntries.Add(journalEntry);
            await _db.SaveChangesAsync();
            _db.CashbookEntries.Add(new CashbookEntry
{
    EntryNumber = await GetNextCashbookEntryNumber(DateTime.Now.Year),
    EntryDate = DateTime.Today,
    EntryType = CashbookEntryType.Payment,
    CashAccountId = paidFromAccountId,
    ContraAccountId = apAccount.Id,
    Amount = amount,
    Description = $"Payment to {invoice.Supplier.Name} — {invoice.InvoiceNumber}",
    PayeePayer = invoice.Supplier.Name,
    Reference = referenceNumber,
    JournalEntryId = journalEntry.Id,
    CreatedBy = User.Identity?.Name ?? "system",
    CreatedAt = DateTime.UtcNow
});
await _db.SaveChangesAsync();
        }

        var payment = new SupplierPayment
{
    PaymentNumber = await GetNextSupplierPaymentNumber(DateTime.Now.Year),
            SupplierInvoiceId = invoiceId,
            SupplierId = invoice.SupplierId,
            PaymentDate = DateTime.Today,
            Amount = amount,
            PaymentMethod = paymentMethod,
            ReferenceNumber = referenceNumber,
            PaidFromAccountId = paidFromAccountId,
            JournalEntryId = journalEntry?.Id,
            Notes = notes,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        invoice.TotalPaid += amount;
        invoice.Status = invoice.TotalPaid >= invoice.TotalAmount
            ? SupplierInvoiceStatus.Paid
            : SupplierInvoiceStatus.PartiallyPaid;

        _db.SupplierPayments.Add(payment);
        await _db.SaveChangesAsync();

        TempData["Success"] =
            $"Payment of KES {amount:N0} to {invoice.Supplier.Name} recorded!";
            await LogAudit("AccountsPayable", "PaySupplierInvoice",
    $"Paid KES {amount:N0} to {invoice.Supplier.Name} — Invoice {invoice.InvoiceNumber}");
        return RedirectToAction(nameof(SupplierInvoices));
    }

    // ── EXPENSES ──────────────────────────────────────────────────

    public async Task<IActionResult> Expenses(
        DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? DateTime.Today.AddDays(-30);
        var to = toDate ?? DateTime.Today;

        var expenses = await _db.Expenses
            .Include(e => e.ExpenseAccount)
            .Where(e => e.ExpenseDate.Date >= from.Date &&
                        e.ExpenseDate.Date <= to.Date)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();

        var accounts = await _db.Accounts
            .Where(a => a.AllowDirectEntry && a.IsActive &&
                        a.AccountType == AccountType.Expense)
            .OrderBy(a => a.Code).ToListAsync();

        var terms = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .OrderByDescending(t => t.AcademicYear.Year)
            .ToListAsync();

        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.Accounts = accounts;
        var cashAccounts = await _db.Accounts
    .Where(a => a.Category == AccountCategory.BankCash && a.IsActive)
    .OrderBy(a => a.Code).ToListAsync();
ViewBag.CashAccounts = cashAccounts;
        ViewBag.Terms = terms;
        ViewBag.TotalExpenses = expenses.Sum(e => e.Amount);

        return View(expenses);
    }

    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateExpense(
    DateTime expenseDate, int expenseAccountId,
    decimal amount, string description,
    string? payee, string? receiptNumber,
    int paidFromAccountId, int? academicTermId)
{
    var cashAccount = await _db.Accounts.FindAsync(paidFromAccountId);
    var expAccount = await _db.Accounts.FindAsync(expenseAccountId);

        JournalEntry? journalEntry = null;
        if (cashAccount != null && expAccount != null)
        {
            journalEntry = new JournalEntry
            {
                EntryNumber = await GetNextJournalEntryNumber(DateTime.Now.Year),
                EntryDate = expenseDate,
                Description = description,
                EntryType = JournalEntryType.Expense,
                Reference = receiptNumber,
                AcademicTermId = academicTermId,
                Status = JournalEntryStatus.Posted,
                PostedBy = User.Identity?.Name,
                PostedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow,
                Lines = new List<JournalEntryLine>
                {
                    new JournalEntryLine
                    {
                        AccountId = expenseAccountId,
                        DebitAmount = amount,
                        CreditAmount = 0,
                        Narration = description,
                        LineNumber = 1
                    },
                    new JournalEntryLine
                    {
                        AccountId = cashAccount.Id,
                        DebitAmount = 0,
                        CreditAmount = amount,
                        Narration = description,
                        LineNumber = 2
                    }
                }
            };
            _db.JournalEntries.Add(journalEntry);
            await _db.SaveChangesAsync();
        }

        var expense = new Expense
{
    ExpenseNumber = await GetNextExpenseNumber(DateTime.Now.Year),
            ExpenseDate = expenseDate,
            ExpenseAccountId = expenseAccountId,
            Amount = amount,
            Description = description,
            Payee = payee,
            ReceiptNumber = receiptNumber,
            PaidFromAccountId = paidFromAccountId,
            AcademicTermId = academicTermId,
            JournalEntryId = journalEntry?.Id,
            IsApproved = true,
            ApprovedBy = User.Identity?.Name,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        TempData["Success"] =
            $"Expense of KES {amount:N0} recorded! Ref: {expense.ExpenseNumber}";
            await LogAudit("AccountsPayable", "CreateExpense",
    $"Recorded expense of KES {amount:N0} — {expense.ExpenseNumber}");
        return RedirectToAction(nameof(Expenses));
    }
    private async Task<string> GetNextSupplierCode()
{
    var existing = await _db.Suppliers.Select(s => s.Code).ToListAsync();
    int maxSeq = existing.Select(c => int.TryParse(c.Replace("SUP", ""), out var s) ? s : 0)
        .DefaultIfEmpty(0).Max();
    return $"SUP{(maxSeq + 1):D4}";
}

private async Task<string> GetNextSupplierInvoiceNumber(int year)
{
    var prefix = $"PI/{year}/";
    var existing = await _db.SupplierInvoices
        .Where(i => i.InvoiceNumber.StartsWith(prefix))
        .Select(i => i.InvoiceNumber).ToListAsync();
    int maxSeq = existing.Select(n => int.TryParse(n.Split('/').Last(), out var s) ? s : 0)
        .DefaultIfEmpty(0).Max();
    return $"{prefix}{(maxSeq + 1):D5}";
}

private async Task<string> GetNextSupplierPaymentNumber(int year)
{
    var prefix = $"SP/{year}/";
    var existing = await _db.SupplierPayments
        .Where(p => p.PaymentNumber.StartsWith(prefix))
        .Select(p => p.PaymentNumber).ToListAsync();
    int maxSeq = existing.Select(n => int.TryParse(n.Split('/').Last(), out var s) ? s : 0)
        .DefaultIfEmpty(0).Max();
    return $"{prefix}{(maxSeq + 1):D5}";
}

private async Task<string> GetNextExpenseNumber(int year)
{
    var prefix = $"EXP/{year}/";
    var existing = await _db.Expenses
        .Where(e => e.ExpenseNumber.StartsWith(prefix))
        .Select(e => e.ExpenseNumber).ToListAsync();
    int maxSeq = existing.Select(n => int.TryParse(n.Split('/').Last(), out var s) ? s : 0)
        .DefaultIfEmpty(0).Max();
    return $"{prefix}{(maxSeq + 1):D5}";
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