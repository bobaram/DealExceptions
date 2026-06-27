# Deal Exceptions Tracker

This is a replacement for a legacy PowerApps/SharePoint app used by a lending business to track deal exceptions. The goal was to take something brittle and hard to maintain and turn it into a proper, testable system without losing the data or the workflow the business relies on.

---

## How to Run

### The easy way — Docker

You'll need [Docker Desktop](https://www.docker.com/products/docker-desktop/) running. If you're on Windows, make sure the Docker Desktop window is fully started before running anything — if you get a pipe error, run `docker context use default` first.

```bash
docker compose up --build
```

That's it. The first run pulls the SQL Server 2022 image which is about 1.5 GB, so it takes a few minutes. After that it's cached and starts in seconds.

Once everything is up:

| What          | URL                           |
|---------------|-------------------------------|
| App           | http://localhost:3000         |
| API           | http://localhost:8080         |
| Swagger UI    | http://localhost:8080/swagger |

**Login credentials:**

| Username | Password     |
|----------|--------------|
| admin    | Admin@123    |
| analyst  | Analyst@123  |

Behind the scenes, Docker starts four things in order: SQL Server, a DbUp migration runner that creates the schema and seeds data, the .NET API, and an nginx container serving the built React app.

To stop everything:
```bash
docker compose down
```

To stop and also wipe the database (fresh start):
```bash
docker compose down -v
```

---

### Running locally without Docker

