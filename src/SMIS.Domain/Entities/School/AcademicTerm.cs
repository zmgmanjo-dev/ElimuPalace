using SMIS.Domain.Common;
using SMIS.Domain.Enums;

namespace SMIS.Domain.Entities.School;

/// <summary>
/// Represents one term within an academic year
/// e.g. Term 1 of 2024/2025
/// </summary>
public class AcademicTerm : AuditableEntity
{
    public int AcademicYearId { get; set; }
    public TermNumber TermNumber { get; set; }      // Term1, Term2, Term3
    public string Name { get; set; } = string.Empty; // e.g. "Term 1"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; } = false;

    // Navigation
    public AcademicYear AcademicYear { get; set; } = null!;
}