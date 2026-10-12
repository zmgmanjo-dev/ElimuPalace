using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Finance;

/// <summary>
/// Records a payment made by a student towards their invoice
/// </summary>
public class FeePayment : AuditableEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public int StudentInvoiceId { get; set; }
    public int StudentId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }  // M-Pesa code, cheque no, etc.
    public string? BankName { get; set; }
    public string? Notes { get; set; }
    public bool IsReversed { get; set; } = false;
    public int? JournalEntryId { get; set; }

    // Navigation
    public StudentInvoice StudentInvoice { get; set; } = null!;
    public Students.Student Student { get; set; } = null!;
    public SMIS.Domain.Entities.Accounting.JournalEntry? JournalEntry { get; set; }
}

public enum PaymentMethod
{
    Cash = 1,
    MPesa = 2,
    BankTransfer = 3,
    Cheque = 4,
    Online = 5
}