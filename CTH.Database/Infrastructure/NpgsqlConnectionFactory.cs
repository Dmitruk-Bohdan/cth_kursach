using CTH.Common.Constants;
using CTH.Database.Abstractions;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CTH.Database.Infrastructure;

public class NpgsqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString(ConnectionStringNames.CthDb)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringNames.CthDb}' is missing in configuration.");
    }

    public async Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
