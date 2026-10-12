using SMIS.Domain.Common;
using SMIS.Domain.Enums;

namespace SMIS.Domain.Entities.School;

/// <summary>
/// Stores all information about the school.
/// Only one record exists per installation.
/// </summary>
public class SchoolProfile : AuditableEntity
{
    // Basic Identity
    public string Name { get; set; } = string.Empty;
    public string KemisCode { get; set; } = string.Empty;
    public string Motto { get; set; } = string.Empty;
    public string? LogoPath { get; set; }

    // Classification
    public SchoolLevel Level { get; set; }
    public SchoolCategory Category { get; set; }
    public SchoolType Type { get; set; }

    // Location
    public string County { get; set; } = string.Empty;
    public string SubCounty { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string PhysicalAddress { get; set; } = string.Empty;
    public string PostalAddress { get; set; } = string.Empty;

    // Contacts
    public string PhoneNumber { get; set; } = string.Empty;
    public string? AlternativePhone { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Website { get; set; }

    // Registration
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Principal { get; set; } = string.Empty;
    public string? BoardChairperson { get; set; }
    public DateTime? EstablishedDate { get; set; }

    // Theme / Branding
    public string PrimaryColor { get; set; } = "#1a56db";
    public string SecondaryColor { get; set; } = "#057a55";
    public string AccentColor { get; set; } = "#ffffff";
}