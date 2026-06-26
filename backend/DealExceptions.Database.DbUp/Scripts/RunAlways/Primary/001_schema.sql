-- Idempotent schema: tables + indexes
-- RunAlways/Primary — NullJournal (runs every startup)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DealExceptions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[DealExceptions] (
        [Id]                  INT           NOT NULL IDENTITY(1,1),
        [DealRef]             NVARCHAR(50)  NOT NULL,
        [ClientName]          NVARCHAR(200) NOT NULL,
        [ExceptionType]       NVARCHAR(100) NOT NULL,
        [Description]         NVARCHAR(MAX) NOT NULL,
        [Priority]            NVARCHAR(20)  NOT NULL,
        [Status]              NVARCHAR(20)  NOT NULL,
        [AssignedOwner]       NVARCHAR(200) NULL,
        [CreatedAt]           DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]           DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        [LegacyId]            INT           NULL,
        [IsPossibleDuplicate] BIT           NOT NULL DEFAULT 0,
        CONSTRAINT [PK_DealExceptions] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Comments' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Comments] (
        [Id]          INT           NOT NULL IDENTITY(1,1),
        [ExceptionId] INT           NOT NULL,
        [AuthorName]  NVARCHAR(200) NOT NULL,
        [Text]        NVARCHAR(MAX) NOT NULL,
        [CreatedAt]   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        [LegacyId]    INT           NULL,
        CONSTRAINT [PK_Comments]                PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Comments_DealExceptions] FOREIGN KEY ([ExceptionId]) REFERENCES [dbo].[DealExceptions] ([Id]) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'StatusHistories' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[StatusHistories] (
        [Id]          INT           NOT NULL IDENTITY(1,1),
        [ExceptionId] INT           NOT NULL,
        [FromStatus]  NVARCHAR(20)  NOT NULL,
        [ToStatus]    NVARCHAR(20)  NOT NULL,
        [ChangedBy]   NVARCHAR(200) NOT NULL,
        [ChangedAt]   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        [Notes]       NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_StatusHistories]                PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_StatusHistories_DealExceptions] FOREIGN KEY ([ExceptionId]) REFERENCES [dbo].[DealExceptions] ([Id]) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DealExceptions_Status' AND object_id = OBJECT_ID('dbo.DealExceptions'))
    CREATE INDEX [IX_DealExceptions_Status]   ON [dbo].[DealExceptions] ([Status]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DealExceptions_Priority' AND object_id = OBJECT_ID('dbo.DealExceptions'))
    CREATE INDEX [IX_DealExceptions_Priority] ON [dbo].[DealExceptions] ([Priority]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DealExceptions_Owner' AND object_id = OBJECT_ID('dbo.DealExceptions'))
    CREATE INDEX [IX_DealExceptions_Owner]    ON [dbo].[DealExceptions] ([AssignedOwner]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Comments_ExceptionId' AND object_id = OBJECT_ID('dbo.Comments'))
    CREATE INDEX [IX_Comments_ExceptionId]    ON [dbo].[Comments] ([ExceptionId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StatusHistories_ExceptionId' AND object_id = OBJECT_ID('dbo.StatusHistories'))
    CREATE INDEX [IX_StatusHistories_ExceptionId] ON [dbo].[StatusHistories] ([ExceptionId]);
GO
