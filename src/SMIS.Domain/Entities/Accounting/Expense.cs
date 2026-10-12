using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Accounting;

/// <summary>
/// A direct expense recorded without a supplier invoice
/// e.g. petty cash payments, imprest
/// </summary>
public class Expense : AuditableEntity
{
    public string ExpenseNumber { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public int ExpenseAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Payee { get; set; }
    public string? ReceiptNumber { get; set; }
    public int PaidFromAccountId { get; set; }
public Account PaidFromAccount { get; set; } = null!;
    public int? AcademicTermId { get; set; }
    public int? JournalEntryId { get; set; }
    public bool IsApproved { get; set; } = false;
    public string? ApprovedBy { get; set; }

    // Navigation
    public Account ExpenseAccount { get; set; } = null!;
    public JournalEntry? JournalEntry { get; set; }
    public School.AcademicTerm? AcademicTerm { get; set; }
}