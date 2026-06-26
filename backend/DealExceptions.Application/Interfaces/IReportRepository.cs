namespace DealExceptions.Application.Interfaces;

public record OpenByOwnerRow(string Owner, int Count);
public record CriticalOverdueRow(int Id, string DealRef, string ClientName, string Owner, DateTime CreatedAt, string Status, int DaysOpen);
public record ByStatusPriorityRow(string Status, string Priority, int Count);
public record AvgTimeToCloseRow(string ExceptionType, double AvgDaysToClose);

public interface IReportRepository
{
    Task<IEnumerable<OpenByOwnerRow>> OpenByOwnerAsync();
    Task<IEnumerable<CriticalOverdueRow>> CriticalOverdueAsync();
    Task<IEnumerable<ByStatusPriorityRow>> ByStatusPriorityAsync();
    Task<IEnumerable<AvgTimeToCloseRow>> AvgTimeToCloseAsync();
}
