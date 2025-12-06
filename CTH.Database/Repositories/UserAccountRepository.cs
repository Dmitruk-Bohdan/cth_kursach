using CTH.Database.Abstractions;
using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace CTH.Database.Repositories;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ILogger<UserAccountRepository> _logger;
    private readonly string _getByEmailQuery;
    private readonly string _insertQuery;
    private readonly string _updateLastLoginQuery;

    public UserAccountRepository(
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        ILogger<UserAccountRepository> logger)
    {
        _sqlExecutor = sqlExecutor;
        _logger = logger;
        _getByEmailQuery = sqlQueryProvider.GetQuery("UserAccountUseCases/Queries/GetByEmail");
        _insertQuery = sqlQueryProvider.GetQuery("UserAccountUseCases/Commands/InsertUser");
        _updateLastLoginQuery = sqlQueryProvider.GetQuery("UserAccountUseCases/Commands/UpdateLastLogin");
    }

    public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        var parameters = new[]
        {
            new NpgsqlParameter("email", NpgsqlDbType.Varchar) { Value = email }
        };

        return _sqlExecutor.QuerySingleAsync(
            _getByEmailQuery,
            MapUserAccount,
            parameters,
            cancellationToken);
    }

    public async Task<long> InsertAsync(UserAccount user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        var parameters = new List<NpgsqlParameter>
        {
            new("user_name", NpgsqlDbType.Varchar) { Value = user.UserName },
            new("email", NpgsqlDbType.Varchar) { Value = user.Email },
            new("password_hash", NpgsqlDbType.Varchar) { Value = user.PasswordHash },
            new("role_type_id", NpgsqlDbType.Integer) { Value = user.RoleTypeId },
            new("last_login_at", NpgsqlDbType.TimestampTz) { Value = (object?)user.LastLoginAt ?? DBNull.Value },
            new("created_at", NpgsqlDbType.TimestampTz) { Value = (object?)user.CreatedAt ?? DBNull.Value },
            new("updated_at", NpgsqlDbType.TimestampTz) { Value = (object?)user.UpdatedAt ?? DBNull.Value }
        };

        var insertedId = await _sqlExecutor.QuerySingleAsync(
            _insertQuery,
            reader => reader.GetInt64(0),
            parameters,
            cancellationToken);

        if (insertedId == 0)
        {
            _logger.LogError("Failed to insert user with email {Email}", user.Email);
            throw new InvalidOperationException("Failed to insert user.");
        }

        return insertedId;
    }

    public async Task UpdateLastLoginAsync(long userId, DateTimeOffset lastLoginAt, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("id", NpgsqlDbType.Bigint) { Value = userId },
            new NpgsqlParameter("last_login_at", NpgsqlDbType.TimestampTz) { Value = lastLoginAt },
            new NpgsqlParameter("updated_at", NpgsqlDbType.TimestampTz) { Value = lastLoginAt }
        };

        var affectedRows = await _sqlExecutor.ExecuteAsync(
            _updateLastLoginQuery,
            parameters,
            cancellationToken);

        if (affectedRows == 0)
        {
            _logger.LogWarning("Tried to update last login timestamp for missing user {UserId}", userId);
        }
    }

    private static UserAccount MapUserAccount(NpgsqlDataReader reader)
    {
        return new UserAccount
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            UserName = reader.GetString(reader.GetOrdinal("user_name")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            RoleTypeId = reader.GetInt32(reader.GetOrdinal("role_type_id")),
            LastLoginAt = reader.IsDBNull(reader.GetOrdinal("last_login_at"))
                ? null
                : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_login_at")),
            CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at"))
                ? null
                : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
                ? null
                : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")),
        };
    }
}
