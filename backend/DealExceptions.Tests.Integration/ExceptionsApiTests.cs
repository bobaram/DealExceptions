using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DealExceptions.Tests.Integration;

[Collection("Api")]
public class ExceptionsApiTests : IAsyncLifetime
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ExceptionsApiTests(ApiFixture fixture) =>
        _http = fixture.CreateClient();

    public Task InitializeAsync() => ApiFixture.ResetTablesAsync();
    public Task DisposeAsync()    => Task.CompletedTask;

    // ── GET /api/exceptions ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200WithEmptyPage_WhenNoneExist()
    {
        var response = await _http.GetAsync("/api/exceptions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(0, body.GetProperty("totalCount").GetInt32());
        Assert.Empty(body.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task GetAll_ReturnsCreatedExceptions()
    {
        await CreateExceptionAsync();
        await CreateExceptionAsync(dealRef: "DEAL-002");

        var body = await _http.GetFromJsonAsync<JsonElement>("/api/exceptions", JsonOpts);

        Assert.Equal(2, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task GetAll_FiltersByStatus()
    {
        await CreateExceptionAsync(dealRef: "DEAL-A");
        var id = await CreateExceptionIdAsync(dealRef: "DEAL-B");
        await PatchStatusAsync(id, "Approved");

        var body = await _http.GetFromJsonAsync<JsonElement>("/api/exceptions?openOnly=true", JsonOpts);
        var items = body.GetProperty("items").EnumerateArray().ToList();

        Assert.Single(items);
        Assert.Equal("DEAL-A", items[0].GetProperty("dealRef").GetString());
    }

    [Fact]
    public async Task GetAll_PaginatesCorrectly()
    {
        for (var i = 1; i <= 5; i++)
            await CreateExceptionAsync(dealRef: $"DEAL-{i:D3}");

        var page1 = await _http.GetFromJsonAsync<JsonElement>("/api/exceptions?page=1&pageSize=3", JsonOpts);
        var page2 = await _http.GetFromJsonAsync<JsonElement>("/api/exceptions?page=2&pageSize=3", JsonOpts);

        Assert.Equal(5, page1.GetProperty("totalCount").GetInt32());
        Assert.Equal(3, page1.GetProperty("items").GetArrayLength());
        Assert.Equal(2, page2.GetProperty("items").GetArrayLength());
        Assert.Equal(1, page1.GetProperty("page").GetInt32());
        Assert.Equal(2, page2.GetProperty("page").GetInt32());
    }

    // ── POST /api/exceptions ───────────────────────────────────────────────────

    [Fact]
    public async Task Create_Returns201_WithLocationHeader()
    {
        var response = await CreateExceptionAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Create_ReturnsException_WithCorrectFields()
    {
        var response = await CreateExceptionAsync(
            dealRef: "DEAL-TEST",
            client:  "Acme Corp",
            type:    "Pricing",
            priority: "High");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        Assert.Equal("DEAL-TEST", body.GetProperty("dealRef").GetString());
        Assert.Equal("Acme Corp", body.GetProperty("clientName").GetString());
        Assert.Equal("Pricing",   body.GetProperty("exceptionType").GetString());
        Assert.Equal("High",      body.GetProperty("priority").GetString());
        Assert.Equal("New",       body.GetProperty("status").GetString());
        Assert.True(body.GetProperty("isOpen").GetBoolean());
        Assert.False(body.GetProperty("isPossibleDuplicate").GetBoolean());
    }

    [Theory]
    [InlineData(null, "ACME", "Pricing", "desc", "Low", "user")]
    [InlineData("DEAL-1", null, "Pricing", "desc", "Low", "user")]
    [InlineData("DEAL-1", "ACME", null, "desc", "Low", "user")]
    [InlineData("DEAL-1", "ACME", "Pricing", null, "Low", "user")]
    [InlineData("DEAL-1", "ACME", "Pricing", "desc", "Low", null)]
    public async Task Create_Returns400_WhenRequiredFieldMissing(
        string? dealRef, string? client, string? type, string? desc, string? priority, string? createdBy)
    {
        var response = await PostAsync("/api/exceptions", new
        {
            dealRef, clientName = client, exceptionType = type,
            description = desc, priority, createdBy
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns400_WhenPriorityIsInvalid()
    {
        var response = await PostAsync("/api/exceptions", new
        {
            dealRef = "DEAL-1", clientName = "ACME", exceptionType = "Pricing",
            description = "test", priority = "INVALID", createdBy = "user"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── GET /api/exceptions/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        var response = await _http.GetAsync("/api/exceptions/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsException_WithEmptyCollections()
    {
        var id = await CreateExceptionIdAsync();

        var body = await _http.GetFromJsonAsync<JsonElement>($"/api/exceptions/{id}", JsonOpts);

        Assert.Equal(id, body.GetProperty("id").GetInt32());
        Assert.Empty(body.GetProperty("comments").EnumerateArray());
        // Initial "Created" status history entry is created on insert
        Assert.Single(body.GetProperty("statusHistories").EnumerateArray());
    }

    // ── PATCH /api/exceptions/{id}/status ─────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_Returns404_WhenNotFound()
    {
        var response = await PatchStatusAsync(9999, "Closed");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_Returns400_WhenStatusInvalid()
    {
        var id = await CreateExceptionIdAsync();
        var response = await PatchAsync($"/api/exceptions/{id}/status", new
        {
            status = "BOGUS", changedBy = "user"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_ChangesStatusAndCreatesHistory()
    {
        var id = await CreateExceptionIdAsync();

        var response = await PatchStatusAsync(id, "Pending", notes: "Under review");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Pending", body.GetProperty("status").GetString());

        var histories = body.GetProperty("statusHistories").EnumerateArray().ToList();
        Assert.Equal(2, histories.Count); // Created + Pending
        Assert.Equal("Pending", histories.Last().GetProperty("toStatus").GetString());
        Assert.Equal("Under review", histories.Last().GetProperty("notes").GetString());
    }

    [Fact]
    public async Task UpdateStatus_SetsIsOpen_FalseWhenClosed()
    {
        var id = await CreateExceptionIdAsync();
        var response = await PatchStatusAsync(id, "Closed");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        Assert.False(body.GetProperty("isOpen").GetBoolean());
    }

    // ── Duplicate detection ────────────────────────────────────────────────────

    [Fact]
    public async Task Create_FlagsIsPossibleDuplicate_WhenSameDealRefExists()
    {
        await CreateExceptionAsync(dealRef: "DEAL-DUP");
        var response2 = await CreateExceptionAsync(dealRef: "DEAL-DUP");

        var body = await response2.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        Assert.True(body.GetProperty("isPossibleDuplicate").GetBoolean());
    }

    [Fact]
    public async Task Create_FlagsIsPossibleDuplicate_WhenSameClientAndType()
    {
        await CreateExceptionAsync(client: "BigCorp", type: "Pricing");
        var response2 = await CreateExceptionAsync(dealRef: "DEAL-X", client: "BigCorp", type: "Pricing");

        var body = await response2.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        Assert.True(body.GetProperty("isPossibleDuplicate").GetBoolean());
    }

    [Fact]
    public async Task Create_DoesNotFlagDuplicate_WhenClientAndTypeDiffer()
    {
        await CreateExceptionAsync(client: "BigCorp", type: "Pricing");
        var response2 = await CreateExceptionAsync(dealRef: "DEAL-Y", client: "BigCorp", type: "Compliance");

        var body = await response2.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        Assert.False(body.GetProperty("isPossibleDuplicate").GetBoolean());
    }

    [Fact]
    public async Task Create_BackFlagsExistingException_WhenDuplicateCreated()
    {
        var firstId = await CreateExceptionIdAsync(dealRef: "DEAL-DUP2");
        await CreateExceptionAsync(dealRef: "DEAL-DUP2");

        var first = await _http.GetFromJsonAsync<JsonElement>($"/api/exceptions/{firstId}", JsonOpts);

        Assert.True(first.GetProperty("isPossibleDuplicate").GetBoolean());
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private Task<HttpResponseMessage> CreateExceptionAsync(
        string dealRef   = "DEAL-001",
        string client    = "ACME Corp",
        string type      = "Pricing",
        string priority  = "Medium",
        string? owner    = null,
        string createdBy = "test-user") =>
        PostAsync("/api/exceptions", new
        {
            dealRef,
            clientName    = client,
            exceptionType = type,
            description   = "Integration test exception",
            priority,
            assignedOwner = owner,
            createdBy
        });

    private async Task<int> CreateExceptionIdAsync(
        string dealRef = "DEAL-001", string client = "ACME Corp", string type = "Pricing")
    {
        var response = await CreateExceptionAsync(dealRef: dealRef, client: client, type: type);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("id").GetInt32();
    }

    private async Task<HttpResponseMessage> PatchStatusAsync(int id, string status, string? notes = null) =>
        await PatchAsync($"/api/exceptions/{id}/status", new { status, changedBy = "test-user", notes });

    private Task<HttpResponseMessage> PostAsync(string url, object body) =>
        _http.PostAsJsonAsync(url, body);

    private Task<HttpResponseMessage> PatchAsync(string url, object body) =>
        _http.PatchAsJsonAsync(url, body);
}
