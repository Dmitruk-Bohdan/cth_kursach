using Npgsql;

namespace CTH.Database.Abstractions;

public interface ISqlConnectionFactory
{
    Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
