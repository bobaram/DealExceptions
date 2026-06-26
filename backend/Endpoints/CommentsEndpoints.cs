using DealExceptions.Application.DTOs;
using DealExceptions.Application.Services;

namespace DealExceptions.Endpoints;

public static class CommentsEndpoints
{
    public static void MapCommentsEndpoints(this WebApplication app)
    {
        app.MapPost("/api/exceptions/{id:int}/comments", async (int id, AddCommentRequest req, CommentService svc) =>
        {
            if (string.IsNullOrWhiteSpace(req.AuthorName)) return Results.BadRequest("AuthorName is required.");
            if (string.IsNullOrWhiteSpace(req.Text)) return Results.BadRequest("Text is required.");

            var result = await svc.AddCommentAsync(id, req);
            return result is null ? Results.NotFound() : Results.Created($"/api/exceptions/{id}/comments/{result.Id}", result);
        })
        .WithTags("Comments")
        .WithName("AddComment");
    }
}
