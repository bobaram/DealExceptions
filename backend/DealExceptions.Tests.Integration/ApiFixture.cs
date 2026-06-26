using System.Text.RegularExpressions;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DealExceptions.Tests.Integration;

/// <summary>
/// Shared fixture for the "Api" xUnit collection.
/// Creates dealexceptions_test on the Docker SQL Server before any test runs,
/// then drops it after the collection finishes.
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestDb   = "dealexceptions_test";
    private const string MasterCs = "Server=localhost,1433;Database=master;User Id=sa;Password=Dev@123456!;TrustServerCertificate=True;";
    public  const string TestCs   = "Server=localhost,1433;Database=dealexceptions_test;User Id=sa;Password=Dev@123456!;TrustServerCertificate=True;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestCs);
    }

    // ── IAsyncLifetime ─────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        await using var master = new SqlConnection(MasterCs);
        await master.OpenAsync();

        // Drop from previous run if it exists (clean slate)
        await Exec(master, $"""
            IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{TestDb}')
            BEGIN
                ALTER DATABASE [{TestDb}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{TestDb}];
            END
            """);
        await Exec(master, $"CREATE DATABASE [{TestDb}]");

        // Apply schema + stored procedures
        await using var testDb = new SqlConnection(TestCs);
        await testDb.OpenAsync();
        foreach (var batch in Batches(EmbeddedSql("001_schema.sql")))
            await Exec(testDb, batch);
        foreach (var batch in Batches(EmbeddedSql("002_stored_procedures.sql")))
            await Exec(testDb, batch);
    }

    public new async Task DisposeAsync()
    {
        await using var master = new SqlConnection(MasterCs);
        await master.OpenAsync();
        await Exec(master, $"""
            IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{TestDb}')
            BEGIN
                ALTER DATABASE [{TestDb}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{TestDb}];
            END
            """);
    }

    // ── Per-test table reset ───────────────────────────────────────────────────

    public static async Task ResetTablesAsync()
    {
        await using var conn = new SqlConnection(TestCs);
        await conn.OpenAsync();
        // Delete in FK order; identity reseeds to 0 so next row gets Id=1
        await Exec(conn, """
            DELETE FROM [dbo].[StatusHistories];
            DELETE FROM [dbo].[Comments];
            DELETE FROM [dbo].[DealExceptions];
            DBCC CHECKIDENT ('DealExceptions', RESEED, 0);
            """);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static async Task Exec(SqlConnection conn, string sql)
    {
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static string EmbeddedSql(string logicalName)
    {
        var asm = typeof(ApiFixture).Assembly;
        using var stream = asm.GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException($"Embedded resource '{logicalName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static IEnumerable<string> Batches(string sql) =>
        Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
             .Select(b => b.Trim())
             .Where(b => b.Length > 0);
}

[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiFixture> { }
