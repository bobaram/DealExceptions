CREATE PROCEDURE [dbo].[usp_Comment_Create]
    @ExceptionId INT,
    @AuthorName  NVARCHAR(200),
    @Text        NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    INSERT INTO [dbo].[Comments] ([ExceptionId], [AuthorName], [Text], [CreatedAt])
    VALUES (@ExceptionId, @AuthorName, @Text, @Now);

    UPDATE [dbo].[DealExceptions]
    SET [UpdatedAt] = @Now
    WHERE [Id] = @ExceptionId;

    SELECT SCOPE_IDENTITY() AS [Id];
END
GO
