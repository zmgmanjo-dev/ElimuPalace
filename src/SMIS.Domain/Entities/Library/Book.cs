using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Library;

/// <summary>
/// Represents a book in the school library
/// </summary>
public class Book : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? ISBN { get; set; }
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string? Edition { get; set; }
    public int BookCategoryId { get; set; }
    public int TotalCopies { get; set; } = 1;
    public int AvailableCopies { get; set; } = 1;
    public string? ShelfLocation { get; set; }
    public string? Description { get; set; }
    public string? CoverImagePath { get; set; }
    public BookCondition Condition { get; set; } = BookCondition.Good;

    // Navigation
    public BookCategory Category { get; set; } = null!;
    public ICollection<BookIssue> Issues { get; set; } = new List<BookIssue>();
}

public enum BookCondition
{
    New = 1,
    Good = 2,
    Fair = 3,
    Poor = 4,
    Damaged = 5
}