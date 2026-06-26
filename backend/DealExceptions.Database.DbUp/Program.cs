using DbUp;
using DbUp.Helpers;

var connectionString =
    args.FirstOrDefault()
    ?? Environment.GetEnvironmentVariable("ConnectionString")
    ?? "Host=localhost;Database=dealexceptions;Username=app;Password=app_dev_pw";

EnsureDatabase.For.PostgresqlDatabase(connectionString);

// Phase 1 — Schema (RunAlways/Primary): idempotent CREATE TABLE / index scripts, no journal
var schemaResult = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(typeof(Program).Assembly,
        s => s.Contains("RunAlways.Primary"))
    .WithTransaction()
    .JournalTo(new NullJournal())
    .LogToConsole()
    .Build()
    .PerformUpgrade();

if (!schemaResult.Successful)
{
    Console.Error.WriteLine(schemaResult.Error);
    return 1;
}

// Phase 2 — Seed data (RunAlways/Secondary): idempotent MERGE / DO $$ scripts, no journal
var seedResult = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(typeof(Program).Assembly,
        s => s.Contains("RunAlways.Secondary"))
    .WithTransaction()
    .JournalTo(new NullJournal())
    .LogToConsole()
    .Build()
    .PerformUpgrade();

if (!seedResult.Successful)
{
    Console.Error.WriteLine(seedResult.Error);
    return 1;
}

// Phase 3 — Migrations (RunOnce): journaled in public."DbUp" table, runs each script once
var migrationResult = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(typeof(Program).Assembly,
        s => s.Contains("RunOnce"))
    .WithTransaction()
    .JournalToPostgresqlTable("public", "DbUp")
    .LogToConsole()
    .Build()
    .PerformUpgrade();

if (!migrationResult.Successful)
{
    Console.Error.WriteLine(migrationResult.Error);
    return 1;
}

Console.WriteLine("Database upgrade successful.");
return 0;
