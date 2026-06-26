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
GO

CREATE INDEX [IX_StatusHistories_ExceptionId] ON [dbo].[StatusHistories] ([ExceptionId]);
GO
