using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Library;

/// <summary>
/// Records when a book is issued to a student, staff member, or visitor
/// </summary>
public class BookIssue : AuditableEntity
{
    public int BookId { get; set; }

    // Student borrower
    public int? StudentId { get; set; }

    // Staff borrower — StaffPayrollId is the new, correct way (dropdown-driven).
    // StaffName/StaffId are kept for older records created before this change.
    public int? StaffPayrollId { get; set; }
    public string? StaffName { get; set; }
    public string? StaffId { get; set; }

    // Visitor borrower — a one-off person not in Students or StaffPayrolls
    public string? VisitorName { get; set; }
    public string? VisitorPosition { get; set; }
    public string? VisitorIdNumber { get; set; }

    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public BookIssueStatus Status { get; set; } = BookIssueStatus.Issued;
    public decimal? FineAmount { get; set; }
    public bool FinePaid { get; set; } = false;
    public string? Notes { get; set; }
    public BorrowerType BorrowerType { get; set; }

    // Navigation
    public Book Book { get; set; } = null!;
    public Students.Student? Student { get; set; }
    public SMIS.Domain.Entities.Accounting.StaffPayroll? StaffPayroll { get; set; }
}

public enum BookIssueStatus
{
    Issued = 1,
    Returned = 2,
    Overdue = 3,
    Lost = 4
}

public enum BorrowerType
{
    Student = 1,
    Staff = 2,
    Visitor = 3
}