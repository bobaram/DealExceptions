using DealExceptions.Domain.Entities;

namespace DealExceptions.Application.Interfaces;

public interface ICommentRepository
{
    Task<bool> ExceptionExistsAsync(int id);
    Task<Comment> AddAsync(Comment comment);
    Task TouchExceptionUpdatedAtAsync(int exceptionId, DateTime at);
}
