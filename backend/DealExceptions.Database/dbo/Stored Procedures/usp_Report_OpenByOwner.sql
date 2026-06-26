CREATE PROCEDURE [dbo].[usp_Report_OpenByOwner]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ISNULL([AssignedOwner], '(Unassigned)') AS [Owner],
        COUNT(*) AS [Count]
    FROM [dbo].[DealExceptions]
    WHERE [Status] NOT IN ('Closed', 'Rejected', 'Approved')
    GROUP BY [AssignedOwner]
    ORDER BY [Count] DESC;
END
GO
