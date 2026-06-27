# AI Usage

I used Claude (claude-sonnet-4-6 via Claude Code CLI) throughout this project. This document covers what I actually used it for, what I changed or rejected, and where I had to verify things myself.

---

## What I used it for

**Architecture decisions**

Before writing any code I talked through the architecture with Claude — Minimal APIs vs controllers, Dapper vs EF Core, whether a repository layer was worth the boilerplate at this scale, and how to handle database migrations cleanly. The conversation was useful for pressure-testing assumptions quickly. I agreed with most of it; the main thing I pushed back on was EF Core (more on that below).

**Boilerplate**

The parts of the project that are purely configuration — `.csproj` files with the right package versions, multi-stage Dockerfiles, nginx reverse proxy config, `tsconfig.json`, `vite.config.ts` — I used Claude to generate. Getting these wrong wastes time for no good reason; getting them right is just pattern matching against known-good templates. Claude is good at this and I checked every file before committing.

**Stored procedures**

All nine `usp_*` stored procedures were drafted with Claude's help. I reviewed each one for correctness — particularly the `COUNT(*) OVER()` window function for returning pagination totals in a single query, the `OFFSET … FETCH NEXT … ROWS ONLY` syntax, and the duplicate detection logic that needs to flag both the new row and any existing open matches in the same operation. The SQL was run against a real SQL Server instance and verified to produce the right results before being committed.

**Data normalisation**

The seed data was a mess. The legacy CSV had priorities written as `"H"`, `"critical "` (with a trailing space), and `"Critical"` all meaning the same thing. Dates in three different formats. The same owner name written three different ways. I used Claude to work through the normalisation logic, then verified every decision against the source CSV row by row before accepting it.

**Service and endpoint implementation**

The service layer and API endpoints were generated with Claude and reviewed for correct async usage, sensible error handling, and validation that matched the business rules. Nothing was accepted without being read.

**Tests**

The unit test and integration test scaffolding — xUnit + Moq for services, `WebApplicationFactory` with a real SQL Server test database for integration tests — was generated with Claude. I reviewed the structure and the test cases, particularly the integration test isolation approach (`ICollectionFixture` with per-test table resets) and the `TestAuthHandler` that bypasses JWT validation in the test host.

**Authentication**

The full auth flow was built with Claude: the login endpoint, JWT signing, the fallback policy that protects all routes by default, the frontend login page, token storage, and the `auth:expired` event pattern for handling 401s cleanly. I reviewed the security properties of each part.

**React components**

Components were generated with Claude and reviewed for correct TanStack Query usage, form validation, and that the API paths matched what the backend actually exposed.

---

## What I changed or rejected

**EF Core → Dapper + stored procedures.** Claude's first suggestion was EF Core. I rejected it. Stored procedures give explicit, reviewable SQL that a DBA can understand and own. Dapper is lighter and doesn't hide what's happening. For a system where the audit trail and data integrity matter, I'd rather see the SQL.

**EF Core migrations → DbUp.** Same reasoning. DbUp with `CREATE OR ALTER PROCEDURE` gives idempotent, version-controlled SQL scripts. EF Core migrations are fine for greenfield projects but they're not what I'd want a DBA reviewing before a production deployment.

**`ConfigureAppConfiguration` override in tests → `UseSetting`.** The first approach to overriding the test database connection string wasn't working — the app was still picking up the main database. The root cause was timing: `ConfigureAppConfiguration` ran after `AddInfrastructure` had already read the config. Switching to `builder.UseSetting()` fixed it because it has guaranteed highest priority.

**Redux → TanStack Query.** Claude didn't push this but it came up in the architecture discussion. TanStack Query handles all the server state; there's nothing left that needs Redux.

**Tailwind → plain CSS.** Adds build complexity for no benefit at this scale.

**Azure Entra ID → local JWT issuer.** A full Entra ID integration requires tenant configuration that isn't possible in a self-contained Docker Compose setup. The current implementation uses a local issuer with two hardcoded dev accounts. It's explicitly flagged as incomplete in the README, and the path to the real thing is clear.

---

## How I verified the output

The main rule was: don't commit anything I haven't read. Beyond that:

- Every stored procedure was run against a real SQL Server instance. I didn't trust syntax correctness from inspection alone.
- The seed data normalisation was checked against the source CSV row by row.
- Pagination was traced end-to-end: from the `COUNT(*) OVER()` in the SP, through the `DealExceptionRow.TotalCount` property in the repository, to `PagedResult<T>.TotalCount` in the API response.
- Integration tests were verified to be hitting the test database and not the main one. The way I confirmed this was checking that the 12 seed records from the main database did not appear in test results after `ResetTablesAsync()`.
- All frontend API paths were checked against the backend endpoint registrations.

---

## Risks I thought about

**The JWT secret.** The value in `appsettings.json` is a dev placeholder. It must be overridden via the `JWT__Secret` environment variable before this goes anywhere near production. If that doesn't happen, any token signed with the dev secret would be accepted.

**`ChangedBy` is still client-controlled.** The display name comes from the logged-in user's session on the frontend, which reads it from the decoded JWT. But the API endpoint still accepts `changedBy` as a field in the request body and doesn't verify it against the token claims server-side. A determined client could pass any value. The fix is to extract the claim from `HttpContext.User` on the backend rather than trusting the request body — that's part of the Entra ID integration work.

**No real customer data in AI prompts.** The seed data uses fictional names and deal references throughout. Nothing from the actual legacy system was pasted into any prompt.

**Package versions.** I checked every package version on NuGet rather than accepting whatever Claude suggested. This is an area where AI output goes stale quickly.
