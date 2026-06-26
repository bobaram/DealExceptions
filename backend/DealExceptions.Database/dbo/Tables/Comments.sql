CREATE TABLE [dbo].[Comments] (
    [Id]          INT           NOT NULL IDENTITY(1,1),
    [ExceptionId] INT           NOT NULL,
    [AuthorName]  NVARCHAR(200) NOT NULL,
    [Text]        NVARCHAR(MAX) NOT NULL,
    [CreatedAt]   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    [LegacyId]    INT           NULL,
    CONSTRAINT [PK_Comments]               PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Comments_DealExceptions] FOREIGN KEY ([ExceptionId]) REFERENCES [dbo].[DealExceptions] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Comments_ExceptionId] ON [dbo].[Comments] ([ExceptionId]);
GO
