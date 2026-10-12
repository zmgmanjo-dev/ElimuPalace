using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Accounting;

/// <summary>
/// A payment made TO a supplier
/// </summary>
public class SupplierPayment : AuditableEntity
{
    public string PaymentNumber { get; set; } = string.Empty;
    public int SupplierInvoiceId { get; set; }
    public int SupplierId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public Domain.Entities.Finance.PaymentMethod PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? BankName { get; set; }
    public int PaidFromAccountId { get; set; }
public Account PaidFromAccount { get; set; } = null!;
    public int? JournalEntryId { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public SupplierInvoice SupplierInvoice { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public JournalEntry? JournalEntry { get; set; }
}