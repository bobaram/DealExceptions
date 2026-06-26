using DealExceptions.Application.Interfaces;
using DealExceptions.Application.Services;
using DealExceptions.Endpoints;
using DealExceptions.Infrastructure.Data;
using DealExceptions.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddDbContext<AppDbContext>(opts =>
        opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<IExceptionRepository, ExceptionRepository>();
    builder.Services.AddScoped<ICommentRepository, CommentRepository>();
    builder.Services.AddScoped<ExceptionService>();
    builder.Services.AddScoped<CommentService>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Deal Exceptions API", Version = "v1" }));

    builder.Services.AddCors(opts => opts.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors();

    app.MapExceptionsEndpoints();
    app.MapCommentsEndpoints();
    app.MapReportsEndpoints();

    app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
