namespace SMIS.Web.Controllers;

public class StudentLedgerEntry
{
    public DateTime Date { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string Type { get; set; } = string.Empty;
}