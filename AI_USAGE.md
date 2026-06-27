# AI Usage

I used Claude (claude-sonnet-4-6 via Claude Code CLI) as a productivity tool during this build. All architectural decisions, technology choices, and design calls were mine. I used AI to execute them faster — not to make them.

---

## What I used it for

**Boilerplate and scaffolding.** Things like `.csproj` files, Dockerfiles, nginx config, and TypeScript project setup are tedious and error-prone to write from scratch. I gave Claude the structure I wanted and had it generate the files. I read every one before committing.

**Implementation of agreed designs.** Once I'd decided on the architecture — Dapper + stored procedures, DbUp for migrations, minimal APIs, TanStack Query on the frontend — I used Claude to implement it. I reviewed each file for correctness, not to discover what approach had been taken.

**Stored procedure drafts.** I specified what each procedure needed to do and had Claude write the SQL. I reviewed it for correctness and ran it against a real SQL Server instance before committing anything.

**Test scaffolding.** I used Claude to generate the xUnit unit tests and WebApplicationFactory integration test setup based on the structure I'd already defined. I reviewed the test cases and the isolation approach.

**React components.** Same pattern — I defined the component structure and data flow, Claude implemented it.

---

## Decisions I made and why

**Dapper over EF Core.** I wanted explicit, readable SQL that a DBA can review and own. EF Core abstracts that away. For a system where audit trail and data integrity matter, I'd rather see the query.

**DbUp over EF Core migrations.** `CREATE OR ALTER PROCEDURE` in a DbUp RunAlways script gives you idempotent, version-controlled SQL. EF Core migrations are fine for greenfield but not what I'd want owning the schema on a shared database.

**No Redux.** TanStack Query handles all the server state. There's nothing left that needs a client-side store.

**Local JWT issuer instead of Entra ID.** Entra ID requires tenant configuration that breaks a self-contained Docker Compose setup. The architecture is ready for it — swapping the issuer is a config change, not a rewrite.

**`UseSetting` for test config override.** The first approach to injecting the test database connection string wasn't working because `ConfigureAppConfiguration` ran too late. `UseSetting` has guaranteed highest priority and solved it immediately.

---

## How I verified the output

- Read every generated file before committing
- Ran all stored procedures against a real SQL Server instance and checked the results
- Verified integration tests were hitting the test database and not the main one
- Checked all frontend API paths against the actual backend endpoint registrations
- Verified every package version on NuGet

---

## Security notes

The JWT secret in `appsettings.json` is a dev placeholder. It must be replaced via environment variable before production. The `ChangedBy` field is currently passed from the client — once Entra ID is wired up it'll be extracted from the token server-side instead.
