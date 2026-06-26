CREATE PROCEDURE [dbo].[usp_DealException_GetById]
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
