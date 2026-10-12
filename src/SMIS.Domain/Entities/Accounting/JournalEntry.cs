using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Accounting;

/// <summary>
/// A journal entry — the core of double-entry bookkeeping
/// Every entry has lines that must balance (Total Debits = Total Credits)
/// </summary>
public class JournalEntry : AuditableEntity
{
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public JournalEntryType EntryType { get; set; }
    public JournalEntryStatus Status { get; set; }
        = JournalEntryStatus.Draft;
    public string? Reference { get; set; }
    public string? PostedBy { get; set; }
    public DateTime? PostedAt { get; set; }
    public int? AcademicTermId { get; set; }
    public int? AcademicYearId { get; set; }

    // Navigation
    public ICollection<JournalEntryLine> Lines { get; set; }
        = new List<JournalEntryLine>();
    public School.AcademicTerm? AcademicTerm { get; set; }

    // Computed
    public decimal TotalDebits =>
        Lines.Sum(l => l.DebitAmount);
    public decimal TotalCredits =>
        Lines.Sum(l => l.CreditAmount);
    public bool IsBalanced =>
        TotalDebits == TotalCredits;
}

public enum JournalEntryType
{
    General = 1,
    FeeCollection = 2,
    Expense = 3,
    Payroll = 4,
    OpeningBalance = 5,
    Adjustment = 6,
    Reversal = 7
}

public enum JournalEntryStatus
{
    Draft = 1,
    Posted = 2,
    Reversed = 3
}