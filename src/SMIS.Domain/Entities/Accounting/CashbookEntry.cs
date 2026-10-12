using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Accounting;

public class CashbookEntry : AuditableEntity
{
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public CashbookEntryType EntryType { get; set; }

    public int CashAccountId { get; set; }
    public int ContraAccountId { get; set; }

    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? PayeePayer { get; set; }
    public int? JournalEntryId { get; set; }
    public int? AcademicTermId { get; set; }

    // Navigation
    public Account CashAccountRef { get; set; } = null!;
    public Account ContraAccount { get; set; } = null!;
    public JournalEntry? JournalEntry { get; set; }
    public School.AcademicTerm? AcademicTerm { get; set; }
}

public enum CashbookEntryType
{
    Receipt = 1,
    Payment = 2
}