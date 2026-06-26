CREATE PROCEDURE [dbo].[usp_DealException_UpdateStatus]
    @Id        INT,
    @Status    NVARCHAR(20),
    @ChangedBy NVARCHAR(200),
    @Notes     NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OldStatus NVARCHAR(20);
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    SELECT @OldStatus = [Status] FROM [dbo].[DealExceptions] WHERE [Id] = @Id;

    IF @OldStatus IS NULL RETURN; -- not found

    UPDATE [dbo].[DealExceptions]
    SET [Status] = @Status, [UpdatedAt] = @Now
    WHERE [Id] = @Id;

    INSERT INTO [dbo].[StatusHistories]
        ([ExceptionId], [FromStatus], [ToStatus], [ChangedBy], [ChangedAt], [Notes])
    VALUES
        (@Id, @OldStatus, @Status, @ChangedBy, @Now, @Notes);
END
GO
