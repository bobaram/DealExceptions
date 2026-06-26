using Microsoft.Data.SqlClient;

namespace DealExceptions.Infrastructure;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}

public sealed class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public SqlConnection CreateConnection() => new(connectionString);
}
