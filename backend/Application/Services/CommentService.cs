using DealExceptions.Application.DTOs;
using DealExceptions.Domain.Entities;
using DealExceptions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DealExceptions.Application.Services;

public class CommentService
{
    private readonly AppDbContext _db;

    public CommentService(AppDbContext db) => _db = db;

    public async Task<CommentDto?> AddCommentAsync(int exceptionId, AddCommentRequest req)
    {
        var exists = await _db.DealExceptions.AnyAsync(e => e.Id == exceptionId);
        if (!exists) return null;

        var comment = new Comment
        {
            ExceptionId = exceptionId,
            AuthorName = req.AuthorName.Trim(),
            Text = req.Text.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);

        await _db.DealExceptions
            .Where(e => e.Id == exceptionId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.UpdatedAt, DateTime.UtcNow));

        await _db.SaveChangesAsync();
        return new CommentDto(comment.Id, comment.AuthorName, comment.Text, comment.CreatedAt);
    }
}
