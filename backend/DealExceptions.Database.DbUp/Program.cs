using DbUp;
using DbUp.Helpers;
using Microsoft.Extensions.Configuration;
using System.Reflection;

class Program
{
    static int Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = args.FirstOrDefault()
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=dealexceptions;User Id=sa;Password=Dev@123456!;TrustServerCertificate=True;";

        EnsureDatabase.For.SqlDatabase(connectionString);

        var runAlwaysPrimaryUpgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(),
                s => s.Contains("scripts.RunAlways.Primary", StringComparison.InvariantCultureIgnoreCase))
            .JournalTo(new NullJournal())
            .WithTransaction()
            .LogToConsole()
            .Build();

        var runAlwaysSecondaryUpgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(),
                s => s.Contains("scripts.RunAlways.Secondary", StringComparison.InvariantCultureIgnoreCase))
            .JournalTo(new NullJournal())
            .WithTransaction()
            .LogToConsole()
            .Build();

        var runOnceUpgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(),
                s => s.Contains("scripts.RunOnce", StringComparison.InvariantCultureIgnoreCase))
            .JournalToSqlTable("dbo", "DbUp")
            .WithTransaction()
            .LogToConsole()
            .Build();

        var result1 = runAlwaysPrimaryUpgrader.PerformUpgrade();
        if (!result1.Successful) return HandleError(result1.Error);

        var result2 = runAlwaysSecondaryUpgrader.PerformUpgrade();
        if (!result2.Successful) return HandleError(result2.Error);

        var result3 = runOnceUpgrader.PerformUpgrade();
        if (!result3.Successful) return HandleError(result3.Error);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Success!");
        Console.ResetColor();
        return 0;
    }

    static int HandleError(Exception error)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(error);
        Console.ResetColor();
        return 1;
    }
}
