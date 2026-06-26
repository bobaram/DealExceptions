using DealExceptions.Application.DTOs;
using DealExceptions.Application.Interfaces;
using DealExceptions.Domain;
using DealExceptions.Domain.Entities;

namespace DealExceptions.Application.Services;

public class ExceptionService(IExceptionRepository repo)
{
    public async Task<PagedResult<ExceptionSummaryDto>> GetAllAsync(
        string? status, string? priority, string? search, bool openOnly, int page = 1, int pageSize = 20)
    {
        var (items, total) = await repo.ListAsync(new ExceptionFilters(status, priority, search, openOnly, page, pageSize));
        return new PagedResult<ExceptionSummaryDto>(items.Select(ToSummary), total, page, pageSize);
    }

    public async Task<ExceptionDetailDto?> GetByIdAsync(int id)
    {
        var e = await repo.FindWithDetailsAsync(id);
        return e is null ? null : ToDetail(e);
    }

    public async Task<ExceptionDetailDto> CreateAsync(CreateExceptionRequest req)
    {
        if (!Enum.TryParse<Priority>(req.Priority, true, out var priority))
            throw new ArgumentException($"Invalid priority: {req.Priority}");

        var entity = new DealException
        {
            DealRef = req.DealRef.Trim(),
            ClientName = req.ClientName.Trim(),
            ExceptionType = req.ExceptionType.Trim(),
            Description = req.Description.Trim(),
            Priority = priority,
            Status = ExceptionStatus.New,
            AssignedOwner = string.IsNullOrWhiteSpace(req.AssignedOwner) ? null : req.AssignedOwner.Trim(),
            StatusHistories = [new StatusHistory { ChangedBy = req.CreatedBy, Notes = "Created" }]
        };

        var saved = await repo.AddAsync(entity);
        return ToDetail(saved);
    }

    public async Task<ExceptionDetailDto?> UpdateStatusAsync(int id, UpdateStatusRequest req)
    {
        if (!Enum.TryParse<ExceptionStatus>(req.Status, true, out _))
            throw new ArgumentException($"Invalid status: {req.Status}");

        await repo.UpdateStatusAsync(id, req.Status, req.ChangedBy, req.Notes);
        var entity = await repo.FindWithDetailsAsync(id);
        return entity is null ? null : ToDetail(entity);
    }

    private static bool IsOpen(ExceptionStatus s) =>
        s != ExceptionStatus.Closed && s != ExceptionStatus.Rejected && s != ExceptionStatus.Approved;

    private static ExceptionSummaryDto ToSummary(DealException e) => new(
        e.Id, e.DealRef, e.ClientName, e.ExceptionType,
        e.Priority.ToString(), e.Status.ToString(),
        e.AssignedOwner, e.CreatedAt, e.UpdatedAt,
        IsOpen(e.Status), e.Priority == Priority.Critical,
        e.IsPossibleDuplicate
    );

    private static ExceptionDetailDto ToDetail(DealException e) => new(
        e.Id, e.DealRef, e.ClientName, e.ExceptionType, e.Description,
        e.Priority.ToString(), e.Status.ToString(),
        e.AssignedOwner, e.CreatedAt, e.UpdatedAt,
        IsOpen(e.Status), e.Priority == Priority.Critical,
        e.IsPossibleDuplicate,
        e.Comments.Select(c => new CommentDto(c.Id, c.AuthorName, c.Text, c.CreatedAt)),
        e.StatusHistories.Select(h => new StatusHistoryDto(h.Id, h.FromStatus.ToString(), h.ToStatus.ToString(), h.ChangedBy, h.ChangedAt, h.Notes))
    );
}
