using DealExceptions.Application.Interfaces;
using DealExceptions.Domain.Entities;
using DealExceptions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DealExceptions.Infrastructure.Repositories;

public class CommentRepository(AppDbContext db) : ICommentRepository
{
    public Task<bool> ExceptionExistsAsync(int id)
        => db.DealExceptions.AnyAsync(e => e.Id == id);

    public async Task<Comment> AddAsync(Comment comment)
    {
        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        return comment;
    }

    public Task TouchExceptionUpdatedAtAsync(int exceptionId, DateTime at)
        => db.DealExceptions
            .Where(e => e.Id == exceptionId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.UpdatedAt, at));
}
