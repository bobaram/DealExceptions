using Dapper;
using DealExceptions.Application.Interfaces;
using DealExceptions.Domain;
using DealExceptions.Infrastructure.Dapper;
using DealExceptions.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DealExceptions.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        SqlMapper.AddTypeHandler(new EnumTypeHandler<Priority>());
        SqlMapper.AddTypeHandler(new EnumTypeHandler<ExceptionStatus>());

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddScoped<IExceptionRepository, ExceptionRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();

        return services;
    }
}
