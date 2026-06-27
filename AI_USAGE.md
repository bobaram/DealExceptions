# AI Usage

## Tools Used

- **Claude (claude-sonnet-4-6 via Claude Code CLI)** — primary tool throughout

---

## What I Used AI For

### Architecture and planning

Used AI to talk through the architecture before writing any code: Minimal APIs vs controllers, Dapper vs EF Core, whether to include a repository layer, DbUp vs EF Core migrations, and how to structure stored procedures for idempotent deployment. The recommendations were sensible and I agreed with them.

### Scaffolding boilerplate

Used AI to generate the initial versions of:
- `.csproj` files with correct package references and versions
- Dapper repository pattern with a `DealExceptionRow` private subclass for capturing `COUNT(*) OVER()` pagination columns without polluting the domain entity
- DbUp runner configuration (`NullJournal` for RunAlways scripts, standard journal for one-time scripts)
- Vite + React + TypeScript project config (`tsconfig.json`, `vite.config.ts`, `package.json`)
- Multi-stage Dockerfiles for both backend and frontend
- nginx reverse proxy config

This saved significant time on configuration that is correct-or-broken with no middle ground.

### Data normalisation logic

Used AI to reason through the messy seed data: resolving `"H"` → `High`, `"critical "` → `Critical`, `"CLOSEd"` → `Closed`, three date formats, and three variants of the same owner name. The normalisation decisions were verified against the source CSV before accepting them.

### Stored procedure design

Used AI to draft all nine `usp_*` stored procedures. Reviewed each one for:
- Correct `CREATE OR ALTER PROCEDURE` syntax for idempotent re-deployment
- `COUNT(*) OVER()` window function for single-query pagination totals (avoids a second round-trip)
- `OFFSET … FETCH NEXT … ROWS ONLY` pagination syntax
- Duplicate detection logic: flagging both the new row and all existing open matches in the same transaction
- `SCOPE_IDENTITY()` usage after inserts

### Service and endpoint implementation

Used AI to generate the service and endpoint implementations based on the agreed contracts. Reviewed each file for:
- Correct async/await usage with Dapper (`QueryAsync`, `ExecuteAsync`, `QuerySingleOrDefaultAsync`)
- Sensible error handling (`ArgumentException` → 400, null → 404)
- Validation that matched the business rules from the brief
- `PagedResult<T>` wrapper DTO flowing cleanly from SP → repository → service → endpoint

### Test suite

Used AI to generate the unit and integration test scaffolding:
- xUnit + Moq unit tests for `ExceptionService` (28 tests) and `CommentService` (5 tests)
- `WebApplicationFactory<Program>` integration tests against a real SQL Server test database (28 tests across exceptions and comments endpoints)
- `ICollectionFixture<ApiFixture>` to share one database fixture across all integration test classes
- `TestAuthHandler` that always returns a successful authentication result, registered via `ConfigureTestServices` to bypass JWT validation in the test host

### JWT authentication

Used AI to implement the full auth flow:
- `POST /api/auth/login` endpoint reading users from `appsettings.json`, issuing a signed JWT
- `FallbackPolicy` requiring authentication on all endpoints except login and health check
- Frontend login page, `sessionStorage` token persistence, `Authorization: Bearer …` header injection in `client.ts`, and `auth:expired` DOM event for 401 handling
- Swagger Bearer security definition

### React components

Used AI to generate the component implementations. Reviewed for correct TanStack Query usage (`useQuery` keys, `useMutation` + `invalidateQueries`), form validation patterns, and accessibility basics.

### README

Used AI to draft the README sections, particularly the "Converting the Shadow App" and "Working in a Small Team" sections. These were based on genuine analysis of the legacy data and business user notes — AI helped structure and articulate conclusions I had already reached.

---

## What I Accepted, Changed, or Rejected

| Output | Decision | Reason |
|---|---|---|
| EF Core for data access | Rejected | Stored procedures give explicit control over SQL; Dapper is lighter and fits the repo's audit-trail needs without the ORM overhead |
| EF Core migrations | Rejected | DbUp with `CREATE OR ALTER PROCEDURE` gives idempotent, reviewable SQL scripts that match how the DBA team would want to own schema changes |
| Repository pattern | Accepted (adapted) | Kept the interface for testability with Moq; the concrete implementation is thin Dapper code, not a generic repository |
| `WebApplicationFactory` with `ConfigureAppConfiguration` override | Changed to `UseSetting` | `ConfigureAppConfiguration` ran too late; `UseSetting` has guaranteed highest priority and correctly overrides the connection string before `AddInfrastructure` reads it |
| Swagger + Serilog package versions | Accepted after checking | Confirmed compatible with .NET 8 on NuGet |
| Redux for frontend state | Rejected | TanStack Query handles server state; no complex client state requiring Redux |
| Tailwind CSS | Rejected | Adds build config complexity; plain CSS is sufficient for this scope |
| Status badge inline styles | Accepted | Keeps components self-contained without a CSS class naming convention to maintain |
| Azure Entra ID for auth | Deferred | The brief calls for awareness of auth; a full Entra ID integration requires tenant configuration outside the scope of a self-contained Docker Compose setup. Current implementation uses a local JWT issuer with hardcoded dev users and is explicitly noted as incomplete in the README |

---

## How I Verified AI-Assisted Work

- Read every generated file before committing — did not commit output blindly
- Verified stored procedure SQL against SQL Server 2022 syntax (window functions, `OFFSET … FETCH`, `SCOPE_IDENTITY`)
- Checked that pagination `COUNT(*) OVER()` returns the correct total by tracing the value from SP through `DealExceptionRow.TotalCount` to `PagedResult<T>.TotalCount` in the API response
- Confirmed duplicate detection logic handles both directions: new row flagged, and existing open rows back-flagged
- Verified that `UseSetting` correctly overrides the connection string in integration tests by checking that seed data from the main database did not appear in test results
- Checked all API endpoint paths in the frontend API client match the backend endpoint registrations
- Confirmed Docker Compose service names match the nginx proxy target (`http://backend:8080`)

---

## Risks Considered

**Security:**
- Authentication is implemented with a local JWT issuer and dev-only hardcoded credentials. This is noted as incomplete — production requires Entra ID integration. The `ChangedBy` field is now populated from the frontend's decoded token rather than a free-text input, but it is still accepted as a request body field and not verified server-side against the token claims. A server-side claim extraction would close that gap.
- The JWT secret in `appsettings.json` is a dev placeholder. Production must override it via `JWT__Secret` environment variable; it must never be committed with a real value.

**Data leakage:**
- The seed data uses fictional client names and deal references. No real customer data was pasted into any AI prompt.

**Correctness:**
- Dapper queries checked for correct parameter passing (named parameters matching SP `@Param` names exactly)
- `OFFSET … FETCH` requires an `ORDER BY` clause in SQL Server — verified the SP includes one
- Integration test isolation checked: each test class calls `ResetTablesAsync()` in `InitializeAsync()`, which deletes all rows and reseeds identity columns to zero, preventing test-order dependencies

**Hallucination:**
- Package versions were verified on NuGet rather than trusted from AI output
- All SQL was executed against a real SQL Server instance (Docker) and observed to produce correct results before being committed
