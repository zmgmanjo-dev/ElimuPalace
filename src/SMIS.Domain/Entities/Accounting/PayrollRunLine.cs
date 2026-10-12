using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Accounting;

public class PayrollRunLine : BaseEntity
{
    public int PayrollRunId { get; set; }
    public int StaffPayrollId { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal HouseAllowance { get; set; }
    public decimal TransportAllowance { get; set; }
    public decimal MedicalAllowance { get; set; }
    public decimal OtherAllowances { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal PAYE { get; set; }
    public decimal NSSFEmployee { get; set; }
    public decimal NSSFEmployer { get; set; }
    public decimal SHAEmployee { get; set; }
    public decimal SHAEmployer { get; set; }
    public decimal SACCODeduction { get; set; }
    public decimal LoanDeduction { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal TotalDeductions =>
        PAYE + NSSFEmployee + SHAEmployee +
        SACCODeduction + LoanDeduction + OtherDeductions;
    public decimal NetPay => GrossSalary - TotalDeductions;

    // Navigation
    public PayrollRun PayrollRun { get; set; } = null!;
    public StaffPayroll StaffPayroll { get; set; } = null!;
}