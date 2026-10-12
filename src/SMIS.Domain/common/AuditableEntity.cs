namespace SMIS.Domain.Common;

/// <summary>
/// Extended base class for entities that need 
/// to track who created and modified them.
/// Used for important records like students, fees, exams.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>
    /// Marks whether this record is currently active
    /// Example: a deactivated student, a closed academic year
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional notes or remarks about this record
    /// </summary>
    public string? Remarks { get; set; }
}