using DealExceptions.Domain.Entities;

namespace DealExceptions.Domain.Entities;

public class Comment
{
    public int Id { get; set; }
    public int ExceptionId { get; set; }
    public DealException Exception { get; set; } = null!;
    public string AuthorName { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? LegacyId { get; set; }
}
