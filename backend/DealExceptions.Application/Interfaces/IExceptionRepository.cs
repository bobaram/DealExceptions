using DealExceptions.Domain.Entities;

namespace DealExceptions.Application.Interfaces;

public record ExceptionFilters(string? Status, string? Priority, string? Search, bool OpenOnly, int Page = 1, int PageSize = 20);

public interface IExceptionRepository
{
    Task<(IEnumerable<DealException> Items, int TotalCount)> ListAsync(ExceptionFilters filters);
    Task<DealException?> FindWithDetailsAsync(int id);
    Task<DealException> AddAsync(DealException entity);
    Task UpdateStatusAsync(int id, string newStatus, string changedBy, string? notes);
}
