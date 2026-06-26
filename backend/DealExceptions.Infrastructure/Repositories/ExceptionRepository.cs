using Dapper;
using DealExceptions.Application.Interfaces;
using DealExceptions.Domain.Entities;
using System.Data;

namespace DealExceptions.Infrastructure.Repositories;

public class ExceptionRepository(IDbConnectionFactory connFactory) : IExceptionRepository
{
    public async Task<(IEnumerable<DealException> Items, int TotalCount)> ListAsync(ExceptionFilters filters)
    {
        await using var conn = connFactory.CreateConnection();
        var rows = (await conn.QueryAsync<DealExceptionRow>(
            "usp_DealException_GetAll",
            new
            {
                filters.Status,
                filters.Priority,
                filters.Search,
                OpenOnly  = filters.OpenOnly ? 1 : 0,
                filters.Page,
                filters.PageSize
            },
            commandType: CommandType.StoredProcedure)).ToList();

        return (rows, rows.FirstOrDefault()?.TotalCount ?? 0);
    }

    // Extends DealException to capture the extra TotalCount window-function column.
    private sealed class DealExceptionRow : DealException
    {
        public int TotalCount { get; set; }
    }

    public async Task<DealException?> FindWithDetailsAsync(int id)
    {
        await using var conn = connFactory.CreateConnection();
        await conn.OpenAsync();

        using var multi = await conn.QueryMultipleAsync(
            "usp_DealException_GetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure);

        var entity = await multi.ReadFirstOrDefaultAsync<DealException>();
        if (entity is null) return null;

        entity.Comments = (await multi.ReadAsync<Comment>()).ToList();
        entity.StatusHistories = (await multi.ReadAsync<StatusHistory>()).ToList();
        return entity;
    }

    public async Task<DealException> AddAsync(DealException entity)
    {
        var createdBy = entity.StatusHistories.FirstOrDefault()?.ChangedBy ?? "system";

        int newId;
        await using var conn = connFactory.CreateConnection();
        newId = await conn.ExecuteScalarAsync<int>(
            "usp_DealException_Create",
            new
            {
                entity.DealRef,
                entity.ClientName,
                entity.ExceptionType,
                entity.Description,
                Priority = entity.Priority.ToString(),
                entity.AssignedOwner,
                CreatedBy = createdBy
            },
            commandType: CommandType.StoredProcedure);

        return (await FindWithDetailsAsync(newId))!;
    }

    public async Task UpdateStatusAsync(int id, string newStatus, string changedBy, string? notes)
    {
        await using var conn = connFactory.CreateConnection();
        await conn.ExecuteAsync(
            "usp_DealException_UpdateStatus",
            new { Id = id, Status = newStatus, ChangedBy = changedBy, Notes = notes },
            commandType: CommandType.StoredProcedure);
    }
}
