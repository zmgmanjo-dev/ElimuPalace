using SMIS.Domain.Common;
using SMIS.Domain.Enums;

namespace SMIS.Domain.Entities.Students;

/// <summary>
/// Core student entity — one record per enrolled learner
/// </summary>
public class Student : AuditableEntity
{
    // ── Admission ────────────────────────────────────────────────
    public string AdmissionNumber { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public StudentStatus Status { get; set; } = StudentStatus.Active;

    // ── Personal Details ─────────────────────────────────────────
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? BirthCertificateNumber { get; set; }
    public string? NationalIdNumber { get; set; }
    public string? PassportNumber { get; set; }
    public string? PhotoPath { get; set; }
    public string? Nationality { get; set; } = "Kenyan";
    public string? Religion { get; set; }

    // ── Contact ──────────────────────────────────────────────────
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? PostalAddress { get; set; }
    public string? County { get; set; }
    public string? SubCounty { get; set; }

    // ── Class Allocation ─────────────────────────────────────────
    public int? ClassLevelId { get; set; }
    public int? ClassStreamId { get; set; }

    // ── Parent / Guardian ────────────────────────────────────────
    public string FatherName { get; set; } = string.Empty;
    public string? FatherPhone { get; set; }
    public string? FatherEmail { get; set; }
    public string? FatherOccupation { get; set; }
    public string MotherName { get; set; } = string.Empty;
    public string? MotherPhone { get; set; }
    public string? MotherEmail { get; set; }
    public string? MotherOccupation { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianRelationship { get; set; }

    // ── Medical ──────────────────────────────────────────────────
    public string? BloodGroup { get; set; }
    public string? MedicalConditions { get; set; }
    public string? Allergies { get; set; }
    public bool HasSpecialNeeds { get; set; } = false;
    public string? SpecialNeedsDescription { get; set; }

    // ── Previous School ──────────────────────────────────────────
    public string? PreviousSchool { get; set; }
    public string? PreviousClass { get; set; }
    public string? LeaveReason { get; set; }

    // ── Navigation ───────────────────────────────────────────────
    public School.ClassLevel? ClassLevel { get; set; }
    public School.ClassStream? ClassStream { get; set; }

    // ── Computed ─────────────────────────────────────────────────
    public string FullName => $"{FirstName} {MiddleName} {LastName}".Trim();
    public int Age => DateTime.Today.Year - DateOfBirth.Year -
        (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);
}