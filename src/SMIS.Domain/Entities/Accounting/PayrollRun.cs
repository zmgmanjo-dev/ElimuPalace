using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Accounting;

public class PayrollRun : AuditableEntity
{
    public string RunNumber { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName => new DateTime(Year, Month, 1)
        .ToString("MMMM yyyy");
    public PayrollRunStatus Status { get; set; }
        = PayrollRunStatus.Draft;
    public decimal TotalGross { get; set; }
    public decimal TotalPAYE { get; set; }
    public decimal TotalNSSF { get; set; }
    public decimal TotalSHA { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetPay { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? JournalEntryId { get; set; }

    // Navigation
    public ICollection<PayrollRunLine> Lines { get; set; }
        = new List<PayrollRunLine>();
    public JournalEntry? JournalEntry { get; set; }
}

public enum PayrollRunStatus
{
    Draft = 1,
    Approved = 2,
    Paid = 3
}