using DealExceptions.Domain;
using DealExceptions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DealExceptions.Endpoints;

public static class ReportsEndpoints
{
    public static void MapReportsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports");

        group.MapGet("/open-by-owner", async (AppDbContext db) =>
        {
            var results = await db.DealExceptions
                .Where(e => e.Status != ExceptionStatus.Closed
                    && e.Status != ExceptionStatus.Rejected
                    && e.Status != ExceptionStatus.Approved)
                .GroupBy(e => e.AssignedOwner ?? "(Unassigned)")
                .Select(g => new { Owner = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return Results.Ok(results);
        }).WithName("OpenByOwner");

        group.MapGet("/critical-overdue", async (AppDbContext db) =>
        {
            var threshold = DateTime.UtcNow.AddDays(-3);
            var results = await db.DealExceptions
                .Where(e => e.Priority == Priority.Critical
                    && e.Status != ExceptionStatus.Closed
                    && e.Status != ExceptionStatus.Approved
                    && e.Status != ExceptionStatus.Rejected
                    && e.CreatedAt <= threshold)
                .OrderBy(e => e.CreatedAt)
                .Select(e => new
                {
                    e.Id, e.DealRef, e.ClientName, e.AssignedOwner,
                    e.CreatedAt, e.Status,
                    DaysOpen = (int)((DateTime.UtcNow - e.CreatedAt).TotalDays)
                })
                .ToListAsync();

            return Results.Ok(results);
        }).WithName("CriticalOverdue");

        group.MapGet("/by-status-priority", async (AppDbContext db) =>
        {
            var results = await db.DealExceptions
                .GroupBy(e => new { Status = e.Status.ToString(), Priority = e.Priority.ToString() })
                .Select(g => new { g.Key.Status, g.Key.Priority, Count = g.Count() })
                .OrderBy(x => x.Status).ThenBy(x => x.Priority)
                .ToListAsync();

            return Results.Ok(results);
        }).WithName("ByStatusPriority");

        group.MapGet("/avg-time-to-close", async (AppDbContext db) =>
        {
            var closed = await db.DealExceptions
                .Where(e => e.Status == ExceptionStatus.Closed || e.Status == ExceptionStatus.Approved || e.Status == ExceptionStatus.Rejected)
                .Select(e => new { e.ExceptionType, Days = (e.UpdatedAt - e.CreatedAt).TotalDays })
                .ToListAsync();

            var results = closed
                .GroupBy(x => x.ExceptionType)
                .Select(g => new { ExceptionType = g.Key, AvgDaysToClose = Math.Round(g.Average(x => x.Days), 1) })
                .OrderBy(x => x.ExceptionType);

            return Results.Ok(results);
        }).WithName("AvgTimeToClose");
    }
}
