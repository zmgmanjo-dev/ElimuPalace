using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Academic;

/// <summary>
/// Stores the end of term CBC report card for a student
/// </summary>
public class CBCReportCard : AuditableEntity
{
    public int StudentId { get; set; }
    public int AcademicTermId { get; set; }
    public int AcademicYearId { get; set; }
    public int ClassLevelId { get; set; }

    // Overall Performance
    public string? ClassTeacherRemarks { get; set; }
    public string? PrincipalRemarks { get; set; }

    // Attendance Summary
    public int DaysPresent { get; set; }
    public int DaysAbsent { get; set; }
    public int TotalSchoolDays { get; set; }

    // Conduct & Behaviour
    public string? Conduct { get; set; }
    public string? Punctuality { get; set; }
    public string? Neatness { get; set; }

    // Extra Curricular
    public string? SportActivities { get; set; }
    public string? ClubActivities { get; set; }
    public string? Talents { get; set; }

    // Next Term Info
    public DateTime? NextTermBegins { get; set; }
    public string? NextClassPromotion { get; set; }
    public bool IsPromoted { get; set; } = true;
    public bool IsPublished { get; set; } = false;

    // Navigation
    public Students.Student Student { get; set; } = null!;
    public School.AcademicTerm AcademicTerm { get; set; } = null!;
    public School.AcademicYear AcademicYear { get; set; } = null!;
    public School.ClassLevel ClassLevel { get; set; } = null!;
    public ICollection<CBCAssessment> Assessments { get; set; }
        = new List<CBCAssessment>();
}