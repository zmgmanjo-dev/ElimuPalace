using SMIS.Domain.Common;
using SMIS.Domain.Enums;

namespace SMIS.Domain.Entities.Academic;

/// <summary>
/// Records CBC assessment for a student per subject per term
/// Uses the 4-level CBC performance scale:
/// BE = Below Expectation, AE = Approaching Expectation,
/// ME = Meets Expectation, EE = Exceeds Expectation
/// </summary>
public class CBCAssessment : AuditableEntity
{
    public int StudentId { get; set; }
    public int SubjectId { get; set; }
    public int AcademicTermId { get; set; }
    public int AcademicYearId { get; set; }
    public int ClassLevelId { get; set; }

    // CBC Performance Levels
    public PerformanceLevel PerformanceLevel { get; set; }

    // Assessment scores (optional - for schools that use marks)
    public decimal? Score { get; set; }
    public decimal? MaxScore { get; set; }

    // Teacher observations
    public string? TeacherRemarks { get; set; }

    // Strands assessed
    public string? StrandsAssessed { get; set; }

    // Assessment type
    public AssessmentType AssessmentType { get; set; }

    // Navigation
    public Students.Student Student { get; set; } = null!;
    public School.Subject Subject { get; set; } = null!;
    public School.AcademicTerm AcademicTerm { get; set; } = null!;
    public School.AcademicYear AcademicYear { get; set; } = null!;
    public School.ClassLevel ClassLevel { get; set; } = null!;
}

public enum AssessmentType
{
    Formative = 1,      // Ongoing assessment
    Summative = 2,      // End of term
    Project = 3,        // Project work
    Practical = 4,      // Practical assessment
    Portfolio = 5       // Portfolio assessment
}