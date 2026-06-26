CREATE PROCEDURE [dbo].[usp_DealException_GetAll]
    @Status   NVARCHAR(20)  = NULL,
    @Priority NVARCHAR(20)  = NULL,
    @Search   NVARCHAR(200) = NULL,
    @OpenOnly BIT           = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [Id], [DealRef], [ClientName], [ExceptionType],
        [Priority], [Status], [AssignedOwner],
        [CreatedAt], [UpdatedAt], [IsPossibleDuplicate]
    FROM [dbo].[DealExceptions]
    WHERE
        (@Status   IS NULL OR [Status]   = @Status)
        AND (@Priority IS NULL OR [Priority] = @Priority)
        AND (@Search   IS NULL OR [DealRef]    LIKE '%' + @Search + '%'
                               OR [ClientName] LIKE '%' + @Search + '%')
        AND (@OpenOnly = 0 OR [Status] NOT IN ('Closed', 'Rejected', 'Approved'))
    ORDER BY
        CASE [Priority]
            WHEN 'Critical' THEN 1
            WHEN 'High'     THEN 2
            WHEN 'Medium'   THEN 3
            WHEN 'Low'      THEN 4
        END,
        [CreatedAt] DESC;
END
GO
