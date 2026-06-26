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
    CONSTRAINT [PK_DealExceptions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [CHK_DealExceptions_Priority] CHECK ([Priority] IN ('Low', 'Medium', 'High', 'Critical')),
    CONSTRAINT [CHK_DealExceptions_Status]   CHECK ([Status]   IN ('New', 'Pending', 'InReview', 'Approved', 'Rejected', 'Closed'))
);
GO

CREATE INDEX [IX_DealExceptions_Status]   ON [dbo].[DealExceptions] ([Status]);
GO
CREATE INDEX [IX_DealExceptions_Priority] ON [dbo].[DealExceptions] ([Priority]);
GO
CREATE INDEX [IX_DealExceptions_Owner]    ON [dbo].[DealExceptions] ([AssignedOwner]);
GO
