using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Academic;

/// <summary>
/// Records daily attendance for each student
/// </summary>
public class AttendanceRecord : AuditableEntity
{
    public int StudentId { get; set; }
    public int ClassLevelId { get; set; }
    public int? ClassStreamId { get; set; }
    public int AcademicTermId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? ExcuseReason { get; set; }
    public TimeSpan? ArrivalTime { get; set; }

    // Navigation
    public Students.Student Student { get; set; } = null!;
    public School.ClassLevel ClassLevel { get; set; } = null!;
    public School.ClassStream? ClassStream { get; set; }
    public School.AcademicTerm AcademicTerm { get; set; } = null!;
}

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    Excused = 4
}