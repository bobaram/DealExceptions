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

## Migration Notes

The app is built and the data model is solid, but getting the live SharePoint data across cleanly is the hardest part of the whole project. A few things we already know from looking at the legacy data.

### What we know so far

The data is messier than it looks from the SharePoint UI. When we normalised the seed records we found priorities written as `"H"`, `"critical "` (trailing space), and `"Critical"` all meaning the same thing. Dates in three different formats. Row 1005 is a clear duplicate from an export-reimport cycle — there are almost certainly more. Legacy ID 1011 was never in SharePoint at all; it only existed in an email chain.

The SharePoint list also accepts writes that bypass PowerApps entirely — anyone with direct list edit access can put anything in any field, no validation applied. We don't know how much data came in that way, but we should assume some did.

The other risk is the app builder. A lot of the business logic only exists in PowerApps formulas — Pricing Override needing Finance review, the 3-day rule for Critical exceptions, what a valid close note looks like. If that person isn't available for the migration, some of those rules will get lost.

### How we're planning to migrate

The plan is to export everything to CSV, run it through a normalisation script (same logic as the seed data, just applied to the full dataset), and produce a diff report before anything touches production. Every transformation gets documented — not just the result, but why. The business owner reviews that diff and signs off before we import.

We'll preserve the `LegacyId` on every row so there's always a way to trace back to the source record if something looks wrong after go-live.

The old system stays in read-only mode for at least 60 days. We're not deleting it.

### What we're not rebuilding yet

The Excel export is staying as-is for now. People have built reports and pivot tables on top of those exports and we're not pulling that out from under them. We'll add a CSV download endpoint and let teams migrate their templates in their own time.

Same with the exception type and priority choices — those are config data, not something that needs an admin UI in the first phase.

---

## What's Next

The core system is running. Here's what needs to happen before this goes to production.

**Entra ID integration** is the blocker for everything else. The current login uses a local JWT issuer with two hardcoded accounts — that's fine for development but can't go live. Once we have Entra ID wired up, `ChangedBy` and `AuthorName` get pulled from token claims server-side rather than trusted from the client, and the owner picker has a real user directory to query against.

**Role-based authorisation** is straightforward once Entra ID is in place. Approve and Reject should be gated behind a reviewer role — right now anyone who can log in can do anything. The role membership will come from Entra ID group claims so there's nothing to maintain in the application database.

**Owner picker** replaces the free-text owner field. At the moment "Nomsa", "Nomsa Mokoena", and "nomsa.mokoena@example.com" are three different people as far as the reporting is concerned. Once we have a user directory endpoint, the frontend field becomes a searchable dropdown and the reporting cleans up automatically.

**Notifications** for critical exceptions approaching the 3-day threshold. The data is all there — the overdue report already surfaces them. It just needs an email trigger.

**File attachments** for affordability exceptions. The business flagged this early; it'll need blob storage and a link on the exception record.

**Frontend tests** — the backend has good coverage but the React side has none. Playwright end-to-end tests covering the main workflows before production.

---

## Team

The current split for getting this to production:

**Tech lead** — Entra ID integration, data migration script, normalisation workshops with the business, schema decisions, PR reviews.

**Backend developer** — role-based authorisation, owner lookup service, rate limiting and security hardening on the auth endpoints.

**Frontend developer** — owner autocomplete picker, CSV export button, React component tests, Playwright end-to-end tests.

**Full-stack developer** — email notification system, file attachments, CI/CD pipeline, deployment runbook.

**Tester** — UAT with the business team, test cases for every informal business rule we surface in the workshops, parallel-running validation between old and new systems.

---

## Open Tickets

**DE-01 — Entra ID: swap out the local JWT issuer**

The two hardcoded dev accounts need to go. Wire up Entra ID so every token comes from the company identity provider. Once that's done, pull `ChangedBy` and `AuthorName` from the token claims on the server side rather than accepting them from the request body — right now a client could pass any value there.

**DE-02 — Owner picker: fix the owner name problem**

`GET /api/users` needs to return a validated list from the identity provider or HR feed. The frontend owner field becomes a searchable dropdown. Existing rows with unrecognised owner strings get flagged for review, and the migration script will need a pass to resolve the legacy variants to canonical identities.

**DE-03 — Roles: only reviewers should be able to Approve or Reject**

Add a `reviewer` role sourced from Entra ID group claims. Gate the Approve and Reject status transitions behind it. Everyone else sees those options disabled. Unauthorised attempts return 403.
