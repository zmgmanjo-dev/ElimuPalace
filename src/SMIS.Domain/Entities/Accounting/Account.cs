using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Accounting;

/// <summary>
/// Chart of Accounts — every financial account in the school
/// </summary>
public class Account : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public AccountCategory Category { get; set; }
    public int? ParentAccountId { get; set; }
    public bool IsControlAccount { get; set; } = false;
    public bool AllowDirectEntry { get; set; } = true;
    public string? Description { get; set; }
    public decimal OpeningBalance { get; set; } = 0;
    public BalanceSide NormalBalance { get; set; }
    public string? BankName { get; set; }
public string? AccountNumber { get; set; }

    // Navigation
    public Account? ParentAccount { get; set; }
    public ICollection<Account> SubAccounts { get; set; }
        = new List<Account>();
    public ICollection<JournalEntryLine> JournalLines { get; set; }
        = new List<JournalEntryLine>();
}

public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Income = 4,
    Expense = 5
}

public enum AccountCategory
{
    // Assets
    CurrentAsset = 101,
    FixedAsset = 102,
    BankCash = 103,
    AccountsReceivable = 104,

    // Liabilities
    CurrentLiability = 201,
    LongTermLiability = 202,
    AccountsPayable = 203,

    // Equity
    Capital = 301,
    RetainedEarnings = 302,

    // Income
    FeeIncome = 401,
    OtherIncome = 402,

    // Expenses
    StaffCosts = 501,
    OperatingExpenses = 502,
    AdministrativeExpenses = 503,
    OtherExpenses = 504
}

public enum BalanceSide
{
    Debit = 1,
    Credit = 2
}