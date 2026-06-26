using DealExceptions.Application.DTOs;
using DealExceptions.Domain;
using DealExceptions.Domain.Entities;
using DealExceptions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DealExceptions.Application.Services;

public class ExceptionService
{
    private readonly AppDbContext _db;

    public ExceptionService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<ExceptionSummaryDto>> GetAllAsync(
        string? status, string? priority, string? search, bool openOnly)
    {
        var query = _db.DealExceptions.AsQueryable();

        if (openOnly)
            query = query.Where(e => e.Status != ExceptionStatus.Closed
                && e.Status != ExceptionStatus.Rejected
                && e.Status != ExceptionStatus.Approved);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ExceptionStatus>(status, true, out var s))
            query = query.Where(e => e.Status == s);

        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<Priority>(priority, true, out var p))
            query = query.Where(e => e.Priority == p);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => EF.Functions.ILike(e.DealRef, $"%{search}%")
                || EF.Functions.ILike(e.ClientName, $"%{search}%"));

        return await query
            .OrderByDescending(e => e.Priority)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => ToSummary(e))
            .ToListAsync();
    }

    public async Task<ExceptionDetailDto?> GetByIdAsync(int id)
    {
        var e = await _db.DealExceptions
            .Include(x => x.Comments.OrderBy(c => c.CreatedAt))
            .Include(x => x.StatusHistories.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(x => x.Id == id);

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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        entity.StatusHistories.Add(new StatusHistory
        {
            FromStatus = ExceptionStatus.New,
            ToStatus = ExceptionStatus.New,
            ChangedBy = req.CreatedBy,
            ChangedAt = entity.CreatedAt,
            Notes = "Created"
        });

        _db.DealExceptions.Add(entity);
        await _db.SaveChangesAsync();

        return ToDetail(entity);
    }

    public async Task<ExceptionDetailDto?> UpdateStatusAsync(int id, UpdateStatusRequest req)
    {
        if (!Enum.TryParse<ExceptionStatus>(req.Status, true, out var newStatus))
            throw new ArgumentException($"Invalid status: {req.Status}");

        var entity = await _db.DealExceptions
            .Include(x => x.Comments.OrderBy(c => c.CreatedAt))
            .Include(x => x.StatusHistories.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null) return null;

        var oldStatus = entity.Status;
        entity.Status = newStatus;
        entity.UpdatedAt = DateTime.UtcNow;

        entity.StatusHistories.Add(new StatusHistory
        {
            ExceptionId = entity.Id,
            FromStatus = oldStatus,
            ToStatus = newStatus,
            ChangedBy = req.ChangedBy,
            ChangedAt = entity.UpdatedAt,
            Notes = req.Notes
        });

        await _db.SaveChangesAsync();
        return ToDetail(entity);
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
