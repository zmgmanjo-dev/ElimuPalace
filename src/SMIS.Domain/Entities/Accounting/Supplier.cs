using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Accounting;

/// <summary>
/// A supplier/vendor that the school purchases from
/// </summary>
public class Supplier : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? KRAPin { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public SupplierCategory Category { get; set; }
    public decimal CreditLimit { get; set; } = 0;
    public int PaymentTermsDays { get; set; } = 30;

    // Navigation
    public ICollection<SupplierInvoice> Invoices { get; set; }
        = new List<SupplierInvoice>();
}

public enum SupplierCategory
{
    StationerySupplier = 1,
    FoodSupplier = 2,
    MaintenanceContractor = 3,
    UtilityProvider = 4,
    ITSupplier = 5,
    TransportProvider = 6,
    Other = 7
}