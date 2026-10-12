using SMIS.Domain.Common;
using SMIS.Domain.Enums;

namespace SMIS.Domain.Entities.School;

/// <summary>
/// Represents a class level in the school
/// e.g. Grade 1, Grade 7, PP1, PP2
/// </summary>
public class ClassLevel : AuditableEntity
{
    public string Name { get; set; } = string.Empty;       // e.g. "Grade 1"
    public string ShortName { get; set; } = string.Empty;  // e.g. "G1"
    public SchoolLevel Level { get; set; }                 // PrePrimary, LowerPrimary etc
    public int OrderIndex { get; set; }                    // For sorting: 1, 2, 3...

    // Navigation - one class level has many streams
    public ICollection<ClassStream> Streams { get; set; } = new List<ClassStream>();
}