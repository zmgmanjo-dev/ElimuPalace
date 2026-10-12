using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Finance;

/// <summary>
/// Represents a fee invoice generated for a student per term
/// </summary>
public class StudentInvoice : AuditableEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public int AcademicYearId { get; set; }
    public int AcademicTermId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance => TotalAmount - TotalPaid;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    public string? Notes { get; set; }

    // Navigation
    public Students.Student Student { get; set; } = null!;
    public School.AcademicYear AcademicYear { get; set; } = null!;
    public School.AcademicTerm AcademicTerm { get; set; } = null!;
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<FeePayment> Payments { get; set; } = new List<FeePayment>();
}

public enum InvoiceStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overpaid = 4,
    Waived = 5
}