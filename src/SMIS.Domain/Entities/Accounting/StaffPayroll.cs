using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Accounting;

/// <summary>
/// Staff salary configuration
/// </summary>
public class StaffPayroll : AuditableEntity
{
    public string StaffNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? KRAPin { get; set; }
    public string? NSSFNumber { get; set; }
    public string? shaNumber { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? BankBranch { get; set; }
    public StaffPayrollType StaffType { get; set; }
    public Domain.Enums.EmploymentType EmploymentType { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal HouseAllowance { get; set; } = 0;
    public decimal TransportAllowance { get; set; } = 0;
    public decimal MedicalAllowance { get; set; } = 0;
    public decimal OtherAllowances { get; set; } = 0;
    public decimal SACCODeduction { get; set; } = 0;
    public decimal LoanDeduction { get; set; } = 0;
    public decimal OtherDeductions { get; set; } = 0;
    public new bool IsActive { get; set; } = true;
public string? Email { get; set; }
    public decimal GrossSalary =>
        BasicSalary + HouseAllowance + TransportAllowance +
        MedicalAllowance + OtherAllowances;
}

public enum StaffPayrollType
{
    Teaching = 1,
    NonTeaching = 2,
    Support = 3,
    Management = 4
}