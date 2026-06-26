using DealExceptions.Application.Services;
using DealExceptions.Endpoints;
using DealExceptions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddDbContext<AppDbContext>(opts =>
        opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<ExceptionService>();
    builder.Services.AddScoped<CommentService>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Deal Exceptions API", Version = "v1" }));

    builder.Services.AddCors(opts => opts.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        await DbSeeder.SeedAsync(db);
    }

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
