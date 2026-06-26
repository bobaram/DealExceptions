using DealExceptions.Application.DTOs;
using DealExceptions.Application.Interfaces;
using Xunit;
using DealExceptions.Application.Services;
using DealExceptions.Domain;
using DealExceptions.Domain.Entities;
using Moq;

namespace DealExceptions.Tests.Unit;

public class ExceptionServiceTests
{
    private readonly Mock<IExceptionRepository> _repo = new();
    private readonly ExceptionService _svc;

    public ExceptionServiceTests() => _svc = new ExceptionService(_repo.Object);

    // ── GetAllAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenRepositoryHasNoItems()
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<ExceptionFilters>())).ReturnsAsync([]);

        var result = await _svc.GetAllAsync(null, null, null, false);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_PassesFiltersToRepository()
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<ExceptionFilters>())).ReturnsAsync([]);

        await _svc.GetAllAsync("New", "High", "DEAL-1", true);

        _repo.Verify(r => r.ListAsync(It.Is<ExceptionFilters>(f =>
            f.Status == "New" && f.Priority == "High" && f.Search == "DEAL-1" && f.OpenOnly)), Times.Once);
    }

    [Theory]
    [InlineData(ExceptionStatus.New,      true)]
    [InlineData(ExceptionStatus.Pending,  true)]
    [InlineData(ExceptionStatus.InReview, true)]
    [InlineData(ExceptionStatus.Approved, false)]
    [InlineData(ExceptionStatus.Rejected, false)]
    [InlineData(ExceptionStatus.Closed,   false)]
    public async Task GetAllAsync_SetsIsOpen_CorrectlyByStatus(ExceptionStatus status, bool expectedIsOpen)
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<ExceptionFilters>()))
             .ReturnsAsync([MakeException(status: status)]);

        var result = (await _svc.GetAllAsync(null, null, null, false)).Single();

        Assert.Equal(expectedIsOpen, result.IsOpen);
    }

    [Theory]
    [InlineData(Priority.Critical, true)]
    [InlineData(Priority.High,     false)]
    [InlineData(Priority.Medium,   false)]
    [InlineData(Priority.Low,      false)]
    public async Task GetAllAsync_SetsIsCritical_OnlyForCriticalPriority(Priority priority, bool expected)
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<ExceptionFilters>()))
             .ReturnsAsync([MakeException(priority: priority)]);

        var result = (await _svc.GetAllAsync(null, null, null, false)).Single();

        Assert.Equal(expected, result.IsCritical);
    }

    // ── GetByIdAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repo.Setup(r => r.FindWithDetailsAsync(99)).ReturnsAsync((DealException?)null);

        Assert.Null(await _svc.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedDetail_WhenFound()
    {
        var entity = MakeException(id: 7, dealRef: "DEAL-007");
        _repo.Setup(r => r.FindWithDetailsAsync(7)).ReturnsAsync(entity);

        var result = await _svc.GetByIdAsync(7);

        Assert.NotNull(result);
        Assert.Equal(7, result.Id);
        Assert.Equal("DEAL-007", result.DealRef);
    }

    [Fact]
    public async Task GetByIdAsync_MapsCommentsAndHistories()
    {
        var entity = MakeException(id: 1);
        entity.Comments.Add(new Comment { Id = 10, AuthorName = "Alice", Text = "Hi", CreatedAt = DateTime.UtcNow });
        entity.StatusHistories.Add(new StatusHistory { Id = 20, FromStatus = ExceptionStatus.New, ToStatus = ExceptionStatus.Pending, ChangedBy = "Bob" });
        _repo.Setup(r => r.FindWithDetailsAsync(1)).ReturnsAsync(entity);

        var result = await _svc.GetByIdAsync(1);

        Assert.Single(result!.Comments);
        Assert.Equal("Alice", result.Comments.First().AuthorName);
        Assert.Single(result.StatusHistories);
        Assert.Equal("Pending", result.StatusHistories.First().ToStatus);
    }

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ThrowsArgumentException_WhenPriorityInvalid()
    {
        var req = Req(priority: "BOGUS");

        await Assert.ThrowsAsync<ArgumentException>(() => _svc.CreateAsync(req));
    }

    [Theory]
    [InlineData("low",      "Low")]
    [InlineData("HIGH",     "High")]
    [InlineData("Critical", "Critical")]
    [InlineData("medium",   "Medium")]
    public async Task CreateAsync_ParsesPriorityCaseInsensitively(string input, string expected)
    {
        DealException? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<DealException>()))
             .Callback<DealException>(e => captured = e)
             .ReturnsAsync((DealException e) => e);

        await _svc.CreateAsync(Req(priority: input));

        Assert.Equal(expected, captured!.Priority.ToString());
    }

    [Fact]
    public async Task CreateAsync_TrimsWhitespace_OnAllStringFields()
    {
        DealException? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<DealException>()))
             .Callback<DealException>(e => captured = e)
             .ReturnsAsync((DealException e) => e);

        var req = new CreateExceptionRequest("  DEAL-1  ", "  ACME  ", "  Pricing  ", "  desc  ", "Low", "  owner  ", "user");
        await _svc.CreateAsync(req);

        Assert.Equal("DEAL-1",  captured!.DealRef);
        Assert.Equal("ACME",    captured.ClientName);
        Assert.Equal("Pricing", captured.ExceptionType);
        Assert.Equal("desc",    captured.Description);
        Assert.Equal("owner",   captured.AssignedOwner);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAsync_SetsAssignedOwnerNull_WhenOwnerBlank(string? owner)
    {
        DealException? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<DealException>()))
             .Callback<DealException>(e => captured = e)
             .ReturnsAsync((DealException e) => e);

        await _svc.CreateAsync(Req(assignedOwner: owner));

        Assert.Null(captured!.AssignedOwner);
    }

    [Fact]
    public async Task CreateAsync_SetsStatusToNew()
    {
        DealException? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<DealException>()))
             .Callback<DealException>(e => captured = e)
             .ReturnsAsync((DealException e) => e);

        await _svc.CreateAsync(Req());

        Assert.Equal(ExceptionStatus.New, captured!.Status);
    }

    // ── UpdateStatusAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ThrowsArgumentException_WhenStatusInvalid()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _svc.UpdateStatusAsync(1, new UpdateStatusRequest("BOGUS", "user", null)));
    }

    [Fact]
    public async Task UpdateStatusAsync_ReturnsNull_WhenExceptionNotFound()
    {
        _repo.Setup(r => r.UpdateStatusAsync(99, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
             .Returns(Task.CompletedTask);
        _repo.Setup(r => r.FindWithDetailsAsync(99)).ReturnsAsync((DealException?)null);

        var result = await _svc.UpdateStatusAsync(99, new UpdateStatusRequest("Closed", "user", null));

        Assert.Null(result);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("APPROVED")]
    [InlineData("inreview")]
    public async Task UpdateStatusAsync_ParsesStatusCaseInsensitively(string status)
    {
        _repo.Setup(r => r.UpdateStatusAsync(1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
             .Returns(Task.CompletedTask);
        _repo.Setup(r => r.FindWithDetailsAsync(1)).ReturnsAsync(MakeException(id: 1));

        var result = await _svc.UpdateStatusAsync(1, new UpdateStatusRequest(status, "user", null));

        Assert.NotNull(result);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static DealException MakeException(
        int id = 1,
        string dealRef = "DEAL-001",
        Priority priority = Priority.Medium,
        ExceptionStatus status = ExceptionStatus.New) => new()
    {
        Id = id,
        DealRef = dealRef,
        ClientName = "Test Client",
        ExceptionType = "Pricing",
        Description = "Test description",
        Priority = priority,
        Status = status,
    };

    private static CreateExceptionRequest Req(
        string dealRef = "DEAL-1",
        string client = "ACME",
        string type = "Pricing",
        string desc = "desc",
        string priority = "Low",
        string? assignedOwner = null,
        string createdBy = "user") =>
        new(dealRef, client, type, desc, priority, assignedOwner, createdBy);
}
