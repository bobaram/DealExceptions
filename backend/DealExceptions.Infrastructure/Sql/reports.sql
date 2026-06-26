-- Deal Exceptions Tracker — Ad-hoc Reporting Queries (T-SQL / SQL Server)
-- These queries match the /api/reports/* endpoints in the backend.
-- Run directly against the database for ad-hoc reporting or ExCo exports.

-- ─────────────────────────────────────────────
-- 1. Open exceptions by owner
-- ─────────────────────────────────────────────
SELECT
    ISNULL([AssignedOwner], '(Unassigned)') AS [Owner],
    COUNT(*)                                AS [OpenCount]
FROM [dbo].[DealExceptions]
WHERE [Status] NOT IN ('Closed', 'Rejected', 'Approved')
GROUP BY [AssignedOwner]
ORDER BY [OpenCount] DESC;


-- ─────────────────────────────────────────────
-- 2. Critical exceptions open longer than 3 days
-- ─────────────────────────────────────────────
SELECT
    [Id],
    [DealRef],
    [ClientName],
    ISNULL([AssignedOwner], '(Unassigned)')         AS [Owner],
    [CreatedAt],
    [Status],
    DATEDIFF(DAY, [CreatedAt], SYSUTCDATETIME())    AS [DaysOpen]
FROM [dbo].[DealExceptions]
WHERE [Priority] = 'Critical'
  AND [Status] NOT IN ('Closed', 'Approved', 'Rejected')
  AND DATEDIFF(DAY, [CreatedAt], SYSUTCDATETIME()) > 3
ORDER BY [CreatedAt];


-- ─────────────────────────────────────────────
-- 3. Exception count grouped by status and priority
-- ─────────────────────────────────────────────
SELECT
    [Status],
    [Priority],
    COUNT(*) AS [Count]
FROM [dbo].[DealExceptions]
GROUP BY [Status], [Priority]
ORDER BY [Status], [Priority];


-- ─────────────────────────────────────────────
-- 4. Average days to close by exception type
-- ─────────────────────────────────────────────
SELECT
    [ExceptionType],
    ROUND(AVG(CAST(DATEDIFF(HOUR, [CreatedAt], [UpdatedAt]) AS FLOAT) / 24), 1) AS [AvgDaysToClose]
FROM [dbo].[DealExceptions]
WHERE [Status] IN ('Closed', 'Approved', 'Rejected')
GROUP BY [ExceptionType]
ORDER BY [ExceptionType];


-- ─────────────────────────────────────────────
-- 5. Possible duplicate rows (from Excel import)
-- ─────────────────────────────────────────────
SELECT
    [Id],
    [LegacyId],
    [DealRef],
    [ClientName],
    [Status],
    [CreatedAt]
FROM [dbo].[DealExceptions]
WHERE [IsPossibleDuplicate] = 1
ORDER BY [DealRef];
