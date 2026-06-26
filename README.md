# Deal Exceptions Tracker

A replacement for a legacy PowerApps/SharePoint shadow app used by a lending business to track deal exceptions.

---

## How to Run

**Prerequisites:** Docker + Docker Compose

```bash
cd solution
docker compose up --build
```

| Service  | URL                                    |
|----------|----------------------------------------|
| Frontend | http://localhost:3000                  |
| API      | http://localhost:8080                  |
| Swagger  | http://localhost:8080/swagger          |

The backend runs EF Core `EnsureCreated()` on startup and seeds the 12 normalised legacy exceptions automatically.

**Local dev (without Docker):**

```bash
# Backend — requires .NET 8 SDK and a local PostgreSQL instance
cd backend
dotnet run --ConnectionStrings:DefaultConnection="Host=localhost;Database=dealexceptions;Username=postgres;Password=postgres"

# Frontend — requires Node 20+
cd frontend
npm install
npm run dev     # proxies /api to localhost:8080
```

---

## What Is Complete

- **Listing exceptions** with filters: status, priority, free-text search, open-only toggle
- **Viewing an exception** with full comment and status-change history
- **Creating a new exception** via a form with client-side validation
- **Updating status** with an audit trail entry written on every change
- **Adding comments** — stored as immutable history, not overwriting like the legacy app
- **Priority highlighting** — Critical and High badges, critical-overdue warning banner
- **Duplicate flagging** — row 1005 (Excel import duplicate) surfaced with a visible warning
- **Four reporting endpoints**: open-by-owner, critical-overdue (>3 days), by-status-and-priority, avg-time-to-close
- **Seed data** normalised from the legacy CSV: inconsistent priorities (`H`, `critical`, `Critical `), statuses (`CLOSEd`, `InReview`, `APPROVED`), dates (three formats), and owner names resolved
- **Swagger UI** for API exploration
- **Docker Compose** for one-command startup

---

## What Is Incomplete

- **Authentication / authorisation** — the API is open. In production this would sit behind the company's identity provider (Azure AD / Entra ID is likely given the SharePoint heritage). The `ChangedBy` / `CreatedBy` fields are currently free-text inputs; they would be populated from the JWT claims.
- **EF Core migrations** — startup uses `EnsureCreated()` which is fine for a demo but does not support incremental schema changes. The next step is `dotnet ef migrations add Initial` to produce versioned migrations.
- **Owner management** — owners are stored as free text. Production would join to an HR/identity list to prevent the same person being captured as "Nomsa", "Nomsa Mokoena", and "nomsa.mokoena@sourcefin.example".
- **File attachments** — the business notes that affordability exceptions require supporting documents. This is not implemented.
- **Email/notification integration** — no alerts when a critical exception approaches the 3-day threshold.
- **Pagination** — the list endpoint returns all records. Acceptable at current data volume but needs adding before the dataset grows.
- **Frontend tests** — no unit or integration tests written in the time available.

---

## Converting the Shadow App

### What to investigate in the existing solution

- All SharePoint List columns, including hidden or computed ones not visible in the default view
- Every PowerApps formula, especially validation rules and patch logic — some business rules only exist here
- All Excel export templates and any macros or pivot tables built on them
- Who has SharePoint edit access and who bypasses PowerApps to change rows directly
- The full history of ExceptionType and Priority choice values — deleted options still exist on old rows

### Risks to look for

- **Duplicate rows** from the export-edit-reimport cycle (confirmed: at least one found)
- **Data in email chains** that was never captured in SharePoint (confirmed: legacy ID 1011)
- **Validation only in the UI** — the SharePoint list accepts any value directly, so data written outside PowerApps bypasses all rules
- **Single point of knowledge** — the original app builder holds undocumented rules in their head
- **Trusted-but-wrong reporting** — ExCo receives reports built on this data; cleaning it up could surface figures that differ from what leadership expects

### How to migrate the data

1. Export the full SharePoint list to CSV and the comments list separately
2. Write a one-off normalisation script (priority, status, owner name, date format) — keep the `LegacyId` column as a traceability link
3. Run the script against a staging environment first; produce a diff report showing exactly what was normalised and why
4. Have the business owner review the diff before any production import
5. Keep the legacy system read-only (not deleted) for 60 days post-migration as a fallback

### How to involve the original business users

- Run two or three structured workshops with the app builder to capture every informal rule before writing a line of replacement code
- Use the legacy app side-by-side during UAT so users can compare behaviour directly
- Identify a power user in the business team as the go-to tester and future owner of acceptance criteria
- Do not decommission the old app until the business owner signs off in writing

### What to not immediately rebuild

- The Excel export — users trust it and reports are built on it. Keep a CSV download endpoint and let users migrate their Excel templates gradually
- The Admin Choices screen — leave exception types and priorities as configurable data rather than rebuilding a full admin UI in phase one
- Any PowerApps automation not yet documented — do not replicate what you do not yet understand

### What needs to be true before Tech & Data takes ownership

- All informal business rules documented and reviewed by the business owner
- Data migration validated and signed off
- At least one full sprint of parallel running (old and new system both live)
- Authentication wired to the company identity provider
- A named business owner for the new system who can raise and prioritise bugs
- Runbook written covering deployments, data backups, and incident response

---

## Working in a Small Team

### Split across yourself + 3 developers + 1 tester

**You (tech lead / backend):**
- Own the architecture, PR reviews, and anything touching the database schema
- Build the authentication integration and owner-lookup service
- Write the data migration script and run the normalisation workshops

**Developer 2 (backend):**
- Add pagination, filtering improvements, and the file-attachment endpoint
- Write integration tests for all API endpoints

**Developer 3 (frontend):**
- Extend the React UI: admin choices screen, owner autocomplete, Excel export button
- Write component tests

**Developer 4 (full-stack):**
- Build the notification system (email alerts for critical exceptions approaching 3-day threshold)
- Own the CI/CD pipeline and Docker Compose → Kubernetes migration path

**Tester:**
- Own the UAT process with the business team
- Write and maintain the test cases for all informal business rules
- Run parallel-running validation: same action in old and new system, compare outputs

---

### Example backlog items

**DE-01 — Authentication: replace free-text ChangedBy with JWT identity**

> As a system, I need every status change and comment to record the authenticated user's identity, not a manually typed name, so the audit trail is trustworthy.

Acceptance criteria:
- All API endpoints require a valid Bearer token (Azure Entra ID)
- `ChangedBy` and `AuthorName` fields are populated from the token claims, not request body
- Unauthenticated requests return 401
- Swagger UI supports the OAuth2 implicit flow for manual testing

---

**DE-02 — Owner lookup: replace free-text owner with validated user picker**

> As a deal manager, I want to assign an exception to a person from a validated list, so that owner names are consistent and reportable.

Acceptance criteria:
- `GET /api/users` returns a list of valid owners sourced from the identity provider or HR feed
- The frontend owner field is a searchable dropdown, not a free-text input
- Existing rows with unrecognised owner strings are flagged in the UI as needing review
- Legacy owner variants ("Nomsa", "nomsa.mokoena@…", "Nomsa Mokoena") are resolved to a canonical identity during migration

---

**DE-03 — EF Core migrations: replace EnsureCreated with versioned migrations**

> As a developer, I need schema changes to be versioned and applied incrementally, so that we can deploy updates to production without dropping and recreating the database.

Acceptance criteria:
- `dotnet ef migrations add Initial` produces a valid initial migration from the current model
- `docker compose up` applies pending migrations automatically on startup
- A second migration can be added and applied without data loss on a seeded database
- CI pipeline fails if there are unapplied model changes with no corresponding migration
