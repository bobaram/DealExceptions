using DealExceptions.Application.DTOs;
using DealExceptions.Application.Interfaces;
using DealExceptions.Domain.Entities;

namespace DealExceptions.Application.Services;

public class CommentService(ICommentRepository repo)
{
    public async Task<CommentDto?> AddCommentAsync(int exceptionId, AddCommentRequest req)
    {
        if (!await repo.ExceptionExistsAsync(exceptionId)) return null;

        var comment = new Comment
        {
            ExceptionId = exceptionId,
            AuthorName = req.AuthorName.Trim(),
            Text = req.Text.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var saved = await repo.AddAsync(comment);
        await repo.TouchExceptionUpdatedAtAsync(exceptionId, saved.CreatedAt);
        return new CommentDto(saved.Id, saved.AuthorName, saved.Text, saved.CreatedAt);
    }
}
