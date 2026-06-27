# Deal Exceptions Tracker

A replacement for a legacy PowerApps/SharePoint shadow app used by a lending business to track deal exceptions.

---

## How to Run

### Docker (recommended)

**Prerequisites:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) with the Linux engine running.

> **Windows note:** Docker Desktop must be fully started before running the command below. If you see a `dockerDesktopLinuxEngine` pipe error, open Docker Desktop and wait for it to finish initialising, then run:
> ```bash
> docker context use default
> ```

```bash
docker compose up --build
```

| Service  | URL                           |
|----------|-------------------------------|
| Frontend | http://localhost:3000         |
| API      | http://localhost:8080         |
| Swagger  | http://localhost:8080/swagger |

On first run Docker will pull the SQL Server 2022 image (~1.5 GB) — this takes a few minutes depending on your connection. Subsequent runs use the cached image and start in seconds.

The startup sequence is:
1. **SQL Server** starts and passes a health check
2. **DbUp** migrations run — creates tables, stored procedures, and seeds 12 normalised legacy exceptions
3. **API** starts and listens on port 8080
4. **Frontend** (nginx serving the Vite build) starts on port 3000

**Login credentials (dev only):**

| Username | Password     | Display name  |
|----------|--------------|---------------|
| admin    | Admin@123    | Admin User    |
| analyst  | Analyst@123  | Deal Analyst  |

To stop and remove containers (data is persisted in a named volume):
```bash
docker compose down
```

To also wipe the database volume:
```bash
docker compose down -v
```

---

### Local dev (without Docker)

**Prerequisites:**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local instance, SQL Express, or [LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb))
- [Node.js 20+](https://nodejs.org/)

**1. Run the database migrations**

```bash
cd backend/DealExceptions.Database.DbUp
dotnet run --ConnectionStrings:DefaultConnection="Server=localhost;Database=dealexceptions;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
```

This creates the schema, stored procedures, and seeds the data. It is safe to run multiple times (idempotent).

**2. Start the API**

```bash
cd backend/DealExceptions.Api
dotnet run
```

Update `appsettings.json` (or set the env var) with your local SQL Server connection string if it differs from the default.

**3. Start the frontend**

```bash
cd frontend
npm install
npm run dev     # dev server proxies /api/* to localhost:8080
```

Frontend dev server: http://localhost:5173

---

## What Is Complete

- **Listing exceptions** with filters: status, priority, free-text search, open-only toggle, and server-side pagination
- **Viewing an exception** with full comment and status-change history
- **Creating a new exception** via a form with client-side validation; submitting user populated from the logged-in identity
- **Updating status** with an audit trail entry written on every change, attributed to the authenticated user
- **Adding comments** — stored as immutable history, not overwriting like the legacy app
- **Priority highlighting** — Critical and High badges, critical-overdue warning banner
- **Duplicate flagging** — automatic detection when the same DealRef or the same ClientName + ExceptionType combination exists on any open exception; both the new and existing rows are flagged
- **Four reporting endpoints + Reports tab**: open-by-owner, critical-overdue (>3 days), by-status-and-priority, avg-time-to-close; visualised as bar charts, a pivot table, and a sortable table
- **Seed data** normalised from the legacy CSV: inconsistent priorities (`H`, `critical`, `Critical `), statuses (`CLOSEd`, `InReview`, `APPROVED`), dates (three formats), and owner names resolved
- **JWT Bearer authentication** — login page, Bearer token issued on `POST /api/auth/login`, all other endpoints protected by a fallback policy; display name from token replaces free-text author fields
- **CORS** — configurable allowed origins via `Cors:AllowedOrigins` (comma-separated), overridable per environment
- **Unit tests** — 28 tests covering `ExceptionService` and `CommentService` with Moq
- **Integration tests** — 28 tests against a real SQL Server test database using `WebApplicationFactory`, covering CRUD, validation, duplicate detection, and pagination
- **Swagger UI** with Bearer security definition for manual API testing
- **Docker Compose** for one-command startup
- **DbUp migrations** — idempotent schema + stored procedure scripts run on every startup; all data access through stored procedures (`usp_*`)

---

## What Is Incomplete

- **Owner management** — owners are stored as free text. Production would join to an HR/identity list to prevent the same person being captured as "Nomsa", "Nomsa Mokoena", and "nomsa.mokoena@sourcefin.example"
- **Azure Entra ID integration** — authentication is implemented with a local JWT issuer and hardcoded dev users. In production this would sit behind the company's identity provider (Azure AD / Entra ID given the SharePoint heritage); `ChangedBy` and `AuthorName` would be extracted from token claims server-side rather than passed from the client
- **Role-based authorisation** — all authenticated users can perform all actions. A production system would gate Approve and Reject transitions behind an admin or finance-reviewer role
- **File attachments** — the business notes that affordability exceptions require supporting documents. Not implemented
- **Email/notification integration** — no alerts when a critical exception approaches the 3-day threshold
- **Frontend tests** — no React component or end-to-end tests

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
- Authentication wired to the company identity provider (Azure Entra ID)
- A named business owner for the new system who can raise and prioritise bugs
- Runbook written covering deployments, data backups, and incident response

---

## Working in a Small Team

### Split across yourself + 3 developers + 1 tester

**You (tech lead / backend):**
- Own the architecture, PR reviews, and anything touching the database schema
- Build the Azure Entra ID integration to replace the current dev-only JWT issuer
- Write the data migration script and run the normalisation workshops with the business

**Developer 2 (backend):**
- Add role-based authorisation (admin/finance-reviewer gates on Approve/Reject transitions)
- Build the owner-lookup service against the identity provider or HR feed
- Add rate limiting and hardening to the login endpoint

**Developer 3 (frontend):**
- Build the owner autocomplete picker once the backend lookup exists
- Add a CSV/Excel export button for the exceptions list
- Write React component tests and Playwright end-to-end tests

**Developer 4 (full-stack):**
- Build the notification system (email alerts for critical exceptions approaching 3-day threshold)
- Add file attachment support (upload to blob storage, link to exception)
- Own the CI/CD pipeline and deployment runbook

**Tester:**
- Own the UAT process with the business team
- Write and maintain the test cases for all informal business rules
- Run parallel-running validation: same action in old and new system, compare outputs

---

### Example backlog items

**DE-01 — Entra ID integration: replace dev JWT issuer with company identity provider**

> As a system, I need every status change and comment to record the authenticated user's verified identity from the company directory, so the audit trail is trustworthy and cannot be spoofed.

Acceptance criteria:
- All API endpoints require a valid Bearer token issued by Azure Entra ID
- `ChangedBy` and `AuthorName` are extracted from token claims server-side, not accepted from the request body
- The frontend redirects to the Entra ID login page when no valid session exists
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

**DE-03 — Role-based authorisation: gate Approve/Reject behind a reviewer role**

> As a compliance officer, I need only authorised reviewers to be able to approve or reject exceptions, so that the audit trail reflects a genuine decision by a qualified person.

Acceptance criteria:
- Users with the `reviewer` role can transition exceptions to Approved or Rejected
- Users without the role see the status options disabled in the UI
- Attempts to call the status endpoint with an unauthorised transition return 403
- Role membership is sourced from Entra ID group claims, not maintained in the application database
