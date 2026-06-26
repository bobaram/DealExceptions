using DealExceptions.Application.DTOs;
using DealExceptions.Application.Interfaces;
using Xunit;
using DealExceptions.Application.Services;
using DealExceptions.Domain.Entities;
using Moq;

namespace DealExceptions.Tests.Unit;

public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> _repo = new();
    private readonly CommentService _svc;

    public CommentServiceTests() => _svc = new CommentService(_repo.Object);

    [Fact]
    public async Task AddCommentAsync_ReturnsNull_WhenExceptionDoesNotExist()
    {
        _repo.Setup(r => r.ExceptionExistsAsync(99)).ReturnsAsync(false);

        var result = await _svc.AddCommentAsync(99, new AddCommentRequest("Alice", "Hello"));

        Assert.Null(result);
    }

    [Fact]
    public async Task AddCommentAsync_NeverCallsAddAsync_WhenExceptionDoesNotExist()
    {
        _repo.Setup(r => r.ExceptionExistsAsync(99)).ReturnsAsync(false);

        await _svc.AddCommentAsync(99, new AddCommentRequest("Alice", "Hello"));

        _repo.Verify(r => r.AddAsync(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public async Task AddCommentAsync_TrimsStrings_BeforePersisting()
    {
        Comment? captured = null;
        SetupHappyPath(captured: c => captured = c);

        await _svc.AddCommentAsync(1, new AddCommentRequest("  Alice  ", "  Hello world  "));

        Assert.Equal("Alice",       captured!.AuthorName);
        Assert.Equal("Hello world", captured.Text);
    }

    [Fact]
    public async Task AddCommentAsync_ReturnsCommentDto_WithPersistedId()
    {
        var saved = new Comment { Id = 42, AuthorName = "Alice", Text = "Hello", CreatedAt = DateTime.UtcNow };
        _repo.Setup(r => r.ExceptionExistsAsync(1)).ReturnsAsync(true);
        _repo.Setup(r => r.AddAsync(It.IsAny<Comment>())).ReturnsAsync(saved);
        _repo.Setup(r => r.TouchExceptionUpdatedAtAsync(It.IsAny<int>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);

        var result = await _svc.AddCommentAsync(1, new AddCommentRequest("Alice", "Hello"));

        Assert.NotNull(result);
        Assert.Equal(42,      result.Id);
        Assert.Equal("Alice", result.AuthorName);
        Assert.Equal("Hello", result.Text);
    }

    [Fact]
    public async Task AddCommentAsync_CallsTouchUpdatedAt_WithSavedTimestamp()
    {
        var savedAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var saved = new Comment { Id = 1, AuthorName = "Alice", Text = "Hi", CreatedAt = savedAt };
        _repo.Setup(r => r.ExceptionExistsAsync(1)).ReturnsAsync(true);
        _repo.Setup(r => r.AddAsync(It.IsAny<Comment>())).ReturnsAsync(saved);
        _repo.Setup(r => r.TouchExceptionUpdatedAtAsync(1, savedAt)).Returns(Task.CompletedTask);

        await _svc.AddCommentAsync(1, new AddCommentRequest("Alice", "Hi"));

        _repo.Verify(r => r.TouchExceptionUpdatedAtAsync(1, savedAt), Times.Once);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void SetupHappyPath(Action<Comment>? captured = null)
    {
        _repo.Setup(r => r.ExceptionExistsAsync(1)).ReturnsAsync(true);
        _repo.Setup(r => r.AddAsync(It.IsAny<Comment>()))
             .Callback<Comment>(c => captured?.Invoke(c))
             .ReturnsAsync((Comment c) => c);
        _repo.Setup(r => r.TouchExceptionUpdatedAtAsync(It.IsAny<int>(), It.IsAny<DateTime>()))
             .Returns(Task.CompletedTask);
    }
}
