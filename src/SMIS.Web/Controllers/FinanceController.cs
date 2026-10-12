using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Finance;
using SMIS.Infrastructure.Data;
using SMIS.Domain.Entities.Accounting;

namespace SMIS.Web.Controllers;

[Authorize(Roles = "Administrator,Bursar")]
public class FinanceController : BaseController
{
    private new readonly ApplicationDbContext _db;

    public FinanceController(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    // ── FEE CATEGORIES ───────────────────────────────────────────

    // GET: /Finance/FeeCategories
    public async Task<IActionResult> FeeCategories()
    {
        var categories = await _db.FeeCategories
            .Include(c => c.IncomeAccount)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();

        ViewBag.LinkableAccounts = await _db.Accounts
            .Where(a => a.AllowDirectEntry && a.IsActive &&
                        (a.AccountType == AccountType.Income || a.AccountType == AccountType.Liability))
            .OrderBy(a => a.Code)
            .ToListAsync();

        return View(categories);
    }

    // POST: /Finance/CreateFeeCategory
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFeeCategory(string name,
        string? description, bool isRecurring, int? incomeAccountId)
    {
        var category = new FeeCategory
        {
            Name = name,
            Description = description,
            IsRecurring = isRecurring,
            IncomeAccountId = incomeAccountId,
            OrderIndex = await _db.FeeCategories.CountAsync() + 1,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };
        _db.FeeCategories.Add(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Fee category '{name}' created successfully!";
        return RedirectToAction(nameof(FeeCategories));
    }

    // POST: /Finance/SeedDefaultCategories
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedDefaultCategories()
    {
        if (await _db.FeeCategories.AnyAsync())
        {
            TempData["Error"] = "Fee categories already exist!";
            return RedirectToAction(nameof(FeeCategories));
        }

        var categories = new List<FeeCategory>
        {
            new FeeCategory { Name = "Tuition Fee", IsRecurring = true,
                Description = "Main school fees", OrderIndex = 1,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new FeeCategory { Name = "Activity Fee", IsRecurring = true,
                Description = "Sports and co-curricular activities", OrderIndex = 2,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new FeeCategory { Name = "Boarding Fee", IsRecurring = true,
                Description = "Hostel and meals", OrderIndex = 3,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new FeeCategory { Name = "Uniform Fee", IsRecurring = false,
                Description = "School uniform", OrderIndex = 4,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new FeeCategory { Name = "Books & Stationery", IsRecurring = false,
                Description = "Textbooks and exercise books", OrderIndex = 5,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new FeeCategory { Name = "Examination Fee", IsRecurring = true,
                Description = "Internal and external exams", OrderIndex = 6,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new FeeCategory { Name = "Computer Fee", IsRecurring = true,
                Description = "ICT lab usage", OrderIndex = 7,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
            new FeeCategory { Name = "Transport Fee", IsRecurring = true,
                Description = "School bus", OrderIndex = 8,
                CreatedBy = "system", CreatedAt = DateTime.UtcNow },
        };

        _db.FeeCategories.AddRange(categories);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Default fee categories created successfully!";
        return RedirectToAction(nameof(FeeCategories));
    }

    // ── EDIT / DEACTIVATE / DELETE: FEE CATEGORIES ───────────────

    // POST: /Finance/EditFeeCategory
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFeeCategory(int id, string name,
        string? description, bool isRecurring, int orderIndex, int? incomeAccountId)
    {
        var category = await _db.FeeCategories.FindAsync(id);
        if (category == null) return NotFound();

        name = (name ?? "").Trim();
        if (name.Length == 0)
        {
            TempData["Error"] = "Category name cannot be empty.";
            return RedirectToAction(nameof(FeeCategories));
        }

        if (await _db.FeeCategories.AnyAsync(c => c.Id != id && c.Name == name))
        {
            TempData["Error"] = $"Another category is already named '{name}'.";
            return RedirectToAction(nameof(FeeCategories));
        }

        var oldName = category.Name;
        category.Name = name;
        category.Description = description;
        category.IsRecurring = isRecurring;
        category.OrderIndex = orderIndex;
        category.IncomeAccountId = incomeAccountId;
        category.UpdatedBy = User.Identity?.Name ?? "system";
        category.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Fee category '{name}' updated.";
        await LogAudit("Finance", "EditFeeCategory", $"Edited fee category '{oldName}' → '{name}'");
        return RedirectToAction(nameof(FeeCategories));
    }

    // POST: /Finance/ToggleFeeCategory
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeeCategory(int id)
    {
        var category = await _db.FeeCategories.FindAsync(id);
        if (category == null) return NotFound();

        category.IsActive = !category.IsActive;
        category.UpdatedBy = User.Identity?.Name ?? "system";
        category.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var state = category.IsActive ? "activated" : "deactivated";
        TempData["Success"] = $"'{category.Name}' {state}.";
        await LogAudit("Finance", "ToggleFeeCategory", $"Fee category '{category.Name}' {state}");
        return RedirectToAction(nameof(FeeCategories));
    }

    // POST: /Finance/DeleteFeeCategory
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFeeCategory(int id)
    {
        var category = await _db.FeeCategories.FindAsync(id);
        if (category == null) return NotFound();

        var structureCount = await _db.FeeStructures.CountAsync(s => s.FeeCategoryId == id);
        var invoiceItemCount = await _db.InvoiceItems.CountAsync(i => i.FeeCategoryId == id);

        if (structureCount > 0 || invoiceItemCount > 0)
        {
            TempData["Error"] = $"'{category.Name}' can't be deleted because it is used in " +
                $"{structureCount} fee structure(s) and {invoiceItemCount} invoice item(s). " +
                "Deactivate it instead to hide it from new invoices.";
            return RedirectToAction(nameof(FeeCategories));
        }

        _db.FeeCategories.Remove(category);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"'{category.Name}' deleted.";
        await LogAudit("Finance", "DeleteFeeCategory", $"Deleted fee category '{category.Name}'");
        return RedirectToAction(nameof(FeeCategories));
    }

    // ── EDIT / DELETE: FEE STRUCTURES ────────────────────────────

    // POST: /Finance/EditFeeStructure
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFeeStructure(int id, decimal amount,
        string? notes, string? returnUrl)
    {
        var structure = await _db.FeeStructures
            .Include(s => s.FeeCategory)
            .Include(s => s.ClassLevel)
            .Include(s => s.AcademicTerm)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (structure == null) return NotFound();

        var oldAmount = structure.Amount;
        structure.Amount = amount;
        structure.Notes = notes;
        structure.UpdatedBy = User.Identity?.Name ?? "system";
        structure.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Fee structure updated. Invoices already generated are not affected.";
        await LogAudit("Finance", "EditFeeStructure",
            $"{structure.ClassLevel.Name} / {structure.FeeCategory.Name} / {structure.AcademicTerm.Name}: " +
            $"KES {oldAmount:N0} → KES {amount:N0}");
        return RedirectBack(returnUrl);
    }

    // POST: /Finance/DeleteFeeStructure
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFeeStructure(int id, string? returnUrl)
    {
        var structure = await _db.FeeStructures
            .Include(s => s.FeeCategory)
            .Include(s => s.ClassLevel)
            .Include(s => s.AcademicTerm)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (structure == null) return NotFound();

        _db.FeeStructures.Remove(structure);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Fee structure deleted. Invoices already generated are not affected.";
        await LogAudit("Finance", "DeleteFeeStructure",
            $"Deleted {structure.ClassLevel.Name} / {structure.FeeCategory.Name} / {structure.AcademicTerm.Name}");
        return RedirectBack(returnUrl);
    }

    private IActionResult RedirectBack(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(FeeStructures));
    }

    // ── FEE STRUCTURES ───────────────────────────────────────────

    // GET: /Finance/FeeStructures
    public async Task<IActionResult> FeeStructures(int? academicYearId,
        int? academicTermId, int? classLevelId)
    {
        var currentTerm = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .FirstOrDefaultAsync(t => t.IsCurrent);

        var query = _db.FeeStructures
            .Include(s => s.FeeCategory)
            .Include(s => s.ClassLevel)
            .Include(s => s.AcademicYear)
            .Include(s => s.AcademicTerm)
            .AsQueryable();

        if (academicYearId.HasValue)
            query = query.Where(s => s.AcademicYearId == academicYearId);
        if (academicTermId.HasValue)
            query = query.Where(s => s.AcademicTermId == academicTermId);
        if (classLevelId.HasValue)
            query = query.Where(s => s.ClassLevelId == classLevelId);

        var structures = await query
            .OrderBy(s => s.AcademicYear.Year)
            .ThenBy(s => s.AcademicTerm.TermNumber)
            .ThenBy(s => s.ClassLevel.OrderIndex)
            .ThenBy(s => s.FeeCategory.OrderIndex)
            .ToListAsync();

        ViewBag.Categories = await _db.FeeCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.OrderIndex).ToListAsync();
        ViewBag.Classes = await _db.ClassLevels
            .Where(c => c.IsActive)
            .OrderBy(c => c.OrderIndex).ToListAsync();
        ViewBag.AcademicYears = await _db.AcademicYears
            .OrderByDescending(y => y.Year).ToListAsync();
        ViewBag.Terms = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .OrderByDescending(t => t.AcademicYear.Year)
            .ThenBy(t => t.TermNumber)
            .ToListAsync();

        ViewBag.SelectedYearId = academicYearId;
        ViewBag.SelectedTermId = academicTermId ?? currentTerm?.Id;
        ViewBag.SelectedClassId = classLevelId;

        return View(structures);
    }

    // POST: /Finance/CreateFeeStructure
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFeeStructure(int feeCategoryId,
        int classLevelId, int academicYearId, int academicTermId,
        decimal amount, string? notes)
    {
        var exists = await _db.FeeStructures.AnyAsync(s =>
            s.FeeCategoryId == feeCategoryId &&
            s.ClassLevelId == classLevelId &&
            s.AcademicYearId == academicYearId &&
            s.AcademicTermId == academicTermId);

        if (exists)
        {
            TempData["Error"] = "A fee structure for this class, category and term already exists!";
            return RedirectToAction(nameof(FeeStructures));
        }

        var structure = new FeeStructure
        {
            FeeCategoryId = feeCategoryId,
            ClassLevelId = classLevelId,
            AcademicYearId = academicYearId,
            AcademicTermId = academicTermId,
            Amount = amount,
            Notes = notes,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        _db.FeeStructures.Add(structure);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Fee structure created successfully!";
        return RedirectToAction(nameof(FeeStructures));
    }

    // ── INVOICES ─────────────────────────────────────────────────

    // GET: /Finance/Invoices
    public async Task<IActionResult> Invoices(int? termId)
    {
        var currentTerm = await _db.AcademicTerms
            .FirstOrDefaultAsync(t => t.IsCurrent);

        var query = _db.StudentInvoices
            .Include(i => i.Student)
                .ThenInclude(s => s.ClassLevel)
            .Include(i => i.AcademicTerm)
            .Include(i => i.AcademicYear)
            .AsQueryable();

        if (termId.HasValue)
            query = query.Where(i => i.AcademicTermId == termId);
        else if (currentTerm != null)
            query = query.Where(i => i.AcademicTermId == currentTerm.Id);

        var invoices = await query
            .OrderBy(i => i.Student.ClassLevel!.OrderIndex)
            .ThenBy(i => i.Student.LastName)
            .ToListAsync();

        ViewBag.Terms = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .OrderByDescending(t => t.AcademicYear.Year)
            .ToListAsync();
        ViewBag.CurrentTermId = termId ?? currentTerm?.Id;
        ViewBag.Students = await _db.Students
            .Include(s => s.ClassLevel)
            .Where(s => s.Status == Domain.Enums.StudentStatus.Active)
            .OrderBy(s => s.ClassLevel!.OrderIndex)
            .ThenBy(s => s.LastName)
            .ToListAsync();
        ViewBag.TotalInvoiced = invoices.Sum(i => i.TotalAmount);
        ViewBag.TotalPaid = invoices.Sum(i => i.TotalPaid);
        ViewBag.TotalBalance = invoices.Sum(i => i.Balance);

        return View(invoices);
    }

    // POST: /Finance/GenerateInvoice
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateInvoice(int studentId, int academicYearId,
        int academicTermId)
    {
        // Check if invoice already exists
        var exists = await _db.StudentInvoices.AnyAsync(i =>
            i.StudentId == studentId &&
            i.AcademicTermId == academicTermId);

        if (exists)
        {
            TempData["Error"] = "Invoice already exists for this student and term!";
            return RedirectToAction(nameof(Invoices));
        }

        var student = await _db.Students
            .Include(s => s.ClassLevel)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
        {
            TempData["Error"] = "Student not found!";
            return RedirectToAction(nameof(Invoices));
        }

        // Get fee structures for this class and year
        var feeStructures = await _db.FeeStructures
            .Include(s => s.FeeCategory)
            .Where(s => s.ClassLevelId == student.ClassLevelId &&
                    s.AcademicYearId == academicYearId &&
                    s.AcademicTermId == academicTermId &&
                    s.FeeCategory.IsActive)
            .ToListAsync();

        if (!feeStructures.Any())
        {
            TempData["Error"] = "No fee structures found for this class. Please set up fee structures first!";
            return RedirectToAction(nameof(Invoices));
        }

        // Generate invoice number
        var invoiceNumber = await GetNextInvoiceNumber(DateTime.Now.Year);

        var invoice = new StudentInvoice
        {
            InvoiceNumber = invoiceNumber,
            StudentId = studentId,
            AcademicYearId = academicYearId,
            AcademicTermId = academicTermId,
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            Status = InvoiceStatus.Unpaid,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        // Create invoice items from fee structures
        var items = feeStructures.Select(fs => new InvoiceItem
        {
            FeeCategoryId = fs.FeeCategoryId,
            Description = fs.FeeCategory.Name,
            Amount = fs.Amount,
            Discount = 0,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        invoice.TotalAmount = items.Sum(i => i.NetAmount);
        invoice.Items = items;

        _db.StudentInvoices.Add(invoice);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Invoice {invoiceNumber} generated for {student.FullName}!";
        return RedirectToAction(nameof(Invoices));
    }

    // GET: /Finance/InvoiceDetails/5
    public async Task<IActionResult> InvoiceDetails(int id)
    {
        var invoice = await _db.StudentInvoices
            .Include(i => i.Student)
                .ThenInclude(s => s.ClassLevel)
            .Include(i => i.AcademicYear)
            .Include(i => i.AcademicTerm)
            .Include(i => i.Items)
                .ThenInclude(item => item.FeeCategory)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return NotFound();
        ViewBag.CashAccounts = await _db.Accounts
            .Where(a => a.Category == SMIS.Domain.Entities.Accounting.AccountCategory.BankCash && a.IsActive)
            .OrderBy(a => a.Code).ToListAsync();
        return View(invoice);
    }

    // ── PAYMENTS ─────────────────────────────────────────────────

    // POST: /Finance/RecordPayment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(int invoiceId, decimal amount,
        PaymentMethod paymentMethod, int cashAccountId,
        string? referenceNumber, string? bankName, string? notes)
    {
        var invoice = await _db.StudentInvoices
            .Include(i => i.Student)
            .Include(i => i.Items).ThenInclude(item => item.FeeCategory)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
        {
            TempData["Error"] = "Invoice not found!";
            return RedirectToAction(nameof(Invoices));
        }

        var cashAccount = await _db.Accounts.FindAsync(cashAccountId);
        var defaultFeeIncomeAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == "4100");

        var receiptNumber = await GetNextReceiptNumber(DateTime.Now.Year);

        JournalEntry? journalEntry = null;
        if (cashAccount != null && defaultFeeIncomeAccount != null)
        {
            // Split this payment across each invoice item's linked account,
            // proportional to that item's share of the invoice total. If the
            // student paid less than the full invoice, each account gets its
            // proportional share of what was actually received, not billed.
            var items = invoice.Items.ToList();
            var allocations = new List<(int AccountId, decimal Amount)>();

            if (items.Any() && invoice.TotalAmount > 0)
            {
                decimal allocated = 0;
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var accountId = item.FeeCategory?.IncomeAccountId ?? defaultFeeIncomeAccount.Id;

                    decimal share;
                    if (i == items.Count - 1)
                        share = amount - allocated; // remainder to the last item, avoids rounding drift
                    else
                    {
                        share = Math.Round(amount * (item.NetAmount / invoice.TotalAmount), 2);
                        allocated += share;
                    }
                    allocations.Add((accountId, share));
                }
            }
            else
            {
                allocations.Add((defaultFeeIncomeAccount.Id, amount));
            }

            var grouped = allocations
                .GroupBy(a => a.AccountId)
                .Select(g => new { AccountId = g.Key, Amount = g.Sum(x => x.Amount) })
                .Where(g => g.Amount != 0)
                .ToList();

            var lines = new List<SMIS.Domain.Entities.Accounting.JournalEntryLine>
            {
                new() { AccountId = cashAccountId, DebitAmount = amount, CreditAmount = 0,
                    Narration = $"Payment — {receiptNumber}", LineNumber = 1 }
            };
            int lineNum = 2;
            foreach (var g in grouped)
            {
                lines.Add(new SMIS.Domain.Entities.Accounting.JournalEntryLine
                {
                    AccountId = g.AccountId,
                    DebitAmount = 0,
                    CreditAmount = g.Amount,
                    Narration = $"Payment — {receiptNumber}",
                    LineNumber = lineNum++
                });
            }

            journalEntry = new JournalEntry
            {
                EntryNumber = await GetNextJournalEntryNumber(DateTime.Now.Year),
                EntryDate = DateTime.Today,
                Description = $"Fee Payment — {invoice.Student.FullName} — {receiptNumber}",
                EntryType = SMIS.Domain.Entities.Accounting.JournalEntryType.FeeCollection,
                Reference = referenceNumber,
                Status = SMIS.Domain.Entities.Accounting.JournalEntryStatus.Posted,
                PostedBy = User.Identity?.Name,
                PostedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow,
                Lines = lines
            };
            _db.JournalEntries.Add(journalEntry);
            await _db.SaveChangesAsync();
        }

        var payment = new FeePayment
        {
            ReceiptNumber = receiptNumber,
            StudentInvoiceId = invoiceId,
            StudentId = invoice.StudentId,
            PaymentDate = DateTime.Today,
            Amount = amount,
            PaymentMethod = paymentMethod,
            ReferenceNumber = referenceNumber,
            BankName = bankName,
            Notes = notes,
            JournalEntryId = journalEntry?.Id,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        };
        _db.FeePayments.Add(payment);

        if (cashAccount != null && defaultFeeIncomeAccount != null)
        {
            _db.CashbookEntries.Add(new SMIS.Domain.Entities.Accounting.CashbookEntry
            {
                EntryNumber = await GetNextCashbookEntryNumber(DateTime.Now.Year),
                EntryDate = DateTime.Today,
                EntryType = SMIS.Domain.Entities.Accounting.CashbookEntryType.Receipt,
                CashAccountId = cashAccountId,
                ContraAccountId = defaultFeeIncomeAccount.Id,
                Amount = amount,
                Description = $"Fee Payment — {invoice.Student.FullName}",
                PayeePayer = invoice.Student.FullName,
                Reference = referenceNumber,
                JournalEntryId = journalEntry?.Id,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow
            });
        }

        invoice.TotalPaid += amount;
        invoice.Status = invoice.TotalPaid >= invoice.TotalAmount
            ? InvoiceStatus.Paid
            : invoice.TotalPaid > 0
                ? InvoiceStatus.PartiallyPaid
                : InvoiceStatus.Unpaid;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Payment of KES {amount:N0} recorded! Receipt: {receiptNumber}";
        TempData["NewReceiptId"] = payment.Id;
        await LogAudit("Finance", "RecordPayment",
            $"Recorded KES {amount:N0} payment for {invoice.Student.FullName} — Receipt {receiptNumber}");
        return RedirectToAction(nameof(InvoiceDetails), new { id = invoiceId });
    }

    // POST: /Finance/BackfillFeePayments
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BackfillFeePayments()
    {
        var cashAccount = await _db.Accounts.FirstOrDefaultAsync(a => a.Code == "1101");
        var feeIncomeAccount = await _db.Accounts.FirstOrDefaultAsync(a => a.Code == "4100");

        if (cashAccount == null || feeIncomeAccount == null)
        {
            TempData["Error"] = "Required accounts (Cash in Hand / Fee Income) not found!";
            return RedirectToAction(nameof(Invoices));
        }

        var unlinkedPayments = await _db.FeePayments
            .Include(p => p.Student)
            .Where(p => p.JournalEntryId == null)
            .ToListAsync();

        int processed = 0;
        foreach (var payment in unlinkedPayments)
        {
            var journalEntry = new JournalEntry
            {
                EntryNumber = await GetNextJournalEntryNumber(payment.PaymentDate.Year),
                EntryDate = payment.PaymentDate,
                Description = $"Fee Payment (backfill) — {payment.Student.FullName} — {payment.ReceiptNumber}",
                EntryType = JournalEntryType.FeeCollection,
                Reference = payment.ReferenceNumber,
                Status = JournalEntryStatus.Posted,
                PostedBy = "system-backfill",
                PostedAt = DateTime.UtcNow,
                CreatedBy = "system-backfill",
                CreatedAt = DateTime.UtcNow,
                Lines = new List<JournalEntryLine>
                {
                    new() { AccountId = cashAccount.Id, DebitAmount = payment.Amount, CreditAmount = 0,
                        Narration = $"Payment — {payment.ReceiptNumber}", LineNumber = 1 },
                    new() { AccountId = feeIncomeAccount.Id, DebitAmount = 0, CreditAmount = payment.Amount,
                        Narration = $"Payment — {payment.ReceiptNumber}", LineNumber = 2 }
                }
            };
            _db.JournalEntries.Add(journalEntry);
            await _db.SaveChangesAsync();

            _db.CashbookEntries.Add(new CashbookEntry
            {
                EntryNumber = await GetNextCashbookEntryNumber(payment.PaymentDate.Year),
                EntryDate = payment.PaymentDate,
                EntryType = CashbookEntryType.Receipt,
                CashAccountId = cashAccount.Id,
                ContraAccountId = feeIncomeAccount.Id,
                Amount = payment.Amount,
                Description = $"Fee Payment (backfill) — {payment.Student.FullName}",
                PayeePayer = payment.Student.FullName,
                Reference = payment.ReferenceNumber,
                JournalEntryId = journalEntry.Id,
                CreatedBy = "system-backfill",
                CreatedAt = DateTime.UtcNow
            });

            payment.JournalEntryId = journalEntry.Id;
            processed++;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Backfilled {processed} historical fee payments into Cashbook/GL!";
        await LogAudit("Finance", "BackfillFeePayments",
            $"Backfilled {processed} historical fee payments into Cashbook/GL");
        return RedirectToAction(nameof(Invoices));
    }

    // POST: /Finance/VoidPayment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VoidPayment(int paymentId)
    {
        var payment = await _db.FeePayments
            .Include(p => p.StudentInvoice)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
        {
            TempData["Error"] = "Payment not found!";
            return RedirectToAction(nameof(Invoices));
        }

        if (payment.IsReversed)
        {
            TempData["Error"] = "This payment has already been voided!";
            return RedirectToAction(nameof(InvoiceDetails), new { id = payment.StudentInvoiceId });
        }

        // Remove the linked cashbook entry, if any
        if (payment.JournalEntryId.HasValue)
        {
            var cashbookEntry = await _db.CashbookEntries
                .FirstOrDefaultAsync(c => c.JournalEntryId == payment.JournalEntryId);
            if (cashbookEntry != null)
                _db.CashbookEntries.Remove(cashbookEntry);

            // Remove the journal entry and its lines
            var journalEntry = await _db.JournalEntries
                .Include(j => j.Lines)
                .FirstOrDefaultAsync(j => j.Id == payment.JournalEntryId);
            if (journalEntry != null)
            {
                _db.JournalEntryLines.RemoveRange(journalEntry.Lines);
                _db.JournalEntries.Remove(journalEntry);
            }
        }

        // Reverse the payment's effect on the invoice
        var invoice = payment.StudentInvoice;
        invoice.TotalPaid -= payment.Amount;
        if (invoice.TotalPaid < 0) invoice.TotalPaid = 0;

        invoice.Status = invoice.TotalPaid >= invoice.TotalAmount
            ? InvoiceStatus.Paid
            : invoice.TotalPaid > 0
                ? InvoiceStatus.PartiallyPaid
                : InvoiceStatus.Unpaid;

        payment.IsReversed = true;
        payment.JournalEntryId = null;
        payment.UpdatedBy = User.Identity?.Name ?? "system";
        payment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] =
            $"Payment {payment.ReceiptNumber} (KES {payment.Amount:N0}) has been voided.";
        await LogAudit("Finance", "VoidPayment",
            $"Voided payment {payment.ReceiptNumber} (KES {payment.Amount:N0}) for invoice #{payment.StudentInvoiceId}");
        return RedirectToAction(nameof(InvoiceDetails), new { id = payment.StudentInvoiceId });
    }

    // GET: /Finance/StudentStatement/5
    public async Task<IActionResult> StudentStatement(int studentId)
    {
        var student = await _db.Students
            .Include(s => s.ClassLevel)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null) return NotFound();

        var invoices = await _db.StudentInvoices
            .Include(i => i.AcademicYear)
            .Include(i => i.AcademicTerm)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.StudentId == studentId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        ViewBag.Student = student;
        ViewBag.TotalInvoiced = invoices.Sum(i => i.TotalAmount);
        ViewBag.TotalPaid = invoices.Sum(i => i.TotalPaid);
        ViewBag.TotalBalance = invoices.Sum(i => i.Balance);

        return View(invoices);
    }

    // ── BULK INVOICE GENERATION ──────────────────────────────────

    // GET: /Finance/BulkInvoice
    public async Task<IActionResult> BulkInvoice()
    {
        ViewBag.Classes = await _db.ClassLevels
            .OrderBy(c => c.OrderIndex).ToListAsync();
        ViewBag.Terms = await _db.AcademicTerms
            .Include(t => t.AcademicYear)
            .OrderByDescending(t => t.AcademicYear.Year)
            .ToListAsync();
        return View();
    }

    // POST: /Finance/GenerateBulkInvoices
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateBulkInvoices(int? classLevelId,
        int academicYearId, int academicTermId)
    {
        // Get students
        var studentsQuery = _db.Students
            .Include(s => s.ClassLevel)
            .Where(s => s.Status == Domain.Enums.StudentStatus.Active);

        if (classLevelId.HasValue)
            studentsQuery = studentsQuery.Where(s => s.ClassLevelId == classLevelId);

        var students = await studentsQuery.ToListAsync();

        if (!students.Any())
        {
            TempData["Error"] = "No active students found for the selected criteria!";
            return RedirectToAction(nameof(BulkInvoice));
        }

        int created = 0;
        int skipped = 0;

        foreach (var student in students)
        {
            // Skip if invoice already exists
            var exists = await _db.StudentInvoices.AnyAsync(i =>
                i.StudentId == student.Id &&
                i.AcademicTermId == academicTermId);

            if (exists) { skipped++; continue; }

            // Get fee structures for this student's class
            var feeStructures = await _db.FeeStructures
                .Include(s => s.FeeCategory)
                .Where(s => s.ClassLevelId == student.ClassLevelId &&
                        s.AcademicYearId == academicYearId &&
                        s.AcademicTermId == academicTermId &&
                        s.FeeCategory.IsActive)
                .ToListAsync();

            if (!feeStructures.Any()) { skipped++; continue; }

            var invoiceNumber = await GetNextInvoiceNumber(DateTime.Now.Year);

            var invoice = new StudentInvoice
            {
                InvoiceNumber = invoiceNumber,
                StudentId = student.Id,
                AcademicYearId = academicYearId,
                AcademicTermId = academicTermId,
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                Status = InvoiceStatus.Unpaid,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow
            };

            var items = feeStructures.Select(fs => new InvoiceItem
            {
                FeeCategoryId = fs.FeeCategoryId,
                Description = fs.FeeCategory.Name,
                Amount = fs.Amount,
                Discount = 0,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow
            }).ToList();

            invoice.TotalAmount = items.Sum(i => i.NetAmount);
            invoice.Items = items;

            _db.StudentInvoices.Add(invoice);
            await _db.SaveChangesAsync();
            created++;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Bulk generation complete! " +
            $"{created} invoices created, {skipped} skipped.";
        return RedirectToAction(nameof(Invoices));
    }

    // ── PDF GENERATION ───────────────────────────────────────────

    // GET: /Finance/DownloadInvoice/5
    public async Task<IActionResult> DownloadInvoice(int id)
    {
        var invoice = await _db.StudentInvoices
            .Include(i => i.Student).ThenInclude(s => s.ClassLevel)
            .Include(i => i.AcademicYear)
            .Include(i => i.AcademicTerm)
            .Include(i => i.Items).ThenInclude(item => item.FeeCategory)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return NotFound();

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        var pdfService = HttpContext.RequestServices
            .GetRequiredService<SMIS.Web.Services.PdfService>();
        var pdf = pdfService.GenerateInvoicePdf(invoice, school);

        return File(pdf, "application/pdf",
            $"Invoice_{invoice.InvoiceNumber}_{invoice.Student.FullName}.pdf");
    }

    // GET: /Finance/DownloadReceipt/5
    public async Task<IActionResult> DownloadReceipt(int id)
    {
        var payment = await _db.FeePayments
            .Include(p => p.Student).ThenInclude(s => s.ClassLevel)
            .Include(p => p.StudentInvoice)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null) return NotFound();

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        var pdfService = HttpContext.RequestServices
            .GetRequiredService<SMIS.Web.Services.PdfService>();
        var pdf = pdfService.GenerateReceiptPdf(payment, school);

        return File(pdf, "application/pdf",
            $"Receipt_{payment.ReceiptNumber}.pdf");
    }

    // GET: /Finance/DownloadBulkInvoices
    public async Task<IActionResult> DownloadBulkInvoices(int? classLevelId, int termId)
    {
        var query = _db.StudentInvoices
            .Include(i => i.Student).ThenInclude(s => s.ClassLevel)
            .Include(i => i.AcademicYear)
            .Include(i => i.AcademicTerm)
            .Include(i => i.Items).ThenInclude(item => item.FeeCategory)
            .Include(i => i.Payments)
            .Where(i => i.AcademicTermId == termId);

        if (classLevelId.HasValue)
            query = query.Where(i => i.Student.ClassLevelId == classLevelId);

        var invoices = await query
            .OrderBy(i => i.Student.ClassLevel!.OrderIndex)
            .ThenBy(i => i.Student.LastName)
            .ToListAsync();

        if (!invoices.Any())
        {
            TempData["Error"] = "No invoices found for the selected criteria!";
            return RedirectToAction(nameof(Invoices));
        }

        var school = await _db.SchoolProfiles.FirstOrDefaultAsync();
        var pdfService = HttpContext.RequestServices
            .GetRequiredService<SMIS.Web.Services.PdfService>();

        // Merge all PDFs into one document
        var allPdfs = invoices.Select(inv =>
            pdfService.GenerateInvoicePdf(inv, school)).ToList();

        // Combine using simple concatenation via QuestPDF
        var mergedPdf = MergePdfs(allPdfs);

        return File(mergedPdf, "application/pdf",
            $"BulkInvoices_{DateTime.Now:yyyyMMdd}.pdf");
    }

    private byte[] MergePdfs(List<byte[]> pdfs)
    {
        // Simple merge - create a new document with all pages
        using var outputStream = new MemoryStream();
        foreach (var pdf in pdfs)
            outputStream.Write(pdf, 0, pdf.Length);
        return outputStream.ToArray();
    }

    private async Task<string> GetNextInvoiceNumber(int year)
    {
        var prefix = $"INV/{year}/";
        var existing = await _db.StudentInvoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .Select(i => i.InvoiceNumber).ToListAsync();
        int maxSeq = existing.Select(n => int.TryParse(n.Split('/').Last(), out var s) ? s : 0)
            .DefaultIfEmpty(0).Max();
        return $"{prefix}{(maxSeq + 1):D4}";
    }

    private async Task<string> GetNextReceiptNumber(int year)
    {
        var prefix = $"RCP/{year}/";
        var existing = await _db.FeePayments
            .Where(p => p.ReceiptNumber.StartsWith(prefix))
            .Select(p => p.ReceiptNumber).ToListAsync();
        int maxSeq = existing.Select(n => int.TryParse(n.Split('/').Last(), out var s) ? s : 0)
            .DefaultIfEmpty(0).Max();
        return $"{prefix}{(maxSeq + 1):D4}";
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