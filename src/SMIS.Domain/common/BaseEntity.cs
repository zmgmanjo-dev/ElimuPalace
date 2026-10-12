namespace SMIS.Domain.Common;

/// <summary>
/// Base class for all entities in the system.
/// Every database table will inherit from this class,
/// giving them automatic Id, audit trail and soft delete fields.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for every record (auto-generated)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// When this record was first created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created this record (username)
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When this record was last modified
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Who last modified this record (username)
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Soft delete flag - when true, record is hidden but not erased from database
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// When this record was deleted (if soft deleted)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Who deleted this record
    /// </summary>
    public string? DeletedBy { get; set; }
}