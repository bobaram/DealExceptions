CREATE PROCEDURE [dbo].[usp_Report_CriticalOverdue]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [Id], [DealRef], [ClientName],
        ISNULL([AssignedOwner], '(Unassigned)') AS [Owner],
        [CreatedAt], [Status],
        DATEDIFF(DAY, [CreatedAt], SYSUTCDATETIME()) AS [DaysOpen]
    FROM [dbo].[DealExceptions]
    WHERE [Priority] = 'Critical'
      AND [Status] NOT IN ('Closed', 'Approved', 'Rejected')
      AND DATEDIFF(DAY, [CreatedAt], SYSUTCDATETIME()) > 3
    ORDER BY [CreatedAt];
END
GO
