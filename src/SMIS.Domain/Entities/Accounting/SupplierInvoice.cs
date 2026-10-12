using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Accounting;

/// <summary>
/// An invoice received FROM a supplier (accounts payable)
/// </summary>
public class SupplierInvoice : AuditableEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string SupplierInvoiceRef { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance => TotalAmount - TotalPaid;
    public SupplierInvoiceStatus Status { get; set; }
        = SupplierInvoiceStatus.Unpaid;
    public string? Description { get; set; }
    public int? ExpenseAccountId { get; set; }
    public int? JournalEntryId { get; set; }

    // Navigation
    public Supplier Supplier { get; set; } = null!;
    public Account? ExpenseAccount { get; set; }
    public JournalEntry? JournalEntry { get; set; }
    public ICollection<SupplierPayment> Payments { get; set; }
        = new List<SupplierPayment>();
}

public enum SupplierInvoiceStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Cancelled = 4
}