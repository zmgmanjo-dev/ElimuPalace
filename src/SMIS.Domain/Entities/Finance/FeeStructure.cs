using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Finance;

/// <summary>
/// Defines how much a specific class pays for a specific fee category,
/// per term — e.g. Grade 1 pays KES 5,000 Tuition Fee in Term 1,
/// but KES 4,500 in Term 2.
/// </summary>
public class FeeStructure : AuditableEntity
{
    public int FeeCategoryId { get; set; }
    public int ClassLevelId { get; set; }
    public int AcademicYearId { get; set; }
    public int AcademicTermId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public FeeCategory FeeCategory { get; set; } = null!;
    public School.ClassLevel ClassLevel { get; set; } = null!;
    public School.AcademicYear AcademicYear { get; set; } = null!;
    public School.AcademicTerm AcademicTerm { get; set; } = null!;
}