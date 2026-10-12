using SMIS.Domain.Common;
using SMIS.Domain.Enums;

namespace SMIS.Domain.Entities.School;

/// <summary>
/// Represents a subject or learning area in the CBC curriculum
/// e.g. Mathematics, English, Kiswahili, Creative Arts
/// </summary>
public class Subject : AuditableEntity
{
    public string Name { get; set; } = string.Empty;        // e.g. "Mathematics"
    public string Code { get; set; } = string.Empty;        // e.g. "MATH"
    public string? Description { get; set; }
    public SchoolLevel Level { get; set; }                  // Which level this subject belongs to
    public bool IsCompulsory { get; set; } = true;          // Compulsory or optional
    public bool IsCBCLearningArea { get; set; } = true;     // CBC learning area or traditional subject
    public int OrderIndex { get; set; }                     // Display order
}