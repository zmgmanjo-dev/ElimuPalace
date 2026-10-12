using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.School;

/// <summary>
/// Represents one academic year e.g. 2024/2025
/// A school can have many academic years but only one is current
/// </summary>
public class AcademicYear : AuditableEntity
{
    public string Name { get; set; } = string.Empty;  // e.g. "2024/2025"
    public int Year { get; set; }                      // e.g. 2024
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; } = false;

    // Navigation - one academic year has many terms
    public ICollection<AcademicTerm> Terms { get; set; } = new List<AcademicTerm>();
}