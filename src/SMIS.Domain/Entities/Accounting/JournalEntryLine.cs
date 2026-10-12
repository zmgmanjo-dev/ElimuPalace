using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Accounting;

/// <summary>
/// Individual debit or credit line within a journal entry
/// </summary>
public class JournalEntryLine : BaseEntity
{
    public int JournalEntryId { get; set; }
    public int AccountId { get; set; }
    public decimal DebitAmount { get; set; } = 0;
    public decimal CreditAmount { get; set; } = 0;
    public string? Narration { get; set; }
    public int LineNumber { get; set; }

    // Navigation
    public JournalEntry JournalEntry { get; set; } = null!;
    public Account Account { get; set; } = null!;
}