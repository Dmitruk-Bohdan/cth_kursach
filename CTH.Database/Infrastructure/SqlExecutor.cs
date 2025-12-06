using CTH.Database.Abstractions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CTH.Database.Infrastructure;

public class SqlExecutor : ISqlExecutor
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlExecutor> _logger;

    public SqlExecutor(ISqlConnectionFactory connectionFactory, ILogger<SqlExecutor> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(
        string query,
        IReadOnlyCollection<NpgsqlParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, query, parameters);

        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to execute SQL command.");
            throw;
        }
    }

    public async Task<TResult?> QuerySingleAsync<TResult>(
        string query,
        Func<NpgsqlDataReader, TResult> map,
        IReadOnlyCollection<NpgsqlParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, query, parameters);

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return default;
            }

            return map(reader);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to execute SQL single-row query.");
            throw;
        }
    }

    public async Task<IReadOnlyCollection<TResult>> QueryAsync<TResult>(
        string query,
        Func<NpgsqlDataReader, TResult> map,
        IReadOnlyCollection<NpgsqlParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, query, parameters);

        var results = new List<TResult>();
        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(map(reader));
            }

            return results;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to execute SQL multi-row query.");
            throw;
        }
    }

    private static NpgsqlCommand CreateCommand(
        NpgsqlConnection connection,
        string query,
        IReadOnlyCollection<NpgsqlParameter>? parameters)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("SQL query cannot be empty.", nameof(query));
        }

        var command = connection.CreateCommand();
        command.CommandText = query;

        if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }
        }

        return command;
    }
}
