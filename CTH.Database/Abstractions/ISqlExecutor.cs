using Npgsql;

namespace CTH.Database.Abstractions;

public interface ISqlExecutor
{
    Task<int> ExecuteAsync(
        string query,
        IReadOnlyCollection<NpgsqlParameter>? parameters = null,
        CancellationToken cancellationToken = default);

    Task<TResult?> QuerySingleAsync<TResult>(
        string query,
        Func<NpgsqlDataReader, TResult> map,
        IReadOnlyCollection<NpgsqlParameter>? parameters = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TResult>> QueryAsync<TResult>(
        string query,
        Func<NpgsqlDataReader, TResult> map,
        IReadOnlyCollection<NpgsqlParameter>? parameters = null,
        CancellationToken cancellationToken = default);
}
