using DealExceptions.Application.Interfaces;
using DealExceptions.Domain;
using DealExceptions.Domain.Entities;
using DealExceptions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DealExceptions.Infrastructure.Repositories;

public class ExceptionRepository(AppDbContext db) : IExceptionRepository
{
    public async Task<IEnumerable<DealException>> ListAsync(ExceptionFilters filters)
    {
        var query = db.DealExceptions.AsQueryable();

        if (filters.OpenOnly)
            query = query.Where(e => e.Status != ExceptionStatus.Closed
                && e.Status != ExceptionStatus.Rejected
                && e.Status != ExceptionStatus.Approved);

        if (!string.IsNullOrWhiteSpace(filters.Status) && Enum.TryParse<ExceptionStatus>(filters.Status, true, out var s))
            query = query.Where(e => e.Status == s);

        if (!string.IsNullOrWhiteSpace(filters.Priority) && Enum.TryParse<Priority>(filters.Priority, true, out var p))
            query = query.Where(e => e.Priority == p);

        if (!string.IsNullOrWhiteSpace(filters.Search))
            query = query.Where(e => EF.Functions.ILike(e.DealRef, $"%{filters.Search}%")
                || EF.Functions.ILike(e.ClientName, $"%{filters.Search}%"));

        return await query
            .OrderByDescending(e => e.Priority)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<DealException?> FindWithDetailsAsync(int id)
        => await db.DealExceptions
            .Include(x => x.Comments.OrderBy(c => c.CreatedAt))
            .Include(x => x.StatusHistories.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<DealException> AddAsync(DealException entity)
    {
        db.DealExceptions.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
