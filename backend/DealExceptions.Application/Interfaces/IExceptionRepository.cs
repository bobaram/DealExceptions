using DealExceptions.Domain.Entities;

namespace DealExceptions.Application.Interfaces;

public record ExceptionFilters(string? Status, string? Priority, string? Search, bool OpenOnly);

public interface IExceptionRepository
{
    Task<IEnumerable<DealException>> ListAsync(ExceptionFilters filters);
    Task<DealException?> FindWithDetailsAsync(int id);
    Task<DealException> AddAsync(DealException entity);
    Task SaveChangesAsync();
}
