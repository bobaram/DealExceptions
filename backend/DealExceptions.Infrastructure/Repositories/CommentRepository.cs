using Dapper;
using DealExceptions.Application.Interfaces;
using DealExceptions.Domain.Entities;
using System.Data;

namespace DealExceptions.Infrastructure.Repositories;

public class CommentRepository(IDbConnectionFactory connFactory) : ICommentRepository
{
    public async Task<bool> ExceptionExistsAsync(int id)
    {
        await using var conn = connFactory.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [dbo].[DealExceptions] WHERE [Id] = @Id",
            new { Id = id });
        return count > 0;
    }

    public async Task<Comment> AddAsync(Comment comment)
    {
        await using var conn = connFactory.CreateConnection();
        var newId = await conn.ExecuteScalarAsync<int>(
            "usp_Comment_Create",
            new { comment.ExceptionId, comment.AuthorName, Text = comment.Text },
            commandType: CommandType.StoredProcedure);

        comment.Id = newId;
        return comment;
    }

    // usp_Comment_Create already touches DealExceptions.UpdatedAt — no-op here
    public Task TouchExceptionUpdatedAtAsync(int exceptionId, DateTime at) => Task.CompletedTask;
}
