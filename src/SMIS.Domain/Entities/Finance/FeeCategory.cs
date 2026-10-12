using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Finance;

/// <summary>
/// Defines types of fees charged by the school
/// e.g. Tuition Fee, Boarding Fee, Uniform Fee
/// </summary>
public class FeeCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRecurring { get; set; } = true; // charged every term
    public int OrderIndex { get; set; }

    // Which ledger account this category posts to when a payment is
    // recorded. Null means it falls back to the default Fee Income
    // account (code 4100). Most categories point at an Income account;
    // a category like a refundable deposit should point at a Liability
    // account instead, since it isn't earned income yet.
    public int? IncomeAccountId { get; set; }

    // Navigation
    public ICollection<FeeStructure> FeeStructures { get; set; } = new List<FeeStructure>();
    public SMIS.Domain.Entities.Accounting.Account? IncomeAccount { get; set; }
}