If you'd rather run things directly, you'll need:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A SQL Server instance (local, SQL Express, or LocalDB)
- [Node.js 20+](https://nodejs.org/)

**Step 1 — Run the migrations**

```bash
cd backend/DealExceptions.Database.DbUp
dotnet run --ConnectionStrings:DefaultConnection="Server=localhost;Database=dealexceptions;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
```

This creates all the tables, stored procedures, and seeds the 12 normalised records from the legacy data. You can run it multiple times safely — everything is idempotent.

**Step 2 — Start the API**

```bash
cd backend/DealExceptions.Api
dotnet run
```

If your SQL Server connection string differs from the default in `appsettings.json`, either edit that file or set the `ConnectionStrings__DefaultConnection` environment variable.

**Step 3 — Start the frontend**

```bash
cd frontend
npm install
npm run dev
```

The dev server runs at http://localhost:5173 and proxies all `/api/*` calls to the backend at port 8080.

---

## What's Done

- **Full exception lifecycle** — create, view, update status, add comments. Status changes write a full audit trail entry every time including who changed it and when. Comments are immutable history, not an overwriting "latest note" like the legacy app.
- **Filtering and search** — filter by status, priority, free-text search across deal ref and client name, and an open-only toggle. All server-side.
- **Pagination** — server-side with configurable page size, total count returned alongside results.
- **Priority visibility** — Critical and High exceptions get coloured badges throughout the UI. There's also a warning banner that surfaces critical exceptions open for more than 3 days.
- **Duplicate detection** — when a new exception is created, the system checks whether any open exception shares the same DealRef or the same ClientName + ExceptionType combination. If it finds a match, both the new exception and the existing one get flagged as possible duplicates.
- **Reports tab** — four views: open exceptions by owner (bar chart), critical and overdue list (sortable table), exceptions by status and priority (pivot matrix), and average time to close by exception type (bar chart).
- **JWT authentication** — login page, Bearer token on every request, all endpoints protected by default. The `ChangedBy` and `AuthorName` fields come from the logged-in user, not a free-text field.
- **CORS** — configurable allowed origins via `Cors:AllowedOrigins` in config, overridable per environment.
- **Tests** — 28 unit tests (ExceptionService, CommentService) and 28 integration tests that run against a real SQL Server test database using WebApplicationFactory.
- **Seed data** — the legacy CSV had a mess of inconsistent values (`"H"`, `"critical "`, `"CLOSEd"`, three different date formats, the same person under three different names). All of that is normalised in the seed scripts, with the original legacy IDs preserved for traceability.
- **Swagger UI** — available at `/swagger` with a Bearer security definition so you can test authenticated endpoints directly.

---

## What's Not Done

A few things were deliberately left out given the time constraint, but they're worth flagging:

- **Azure Entra ID** — the current auth uses a local JWT issuer with two hardcoded dev users. In production this would integrate with the company's identity provider (almost certainly Azure Entra ID given the SharePoint background). The `Jwt:Secret` in `appsettings.json` is a dev placeholder and must be overridden in production.
- **Role-based authorisation** — everyone who can log in can do everything. A real deployment would want finance reviewers to be the only ones who can Approve or Reject, with other users able to create and move exceptions through earlier statuses.
- **Owner picker** — the assigned owner field is still free text. This means the same person can appear as "Nomsa", "Nomsa Mokoena", and "nomsa.mokoena@example.com" in reports. A proper implementation would pull from the identity provider or HR feed and use a searchable dropdown.
- **File attachments** — the business notes that affordability exceptions need supporting documents. Not implemented.
- **Email notifications** — no alerts when a critical exception is approaching the 3-day threshold.
- **Frontend tests** — no React component or end-to-end tests.

---

## Converting the Shadow App

### What to investigate before writing any code

The legacy system has a lot of hidden complexity. Before touching anything in production:

- Pull every column out of the SharePoint list, including hidden and computed columns that aren't visible in the default view
- Go through every PowerApps formula, especially the validation rules and patch logic — some business rules only exist in formulas and aren't written down anywhere
- Find all the Excel export templates and any macros or pivot tables built on top of them — people have built things on this data that you don't know about
- Find out who has direct SharePoint edit access and is bypassing PowerApps to modify rows. The data coming out of that list is not all coming through the same validation path
- Track down every historical value of ExceptionType and Priority — deleted choices still exist on old rows

### Risks that need to be understood before migrating

- **Duplicate rows.** The export-edit-reimport cycle has been creating duplicates. At least one was found in the seed data (row 1005). There are likely more that won't be obvious until you look closely.
- **Data living in email chains.** Information about some exceptions was captured in emails and never made it into SharePoint. Legacy ID 1011 is an example. A migration that only looks at the SharePoint list will miss this.
- **Validation that only exists in the UI.** The SharePoint list itself will accept anything. Rows written directly to SharePoint (bypassing PowerApps) have no validation applied. Assume the data is messier than it looks.
- **One person holds the context.** The original app builder knows rules that aren't documented anywhere. If that person leaves before those rules are captured, they're gone.
- **Reporting that ExCo trusts but shouldn't.** The current reports are built on messy data. When you clean the data up during migration, the numbers will change. That needs to be managed carefully — leadership will notice and it needs an explanation ready.

### How to approach the migration

1. Export the full SharePoint list to CSV. Export comments separately if they're in a related list.
2. Write a one-off normalisation script. Keep a `LegacyId` column in every row so you can always trace back to the source.
3. Run the script against a staging environment and produce a diff report — exactly what changed and why. Don't just import; document every transformation.
4. Get the business owner to review the diff before anything goes near production.
5. Keep the old system in read-only mode for at least 60 days after the migration. Don't delete it.

### Involving the business

- Run structured workshops with the app builder before writing any replacement logic. The goal is to surface every informal rule and get it written down.
- Run both systems in parallel during UAT so users can directly compare the behaviour.
- Find one power user in the business team to own the acceptance criteria and do hands-on testing.
- Don't decomission the old app until someone in the business signs off in writing that the new one is ready.

### What not to rebuild immediately

- **The Excel export.** Users trust it and ExCo reports depend on it. Add a CSV download endpoint and let them migrate their templates in their own time.
- **The Admin Choices screen.** Leave exception types and priorities as config data for now rather than building an admin UI in the first phase.
- **Anything in PowerApps that isn't documented.** Don't try to replicate behaviour you don't fully understand.

### What needs to be true before Tech & Data takes ownership

- Every informal business rule has been documented and reviewed by the business owner
- The data migration has been validated and signed off
- At least one full sprint of parallel running — both old and new systems live at the same time
- Authentication integrated with the company identity provider
- A named business owner for the new system who can raise bugs and set priorities
- A runbook covering deployments, database backups, and what to do when something breaks

---

## Working as a Team

Here's how I'd split this across a team of four developers and a tester, assuming the core build is done and the work now is to harden and hand over.

**Tech lead (me):**
Own the architecture and PR reviews. Lead the Entra ID integration to replace the dev JWT issuer. Write the data migration script and run the normalisation workshops with the business. Make the final call on anything touching the database schema.

**Developer 2 — backend:**
Build role-based authorisation (admin/reviewer gates on Approve and Reject). Add the owner lookup service against the identity provider or HR feed. Add rate limiting on the login endpoint and any other hardening before production.

**Developer 3 — frontend:**
Build the owner autocomplete picker once the backend lookup is ready. Add a CSV export button. Write React component tests and Playwright end-to-end tests.

**Developer 4 — full stack:**
Build the notification system for critical exceptions approaching the 3-day threshold. Add file attachment support. Own the CI/CD pipeline and deployment runbook.

**Tester:**
Own UAT with the business team. Write and maintain test cases for all the informal business rules uncovered in the workshops. Run the parallel-running validation — same action in both systems, compare the output.

---

### Backlog items

**DE-01 — Entra ID: replace dev JWT issuer with the company identity provider**

The current auth works but uses hardcoded users and a local JWT issuer. Production needs to go through Entra ID.

Acceptance criteria:
- All endpoints require a valid Bearer token from Entra ID
- `ChangedBy` and `AuthorName` are extracted from token claims server-side, not passed in the request body
- The frontend redirects to the Entra ID login page when no valid session exists
- Swagger supports the OAuth2 implicit flow for manual testing

---

**DE-02 — Owner lookup: replace the free-text owner field with a validated picker**

Right now "Nomsa", "Nomsa Mokoena", and "nomsa.mokoena@example.com" are three different owners in reports. They shouldn't be.

Acceptance criteria:
- `GET /api/users` returns valid owners from the identity provider or HR feed
- The frontend owner field is a searchable dropdown
- Existing rows with unrecognised owner strings are flagged for review
- Legacy owner variants are resolved to canonical identities during migration

---

**DE-03 — Role-based authorisation: only reviewers can Approve or Reject**

Acceptance criteria:
- Users with the `reviewer` role can transition exceptions to Approved or Rejected
- Other users see those options disabled
- Attempts to call the endpoint without the right role return 403
- Role membership comes from Entra ID group claims, not from the application database
