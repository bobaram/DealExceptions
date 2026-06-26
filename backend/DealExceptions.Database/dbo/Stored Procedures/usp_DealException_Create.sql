CREATE PROCEDURE [dbo].[usp_DealException_Create]
    @DealRef       NVARCHAR(50),
    @ClientName    NVARCHAR(200),
    @ExceptionType NVARCHAR(100),
    @Description   NVARCHAR(MAX),
    @Priority      NVARCHAR(20),
    @AssignedOwner NVARCHAR(200) = NULL,
    @CreatedBy     NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Id INT;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    INSERT INTO [dbo].[DealExceptions]
        ([DealRef], [ClientName], [ExceptionType], [Description], [Priority], [Status], [AssignedOwner], [CreatedAt], [UpdatedAt])
    VALUES
        (@DealRef, @ClientName, @ExceptionType, @Description, @Priority, 'New', @AssignedOwner, @Now, @Now);

    SET @Id = SCOPE_IDENTITY();

    INSERT INTO [dbo].[StatusHistories]
        ([ExceptionId], [FromStatus], [ToStatus], [ChangedBy], [ChangedAt], [Notes])
    VALUES
        (@Id, 'New', 'New', @CreatedBy, @Now, 'Created');

    SELECT @Id AS [Id];
END
GO
