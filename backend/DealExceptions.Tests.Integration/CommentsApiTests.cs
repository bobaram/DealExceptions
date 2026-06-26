using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DealExceptions.Tests.Integration;

[Collection("Api")]
public class CommentsApiTests : IAsyncLifetime
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CommentsApiTests(ApiFixture fixture) =>
        _http = fixture.CreateClient();

    public Task InitializeAsync() => ApiFixture.ResetTablesAsync();
    public Task DisposeAsync()    => Task.CompletedTask;

    // ── POST /api/exceptions/{id}/comments ────────────────────────────────────

    [Fact]
    public async Task AddComment_Returns201_WithLocationHeader()
    {
        var id = await CreateExceptionIdAsync();

        var response = await PostCommentAsync(id, "Alice", "Looks good");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task AddComment_ReturnsCommentDto_WithCorrectFields()
    {
        var id = await CreateExceptionIdAsync();

        var response = await PostCommentAsync(id, "Alice", "Looks good");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        Assert.Equal("Alice",      body.GetProperty("authorName").GetString());
        Assert.Equal("Looks good", body.GetProperty("text").GetString());
        Assert.True(body.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task AddComment_Returns404_WhenExceptionDoesNotExist()
    {
        var response = await PostCommentAsync(9999, "Alice", "Hello");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddComment_Returns400_WhenAuthorNameMissing()
    {
        var id = await CreateExceptionIdAsync();

        var response = await _http.PostAsJsonAsync($"/api/exceptions/{id}/comments", new
        {
            authorName = (string?)null,
            text = "Hello"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddComment_Returns400_WhenTextMissing()
    {
        var id = await CreateExceptionIdAsync();

        var response = await _http.PostAsJsonAsync($"/api/exceptions/{id}/comments", new
        {
            authorName = "Alice",
            text = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddComment_AppearsInGetById_AfterAdding()
    {
        var id = await CreateExceptionIdAsync();
        await PostCommentAsync(id, "Alice", "First comment");
        await PostCommentAsync(id, "Bob",   "Second comment");

        var body = await _http.GetFromJsonAsync<JsonElement>($"/api/exceptions/{id}", JsonOpts);
        var comments = body.GetProperty("comments").EnumerateArray().ToList();

        Assert.Equal(2, comments.Count);
        Assert.Equal("Alice", comments[0].GetProperty("authorName").GetString());
        Assert.Equal("Bob",   comments[1].GetProperty("authorName").GetString());
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<int> CreateExceptionIdAsync()
    {
        var response = await _http.PostAsJsonAsync("/api/exceptions", new
        {
            dealRef       = "DEAL-001",
            clientName    = "ACME",
            exceptionType = "Pricing",
            description   = "Test",
            priority      = "Low",
            createdBy     = "test-user"
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("id").GetInt32();
    }

    private Task<HttpResponseMessage> PostCommentAsync(int exceptionId, string author, string text) =>
        _http.PostAsJsonAsync($"/api/exceptions/{exceptionId}/comments", new
        {
            authorName = author,
            text
        });
}
