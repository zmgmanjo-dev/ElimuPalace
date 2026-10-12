using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Communication;

/// <summary>
/// A private message thread between a parent and staff, scoped to one student.
/// </summary>
public class StudentFeedback : AuditableEntity
{
    public int StudentId { get; set; }
    public string SenderUserId { get; set; } = null!; // IdentityUser.Id (parent or staff)
    public bool IsFromStaff { get; set; }
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; } = false;

    public Students.Student Student { get; set; } = null!;
}