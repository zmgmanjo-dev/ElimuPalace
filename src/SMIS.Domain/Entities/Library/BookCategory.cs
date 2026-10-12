using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Library;

/// <summary>
/// Categories for organizing library books
/// e.g. Fiction, Science, Mathematics, Reference
/// </summary>
public class BookCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }

    // Navigation
    public ICollection<Book> Books { get; set; } = new List<Book>();
}