using DealExceptions.Application.Interfaces;

namespace DealExceptions.Endpoints;

public static class ReportsEndpoints
{
    public static void MapReportsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports");

        group.MapGet("/open-by-owner", async (IReportRepository repo) =>
            Results.Ok(await repo.OpenByOwnerAsync()))
            .WithName("OpenByOwner");

        group.MapGet("/critical-overdue", async (IReportRepository repo) =>
            Results.Ok(await repo.CriticalOverdueAsync()))
            .WithName("CriticalOverdue");

        group.MapGet("/by-status-priority", async (IReportRepository repo) =>
            Results.Ok(await repo.ByStatusPriorityAsync()))
            .WithName("ByStatusPriority");

        group.MapGet("/avg-time-to-close", async (IReportRepository repo) =>
            Results.Ok(await repo.AvgTimeToCloseAsync()))
            .WithName("AvgTimeToClose");
    }
}
