CREATE OR ALTER PROCEDURE [dbo].[usp_DealException_GetAll]
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

CREATE OR ALTER PROCEDURE [dbo].[usp_DealException_GetById]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.[Id], e.[DealRef], e.[ClientName], e.[ExceptionType], e.[Description],
        e.[Priority], e.[Status], e.[AssignedOwner],
        e.[CreatedAt], e.[UpdatedAt], e.[IsPossibleDuplicate]
    FROM [dbo].[DealExceptions] e
    WHERE e.[Id] = @Id;

    SELECT [Id], [ExceptionId], [AuthorName], [Text], [CreatedAt]
    FROM [dbo].[Comments]
    WHERE [ExceptionId] = @Id
    ORDER BY [CreatedAt];

    SELECT [Id], [ExceptionId], [FromStatus], [ToStatus], [ChangedBy], [ChangedAt], [Notes]
    FROM [dbo].[StatusHistories]
    WHERE [ExceptionId] = @Id
    ORDER BY [ChangedAt];
END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_DealException_Create]
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

    DECLARE @Id          INT;
    DECLARE @Now         DATETIME2 = SYSUTCDATETIME();
    DECLARE @IsDuplicate BIT       = 0;

    -- Flag if any open exception shares the same DealRef
    -- or the same ClientName + ExceptionType combination.
    IF EXISTS (
        SELECT 1 FROM [dbo].[DealExceptions]
        WHERE [Status] NOT IN ('Closed', 'Approved', 'Rejected')
          AND (
              [DealRef] = @DealRef
              OR ([ClientName] = @ClientName AND [ExceptionType] = @ExceptionType)
          )
    )
    BEGIN
        SET @IsDuplicate = 1;

        -- Back-flag the existing open matches so they appear as duplicates too.
        UPDATE [dbo].[DealExceptions]
        SET    [IsPossibleDuplicate] = 1
        WHERE  [Status] NOT IN ('Closed', 'Approved', 'Rejected')
          AND  (
                   [DealRef] = @DealRef
                   OR ([ClientName] = @ClientName AND [ExceptionType] = @ExceptionType)
               );
    END

    INSERT INTO [dbo].[DealExceptions]
        ([DealRef], [ClientName], [ExceptionType], [Description], [Priority], [Status],
         [AssignedOwner], [CreatedAt], [UpdatedAt], [IsPossibleDuplicate])
    VALUES
        (@DealRef, @ClientName, @ExceptionType, @Description, @Priority, 'New',
         @AssignedOwner, @Now, @Now, @IsDuplicate);

    SET @Id = SCOPE_IDENTITY();

    INSERT INTO [dbo].[StatusHistories]
        ([ExceptionId], [FromStatus], [ToStatus], [ChangedBy], [ChangedAt], [Notes])
    VALUES
        (@Id, 'New', 'New', @CreatedBy, @Now, 'Created');

    SELECT @Id AS [Id];
END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_DealException_UpdateStatus]
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

    IF @OldStatus IS NULL RETURN;

    UPDATE [dbo].[DealExceptions]
    SET [Status] = @Status, [UpdatedAt] = @Now
    WHERE [Id] = @Id;

    INSERT INTO [dbo].[StatusHistories]
        ([ExceptionId], [FromStatus], [ToStatus], [ChangedBy], [ChangedAt], [Notes])
    VALUES
        (@Id, @OldStatus, @Status, @ChangedBy, @Now, @Notes);
END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_Comment_Create]
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

CREATE OR ALTER PROCEDURE [dbo].[usp_Report_OpenByOwner]
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

CREATE OR ALTER PROCEDURE [dbo].[usp_Report_CriticalOverdue]
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

CREATE OR ALTER PROCEDURE [dbo].[usp_Report_ByStatusPriority]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT [Status], [Priority], COUNT(*) AS [Count]
    FROM [dbo].[DealExceptions]
    GROUP BY [Status], [Priority]
    ORDER BY [Status], [Priority];
END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_Report_AvgTimeToClose]
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
