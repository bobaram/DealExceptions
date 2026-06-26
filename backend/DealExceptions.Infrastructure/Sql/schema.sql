-- Deal Exceptions Tracker — PostgreSQL Schema
-- Run this against a fresh database to create all tables.
-- EF Core EnsureCreated() generates the same structure at runtime;
-- this file exists as a standalone deliverable and migration baseline.

CREATE TABLE IF NOT EXISTS "DealExceptions" (
    "Id"                  SERIAL          PRIMARY KEY,
    "DealRef"             VARCHAR(50)     NOT NULL,
    "ClientName"          VARCHAR(200)    NOT NULL,
    "ExceptionType"       VARCHAR(100)    NOT NULL,
    "Description"         TEXT            NOT NULL,
    "Priority"            VARCHAR(20)     NOT NULL CHECK ("Priority" IN ('Low','Medium','High','Critical')),
    "Status"              VARCHAR(20)     NOT NULL CHECK ("Status"   IN ('New','Pending','InReview','Approved','Rejected','Closed')),
    "AssignedOwner"       VARCHAR(200)    NULL,
    "CreatedAt"           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "UpdatedAt"           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "LegacyId"            INT             NULL,
    "IsPossibleDuplicate" BOOLEAN         NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS "Comments" (
    "Id"          SERIAL       PRIMARY KEY,
    "ExceptionId" INT          NOT NULL REFERENCES "DealExceptions"("Id") ON DELETE CASCADE,
    "AuthorName"  VARCHAR(200) NOT NULL,
    "Text"        TEXT         NOT NULL,
    "CreatedAt"   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "LegacyId"    INT          NULL
);

CREATE TABLE IF NOT EXISTS "StatusHistories" (
    "Id"          SERIAL      PRIMARY KEY,
    "ExceptionId" INT         NOT NULL REFERENCES "DealExceptions"("Id") ON DELETE CASCADE,
    "FromStatus"  VARCHAR(20) NOT NULL,
    "ToStatus"    VARCHAR(20) NOT NULL,
    "ChangedBy"   VARCHAR(200) NOT NULL,
    "ChangedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "Notes"       TEXT        NULL
);

-- Indexes for common query patterns
CREATE INDEX IF NOT EXISTS ix_exceptions_status   ON "DealExceptions"("Status");
CREATE INDEX IF NOT EXISTS ix_exceptions_priority ON "DealExceptions"("Priority");
CREATE INDEX IF NOT EXISTS ix_exceptions_owner    ON "DealExceptions"("AssignedOwner");
CREATE INDEX IF NOT EXISTS ix_comments_exception  ON "Comments"("ExceptionId");
CREATE INDEX IF NOT EXISTS ix_history_exception   ON "StatusHistories"("ExceptionId");
