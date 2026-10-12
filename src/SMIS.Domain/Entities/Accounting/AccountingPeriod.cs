using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Accounting;

/// <summary>
/// An accounting period (month/term) that can be opened or closed
/// Once closed, no entries can be posted to it
/// </summary>
public class AccountingPeriod : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public bool IsClosed { get; set; } = false;
    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
}