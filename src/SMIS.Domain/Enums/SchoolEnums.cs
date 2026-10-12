namespace SMIS.Domain.Enums;

/// <summary>
/// Type of school ownership
/// </summary>
public enum SchoolType
{
    Public = 1,
    Private = 2,
    International = 3,
    Special = 4
}

/// <summary>
/// School gender category
/// </summary>
public enum SchoolCategory
{
    Mixed = 1,
    Boys = 2,
    Girls = 3
}

/// <summary>
/// CBC education levels supported by this system
/// </summary>
public enum SchoolLevel
{
    PrePrimary = 1,      // PP1, PP2
    LowerPrimary = 2,    // Grade 1-3
    UpperPrimary = 3,    // Grade 4-6
    JuniorSchool = 4,    // Grade 7-9
    SeniorSchool = 5     // Grade 10-12
}

/// <summary>
/// Academic term within a year
/// </summary>
public enum TermNumber
{
    Term1 = 1,
    Term2 = 2,
    Term3 = 3
}

/// <summary>
/// Gender options for students and staff
/// </summary>
public enum Gender
{
    Male = 1,
    Female = 2
}

/// <summary>
/// CBC Performance levels used in assessment
/// </summary>
public enum PerformanceLevel
{
    BelowExpectation = 1,   // BE
    ApproachingExpectation = 2, // AE
    MeetExpectation = 3,    // ME
    ExceedsExpectation = 4  // EE
}

/// <summary>
/// Student enrollment status
/// </summary>
public enum StudentStatus
{
    Active = 1,
    Transferred = 2,
    Withdrawn = 3,
    Graduated = 4,
    Deceased = 5,
    Suspended = 6
}

/// <summary>
/// Staff employment type
/// </summary>
public enum EmploymentType
{
    TSC = 1,        // Teachers Service Commission
    BOM = 2,        // Board of Management
    Contract = 3,
    Volunteer = 4
}

/// <summary>
/// Outcome of a student's year-end class transition
/// </summary>
public enum PromotionOutcome
{
    Promoted = 1,
    Repeated = 2,
    Graduated = 3
}