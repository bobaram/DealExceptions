# AI Usage

## Tools Used

- **Claude (claude-sonnet-4-6 via Claude Code CLI)** — primary tool throughout

---

## What I Used AI For

### Architecture and planning
Used AI to talk through the architecture before writing any code: Minimal APIs vs controllers, EF Core vs Dapper, whether to include a repository layer, and why `EnsureCreated()` is a reasonable trade-off for this scope. The recommendations were sensible and I agreed with them.

### Scaffolding boilerplate
Used AI to generate the initial versions of:
- The `.csproj` with correct package references and versions
- EF Core `DbContext` with enum-to-string conversion and cascade-delete configuration
- Vite + React + TypeScript project config (`tsconfig.json`, `vite.config.ts`, `package.json`)
- Multi-stage Dockerfiles for both backend and frontend
- nginx reverse proxy config

This saved significant time on configuration that is correct-or-broken with no middle ground.

### Data normalisation logic
Used AI to reason through the messy seed data: resolving `"H"` → `High`, `"critical "` → `Critical`, `"CLOSEd"` → `Closed`, three date formats, and three variants of the same owner name. The normalisation decisions were verified against the source CSV before accepting them.

### Endpoint and service implementation
Used AI to generate the service and endpoint implementations based on the agreed contracts. I reviewed each file for:
- Correct async usage
- Sensible error handling (ArgumentException → 400, null → 404)
- Proper EF Core query patterns (no N+1, correct use of Include)
- Validation that matched the business rules from the brief

### React components
Used AI to generate the component implementations. Reviewed for correct TanStack Query usage (`useQuery` keys, `useMutation` + `invalidateQueries`), form validation patterns, and accessibility basics.

### README
Used AI to draft the README sections, particularly the "Converting the Shadow App" and "Working in a Small Team" sections. These were based on genuine analysis of the legacy data and business user notes — AI helped structure and articulate conclusions I had already reached.

---

## What I Accepted, Changed, or Rejected

| Output | Decision | Reason |
|---|---|---|
| Repository pattern over EF Core | Rejected | Adds boilerplate without value at this slice size; services inject DbContext directly |
| Swagger + Serilog package versions | Accepted after checking | Confirmed compatible with .NET 8 on NuGet |
| `Database.Migrate()` on startup | Changed to `EnsureCreated()` | No migration files generated; noted as a next step in README |
| Redux for frontend state | Rejected | TanStack Query handles server state; no complex client state requiring Redux |
| Tailwind CSS | Rejected | Adds build config complexity; plain CSS is sufficient for this scope |
| Status badge inline styles | Accepted | Keeps components self-contained without a CSS class naming convention to maintain |

---

## How I Verified AI-Assisted Work

- Read every generated file before committing — did not commit output blindly
- Cross-checked EF Core configuration against known correct patterns (enum conversion, cascade delete)
- Verified the seed data normalisation against the source CSV row by row
- Checked that all API endpoint paths in the frontend API client match the backend endpoint registrations
- Confirmed Docker Compose service names match the nginx proxy target (`http://backend:8080`)

---

## Risks Considered

**Security:**
- The API has no authentication. This is noted as incomplete. In a real delivery, committing open endpoints to a shared repo would be a finding. The `ChangedBy` field accepting any string is a trust issue — mitigated only once auth is wired.
- No secrets in source: connection string uses placeholder credentials only present in the compose file, not hardcoded in application code.

**Data leakage:**
- The seed data uses fictional client names and deal references. No real customer data was pasted into any AI prompt.

**Correctness:**
- AI-generated EF Core queries were checked for N+1 patterns. The `GetByIdAsync` call uses `.Include()` correctly.
- Date handling in the seeder uses `DateTimeKind.Utc` explicitly to avoid timezone ambiguity with PostgreSQL.

**Hallucination:**
- Package versions were verified on NuGet rather than trusted from AI output.
- No AI-generated SQL or migration files were used — EF Core generates these from the model.
