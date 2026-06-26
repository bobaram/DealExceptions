CREATE PROCEDURE [dbo].[usp_Report_AvgTimeToClose]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [ExceptionType],
        ROUND(AVG(CAST(DATEDIFF(HOUR, [CreatedAt], [UpdatedAt]) AS FLOAT) / 24), 1) AS [AvgDaysToClose]
    FROM [dbo].[DealExceptions]
    WHERE [Status] IN ('Closed', 'Approved', 'Rejected')
    GROUP BY [ExceptionType]
    ORDER BY [ExceptionType];
END
GO
