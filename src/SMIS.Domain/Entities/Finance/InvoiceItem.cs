using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Finance;

/// <summary>
/// Individual line items on a student invoice
/// e.g. Tuition Fee - KES 5,000
/// </summary>
public class InvoiceItem : AuditableEntity
{
    public int StudentInvoiceId { get; set; }
    public int FeeCategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Discount { get; set; } = 0;
    public decimal NetAmount => Amount - Discount;

    // Navigation
    public StudentInvoice StudentInvoice { get; set; } = null!;
    public FeeCategory FeeCategory { get; set; } = null!;
}