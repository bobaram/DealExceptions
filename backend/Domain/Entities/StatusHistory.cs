using DealExceptions.Domain;
using DealExceptions.Domain.Entities;

namespace DealExceptions.Domain.Entities;

public class StatusHistory
{
    public int Id { get; set; }
    public int ExceptionId { get; set; }
    public DealException Exception { get; set; } = null!;
    public ExceptionStatus FromStatus { get; set; }
    public ExceptionStatus ToStatus { get; set; }
    public string ChangedBy { get; set; } = "";
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
