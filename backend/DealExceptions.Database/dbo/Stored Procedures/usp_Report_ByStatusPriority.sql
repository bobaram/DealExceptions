CREATE PROCEDURE [dbo].[usp_Report_ByStatusPriority]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT [Status], [Priority], COUNT(*) AS [Count]
    FROM [dbo].[DealExceptions]
    GROUP BY [Status], [Priority]
    ORDER BY [Status], [Priority];
END
GO
