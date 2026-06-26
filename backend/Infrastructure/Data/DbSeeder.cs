using DealExceptions.Domain;
using DealExceptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DealExceptions.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.DealExceptions.AnyAsync()) return;

        var now = DateTime.UtcNow;

        var exceptions = new List<DealException>
        {
            new() { LegacyId = 1001, DealRef = "SF-2026-00451", ClientName = "Mabena Transport CC", ExceptionType = "Affordability", Description = "Bank statements received but affordability calc missing latest month", Priority = Priority.High, Status = ExceptionStatus.New, AssignedOwner = "Nomsa Mokoena", CreatedAt = new DateTime(2026,6,14,9,15,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,6,14,9,15,0,DateTimeKind.Utc) },
            new() { LegacyId = 1002, DealRef = "SF-2026-00452", ClientName = "Imbizo Hardware (Pty) Ltd", ExceptionType = "Credit Limit", Description = "Requested facility above normal credit limit; manual review required", Priority = Priority.Critical, Status = ExceptionStatus.InReview, AssignedOwner = "Thabo Dlamini", CreatedAt = new DateTime(2026,6,11,14,2,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,6,19,16,31,0,DateTimeKind.Utc) },
            new() { LegacyId = 1003, DealRef = "SF-2026-00453", ClientName = "Langa Foods", ExceptionType = "Missing Docs", Description = "FICA docs not on file, but deal progressed to review", Priority = Priority.Medium, Status = ExceptionStatus.Approved, AssignedOwner = "Busi Khumalo", CreatedAt = new DateTime(2026,6,12,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,6,18,0,0,0,DateTimeKind.Utc) },
            new() { LegacyId = 1004, DealRef = "SF-2026-00454", ClientName = "Blue Crane Logistics", ExceptionType = "Pricing Override", Description = "Rate override requested below floor", Priority = Priority.High, Status = ExceptionStatus.Pending, AssignedOwner = "Nomsa Mokoena", CreatedAt = new DateTime(2026,6,8,8,44,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,6,13,11,10,0,DateTimeKind.Utc) },
            new() { LegacyId = 1005, DealRef = "SF-2026-00454", ClientName = "Blue Crane Logistics", ExceptionType = "Pricing Override", Description = "Duplicate row created from Excel import", Priority = Priority.High, Status = ExceptionStatus.New, AssignedOwner = "Nomsa Mokoena", CreatedAt = new DateTime(2026,6,8,8,45,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,6,9,10,22,0,DateTimeKind.Utc), IsPossibleDuplicate = true },
            new() { LegacyId = 1006, DealRef = "SF-2026-00455", ClientName = "Vela Medical Supplies", ExceptionType = "Director Approval", Description = "One director consent outstanding", Priority = Priority.Low, Status = ExceptionStatus.Closed, AssignedOwner = "Ayanda Ncube", CreatedAt = new DateTime(2026,5,28,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,6,2,0,0,0,DateTimeKind.Utc) },
            new() { LegacyId = 1007, DealRef = "SF-2026-00456", ClientName = "Northern Solar Installers", ExceptionType = "Affordability", Description = "Manual override requested because bank account includes once-off grant", Priority = Priority.Critical, Status = ExceptionStatus.New, AssignedOwner = null, CreatedAt = new DateTime(2026,6,10,7,20,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,6,12,13,2,0,DateTimeKind.Utc) },
            new() { LegacyId = 1008, DealRef = "SF-2026-00457", ClientName = "Kopano Retail Group", ExceptionType = "Data Mismatch", Description = "Client name differs between CRM and loan pack", Priority = Priority.Medium, Status = ExceptionStatus.InReview, AssignedOwner = "Thabo Dlamini", CreatedAt = new DateTime(2026,6,17,15,32,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,6,19,9,9,0,DateTimeKind.Utc) },
            new() { LegacyId = 1009, DealRef = "SF-2026-00458", ClientName = "Umoya Farming", ExceptionType = "Security Docs", Description = "Surety signed but not witnessed", Priority = Priority.Low, Status = ExceptionStatus.Rejected, AssignedOwner = "Busi Khumalo", CreatedAt = new DateTime(2026,6,18,10,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,6,18,12,16,0,DateTimeKind.Utc) },
            new() { LegacyId = 1010, DealRef = "SF-2026-00459", ClientName = "Khanyisa Print Works", ExceptionType = "Affordability", Description = "Calculator in Excel gives different result from SharePoint column", Priority = Priority.High, Status = ExceptionStatus.InReview, AssignedOwner = "Ayanda Ncube", CreatedAt = new DateTime(2026,6,13,16,40,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,6,20,8,55,0,DateTimeKind.Utc) },
            new() { LegacyId = 1011, DealRef = "SF-2026-00460", ClientName = "Urban Renewals SA", ExceptionType = "Credit Limit", Description = "Manual exception created by email; missing from SharePoint until yesterday", Priority = Priority.Critical, Status = ExceptionStatus.New, AssignedOwner = "Justin Naidoo", CreatedAt = new DateTime(2026,6,5,11,30,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,6,21,18,3,0,DateTimeKind.Utc) },
            new() { LegacyId = 1012, DealRef = "SF-2026-00461", ClientName = "Pula Office Supplies", ExceptionType = "Other", Description = "Business says 'just approve this one' with no reason captured", Priority = Priority.Medium, Status = ExceptionStatus.Closed, AssignedOwner = "Nomsa Mokoena", CreatedAt = new DateTime(2026,6,1,9,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,6,1,9,5,0,DateTimeKind.Utc) },
        };

        await db.DealExceptions.AddRangeAsync(exceptions);
        await db.SaveChangesAsync();

        // Re-fetch to get IDs
        var saved = await db.DealExceptions.ToDictionaryAsync(e => e.LegacyId!.Value);

        var comments = new List<Comment>
        {
            new() { LegacyId = 5001, ExceptionId = saved[1001].Id, AuthorName = "Nomsa Mokoena", Text = "Created from PowerApps screen.", CreatedAt = new DateTime(2026,6,14,9,15,0,DateTimeKind.Utc) },
            new() { LegacyId = 5002, ExceptionId = saved[1002].Id, AuthorName = "Justin Naidoo", Text = "Credit memo is in my mailbox; I will forward later.", CreatedAt = new DateTime(2026,6,12,10,21,0,DateTimeKind.Utc) },
            new() { LegacyId = 5003, ExceptionId = saved[1002].Id, AuthorName = "Thabo Dlamini", Text = "Still waiting for signed approval.", CreatedAt = new DateTime(2026,6,19,16,31,0,DateTimeKind.Utc) },
            new() { LegacyId = 5004, ExceptionId = saved[1003].Id, AuthorName = "Busi Khumalo", Text = "Team lead approved conditional on documents.", CreatedAt = new DateTime(2026,6,18,8,12,0,DateTimeKind.Utc) },
            new() { LegacyId = 5005, ExceptionId = saved[1004].Id, AuthorName = "Finance User", Text = "Need margin impact before approval.", CreatedAt = new DateTime(2026,6,13,11,10,0,DateTimeKind.Utc) },
            new() { LegacyId = 5006, ExceptionId = saved[1005].Id, AuthorName = "Nomsa Mokoena", Text = "Possible duplicate of SF-2026-00454 (Legacy ID 1004). Not sure which one is current.", CreatedAt = new DateTime(2026,6,9,10,22,0,DateTimeKind.Utc) },
            new() { LegacyId = 5007, ExceptionId = saved[1007].Id, AuthorName = "Ops User", Text = "No owner found; leaving blank for now.", CreatedAt = new DateTime(2026,6,12,13,2,0,DateTimeKind.Utc) },
            new() { LegacyId = 5008, ExceptionId = saved[1008].Id, AuthorName = "Thabo Dlamini", Text = "CRM trading name differs from application pack.", CreatedAt = new DateTime(2026,6,19,9,9,0,DateTimeKind.Utc) },
            new() { LegacyId = 5009, ExceptionId = saved[1010].Id, AuthorName = "Ayanda Ncube", Text = "Excel formula changed by business user last week.", CreatedAt = new DateTime(2026,6,20,8,55,0,DateTimeKind.Utc) },
            new() { LegacyId = 5010, ExceptionId = saved[1011].Id, AuthorName = "Justin Naidoo", Text = "Backfilled from email chain after the fact.", CreatedAt = new DateTime(2026,6,21,18,3,0,DateTimeKind.Utc) },
        };

        await db.Comments.AddRangeAsync(comments);

        // Seed initial status history entries (imported = system entry)
        var statusHistories = exceptions.Select(e => new StatusHistory
        {
            ExceptionId = saved[e.LegacyId!.Value].Id,
            FromStatus = ExceptionStatus.New,
            ToStatus = e.Status,
            ChangedBy = "LegacyImport",
            ChangedAt = e.CreatedAt,
            Notes = "Imported from legacy PowerApps/SharePoint system"
        }).ToList();

        // For statuses that are already New, the history is just a creation record
        foreach (var h in statusHistories.Where(h => h.FromStatus == h.ToStatus))
            h.Notes = "Created in legacy system";

        await db.StatusHistories.AddRangeAsync(statusHistories);
        await db.SaveChangesAsync();
    }
}
