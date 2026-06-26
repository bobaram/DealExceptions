using DealExceptions.Application.DTOs;
using DealExceptions.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DealExceptions.Endpoints;

public static class ExceptionsEndpoints
{
    public static void MapExceptionsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/exceptions").WithTags("Exceptions");

        group.MapGet("/", async (
            ExceptionService svc,
            [FromQuery] string? status,
            [FromQuery] string? priority,
            [FromQuery] string? search,
            [FromQuery] bool openOnly = false) =>
            Results.Ok(await svc.GetAllAsync(status, priority, search, openOnly)))
            .WithName("GetExceptions");

        group.MapGet("/{id:int}", async (int id, ExceptionService svc) =>
        {
            var result = await svc.GetByIdAsync(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("GetException");

        group.MapPost("/", async (CreateExceptionRequest req, ExceptionService svc) =>
        {
            if (string.IsNullOrWhiteSpace(req.DealRef)) return Results.BadRequest("DealRef is required.");
            if (string.IsNullOrWhiteSpace(req.ClientName)) return Results.BadRequest("ClientName is required.");
            if (string.IsNullOrWhiteSpace(req.ExceptionType)) return Results.BadRequest("ExceptionType is required.");
            if (string.IsNullOrWhiteSpace(req.Description)) return Results.BadRequest("Description is required.");
            if (string.IsNullOrWhiteSpace(req.CreatedBy)) return Results.BadRequest("CreatedBy is required.");

            try
            {
                var result = await svc.CreateAsync(req);
                return Results.Created($"/api/exceptions/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).WithName("CreateException");

        group.MapPatch("/{id:int}/status", async (int id, UpdateStatusRequest req, ExceptionService svc) =>
        {
            if (string.IsNullOrWhiteSpace(req.Status)) return Results.BadRequest("Status is required.");
            if (string.IsNullOrWhiteSpace(req.ChangedBy)) return Results.BadRequest("ChangedBy is required.");

            try
            {
                var result = await svc.UpdateStatusAsync(id, req);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).WithName("UpdateStatus");
    }
}
