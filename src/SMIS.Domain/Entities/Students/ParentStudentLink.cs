using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Students;

/// <summary>
/// Links a parent's Identity account to one or more Student records.
/// A parent may have multiple children; a student could have more than
/// one linked parent account (e.g. mother and father separately).
/// </summary>
public class ParentStudentLink : AuditableEntity
{
    public string ParentUserId { get; set; } = null!; // IdentityUser.Id
    public int StudentId { get; set; }
    public string Relationship { get; set; } = "Parent"; // e.g. Father, Mother, Guardian

    public Student Student { get; set; } = null!;
}