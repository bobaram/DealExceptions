-- Deal Exceptions Tracker — Reporting Queries
-- These queries match the /api/reports/* endpoints in the backend.
-- Run directly against the database for ad-hoc reporting or ExCo exports.

-- ─────────────────────────────────────────────
-- 1. Open exceptions by owner
-- ─────────────────────────────────────────────
SELECT
    COALESCE("AssignedOwner", '(Unassigned)') AS "Owner",
    COUNT(*)                                   AS "OpenCount"
FROM "DealExceptions"
WHERE "Status" NOT IN ('Closed','Rejected','Approved')
GROUP BY "AssignedOwner"
ORDER BY "OpenCount" DESC;


-- ─────────────────────────────────────────────
-- 2. Critical exceptions open longer than 3 days
-- ─────────────────────────────────────────────
SELECT
    "Id",
    "DealRef",
    "ClientName",
    COALESCE("AssignedOwner", '(Unassigned)') AS "Owner",
    "CreatedAt",
    "Status",
    EXTRACT(DAY FROM NOW() - "CreatedAt")::INT AS "DaysOpen"
FROM "DealExceptions"
WHERE "Priority" = 'Critical'
  AND "Status" NOT IN ('Closed','Approved','Rejected')
  AND "CreatedAt" <= NOW() - INTERVAL '3 days'
ORDER BY "CreatedAt";


-- ─────────────────────────────────────────────
-- 3. Exception count grouped by status and priority
-- ─────────────────────────────────────────────
SELECT
    "Status",
    "Priority",
    COUNT(*) AS "Count"
FROM "DealExceptions"
GROUP BY "Status", "Priority"
ORDER BY "Status", "Priority";


-- ─────────────────────────────────────────────
-- 4. Average days to close by exception type
-- ─────────────────────────────────────────────
SELECT
    "ExceptionType",
    ROUND(AVG(EXTRACT(EPOCH FROM ("UpdatedAt" - "CreatedAt")) / 86400)::NUMERIC, 1) AS "AvgDaysToClose"
FROM "DealExceptions"
WHERE "Status" IN ('Closed','Approved','Rejected')
GROUP BY "ExceptionType"
ORDER BY "ExceptionType";


-- ─────────────────────────────────────────────
-- 5. Possible duplicate rows (from Excel import)
-- ─────────────────────────────────────────────
SELECT
    "Id",
    "LegacyId",
    "DealRef",
    "ClientName",
    "Status",
    "CreatedAt"
FROM "DealExceptions"
WHERE "IsPossibleDuplicate" = TRUE
ORDER BY "DealRef";
