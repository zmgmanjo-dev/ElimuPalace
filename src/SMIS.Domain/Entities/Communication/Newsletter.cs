using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Communication;

public class Newsletter : AuditableEntity
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!; // HTML or Markdown, your choice
    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
}