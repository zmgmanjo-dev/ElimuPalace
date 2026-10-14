using SMIS.Domain.Entities.Platform;

namespace SMIS.Domain.Entities.Platform
{
    // One row per user account, platform-wide. Lets the common login page
    // find which school's database to check a password against, without
    // needing a /{slug}/ in the URL. Not a uniqueness constraint — if two
    // schools ever share an email, both rows simply exist side by side,
    // and the login page handles that case explicitly (see AccountController).
    public class UserDirectoryEntry
    {
        public int Id { get; set; }
        public string NormalizedEmail { get; set; } = string.Empty; // UPPERCASE, used for lookup
        public string Email { get; set; } = string.Empty;           // original casing, for display/logs
        public string DatabaseName { get; set; } = string.Empty;    // which tenant database this user lives in
        public string SchoolName { get; set; } = string.Empty;      // friendly label, shown in the "multiple schools" message
        public int? SchoolRegistrationId { get; set; }
        public SchoolRegistration? SchoolRegistration { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}