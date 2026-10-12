using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.School;

/// <summary>
/// Represents a stream within a class level
/// e.g. Grade 1 EAST, Grade 1 WEST, Form 1 NORTH
/// A class level can have multiple streams
/// </summary>
public class ClassStream : AuditableEntity
{
    public int ClassLevelId { get; set; }
    public string Name { get; set; } = string.Empty;      // e.g. "East"
    public string FullName { get; set; } = string.Empty;  // e.g. "Grade 1 East"
    public int Capacity { get; set; } = 45;               // Max students per stream

    // Navigation
    public ClassLevel ClassLevel { get; set; } = null!;
}