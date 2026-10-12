using SMIS.Domain.Common;
using SMIS.Domain.Enums;

namespace SMIS.Domain.Entities.Students;

/// <summary>
/// One record per student per academic year — preserves class history
/// even after the student is promoted, repeats, or graduates.
/// </summary>
public class StudentClassHistory : AuditableEntity
{
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int AcademicYearId { get; set; }
    public School.AcademicYear? AcademicYear { get; set; }

    public int ClassLevelId { get; set; }
    public School.ClassLevel? ClassLevel { get; set; }

    public int? ClassStreamId { get; set; }
    public School.ClassStream? ClassStream { get; set; }

    public PromotionOutcome Outcome { get; set; }
}