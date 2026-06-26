using DealExceptions.Domain;

namespace DealExceptions.Domain.Entities;

public class DealException
{
    public int Id { get; set; }
    public string DealRef { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string ExceptionType { get; set; } = "";
    public string Description { get; set; } = "";
    public Priority Priority { get; set; }
    public ExceptionStatus Status { get; set; }
    public string? AssignedOwner { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? LegacyId { get; set; }
    public bool IsPossibleDuplicate { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<StatusHistory> StatusHistories { get; set; } = new List<StatusHistory>();
}
