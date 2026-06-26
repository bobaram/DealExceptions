using Dapper;
using DealExceptions.Application.Interfaces;
using System.Data;

namespace DealExceptions.Infrastructure.Repositories;

public class ReportRepository(IDbConnectionFactory connFactory) : IReportRepository
{
    public async Task<IEnumerable<OpenByOwnerRow>> OpenByOwnerAsync()
    {
        await using var conn = connFactory.CreateConnection();
        return await conn.QueryAsync<OpenByOwnerRow>(
            "usp_Report_OpenByOwner", commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<CriticalOverdueRow>> CriticalOverdueAsync()
    {
        await using var conn = connFactory.CreateConnection();
        return await conn.QueryAsync<CriticalOverdueRow>(
            "usp_Report_CriticalOverdue", commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<ByStatusPriorityRow>> ByStatusPriorityAsync()
    {
        await using var conn = connFactory.CreateConnection();
        return await conn.QueryAsync<ByStatusPriorityRow>(
            "usp_Report_ByStatusPriority", commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<AvgTimeToCloseRow>> AvgTimeToCloseAsync()
    {
        await using var conn = connFactory.CreateConnection();
        return await conn.QueryAsync<AvgTimeToCloseRow>(
            "usp_Report_AvgTimeToClose", commandType: CommandType.StoredProcedure);
    }
}
