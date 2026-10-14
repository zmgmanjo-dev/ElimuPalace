using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Platform;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Services
{
    // Keeps the master "which school is this email in" lookup table up to date.
    // Call RegisterUserAsync right after any IdentityUser is created in any
    // tenant database (admin provisioning, staff creation, parent accounts).
    public class GlobalUserDirectoryService
    {
        private readonly MasterDbContext _masterDb;

        public GlobalUserDirectoryService(MasterDbContext masterDb)
        {
            _masterDb = masterDb;
        }

        public async Task RegisterUserAsync(string email, string databaseName, string schoolName, int? schoolRegistrationId = null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(databaseName)) return;

            var normalized = email.Trim().ToUpperInvariant();

            var exists = await _masterDb.UserDirectoryEntries
                .AnyAsync(x => x.NormalizedEmail == normalized && x.DatabaseName == databaseName);
            if (exists) return;

            _masterDb.UserDirectoryEntries.Add(new UserDirectoryEntry
            {
                NormalizedEmail = normalized,
                Email = email.Trim(),
                DatabaseName = databaseName,
                SchoolName = schoolName,
                SchoolRegistrationId = schoolRegistrationId
            });
            await _masterDb.SaveChangesAsync();
        }

        public async Task RemoveUserAsync(string email, string databaseName)
        {
            var normalized = email.Trim().ToUpperInvariant();
            var entry = await _masterDb.UserDirectoryEntries
                .FirstOrDefaultAsync(x => x.NormalizedEmail == normalized && x.DatabaseName == databaseName);
            if (entry != null)
            {
                _masterDb.UserDirectoryEntries.Remove(entry);
                await _masterDb.SaveChangesAsync();
            }
        }

        public async Task<List<UserDirectoryEntry>> FindByEmailAsync(string email)
        {
            var normalized = email.Trim().ToUpperInvariant();
            return await _masterDb.UserDirectoryEntries
                .Where(x => x.NormalizedEmail == normalized)
                .ToListAsync();
        }
    }
}