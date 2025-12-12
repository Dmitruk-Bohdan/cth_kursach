using CTH.Database.Abstractions;
using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace CTH.Database.Repositories;

public class InvitationCodeRepository : IInvitationCodeRepository
{
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ILogger<InvitationCodeRepository> _logger;
    private readonly string _createInvitationCodeQuery;
    private readonly string _getInvitationCodesByTeacherQuery;
    private readonly string _getInvitationCodeByCodeQuery;
    private readonly string _updateInvitationCodeQuery;
    private readonly string _deleteInvitationCodeQuery;

    public InvitationCodeRepository(
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        ILogger<InvitationCodeRepository> logger)
    {
        _sqlExecutor = sqlExecutor;
        _logger = logger;
        _createInvitationCodeQuery = sqlQueryProvider.GetQuery("InvitationCodeUseCases/Commands/CreateInvitationCode");
        _getInvitationCodesByTeacherQuery = sqlQueryProvider.GetQuery("InvitationCodeUseCases/Queries/GetInvitationCodesByTeacher");
        _getInvitationCodeByCodeQuery = sqlQueryProvider.GetQuery("InvitationCodeUseCases/Queries/GetInvitationCodeByCode");
        _updateInvitationCodeQuery = sqlQueryProvider.GetQuery("InvitationCodeUseCases/Commands/UpdateInvitationCode");
        _deleteInvitationCodeQuery = sqlQueryProvider.GetQuery("InvitationCodeUseCases/Commands/DeleteInvitationCode");
    }

    public async Task<long> CreateAsync(InvitationCode invitationCode, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("teacher_id", NpgsqlDbType.Bigint) { Value = invitationCode.TeacherId },
            new NpgsqlParameter("code", NpgsqlDbType.Varchar) { Value = invitationCode.Code },
            new NpgsqlParameter("max_uses", NpgsqlDbType.Integer) { Value = (object?)invitationCode.MaxUses ?? DBNull.Value },
            new NpgsqlParameter("expires_at", NpgsqlDbType.TimestampTz) { Value = (object?)invitationCode.ExpiresAt ?? DBNull.Value },
            new NpgsqlParameter("status", NpgsqlDbType.Varchar) { Value = invitationCode.Status }
        };

        var id = await _sqlExecutor.QuerySingleAsync(
            _createInvitationCodeQuery,
            reader => reader.GetInt64(0),
            parameters,
            cancellationToken);

        if (id == 0)
        {
            throw new InvalidOperationException("Failed to create invitation code");
        }

        return id;
    }

    public async Task<IReadOnlyCollection<InvitationCode>> GetByTeacherIdAsync(long teacherId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("teacher_id", NpgsqlDbType.Bigint) { Value = teacherId }
        };

        var result = await _sqlExecutor.QueryAsync(
            _getInvitationCodesByTeacherQuery,
            reader => new InvitationCode
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                TeacherId = reader.GetInt64(reader.GetOrdinal("teacher_id")),
                Code = reader.GetString(reader.GetOrdinal("code")),
                MaxUses = reader.IsDBNull(reader.GetOrdinal("max_uses")) ? null : reader.GetInt32(reader.GetOrdinal("max_uses")),
                UsedCount = reader.GetInt32(reader.GetOrdinal("used_count")),
                ExpiresAt = reader.IsDBNull(reader.GetOrdinal("expires_at")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("expires_at")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at"))
            },
            parameters,
            cancellationToken);

        return result;
    }

    public async Task<InvitationCode?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("code", NpgsqlDbType.Varchar) { Value = code }
        };

        var result = await _sqlExecutor.QuerySingleAsync(
            _getInvitationCodeByCodeQuery,
            reader => new InvitationCode
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                TeacherId = reader.GetInt64(reader.GetOrdinal("teacher_id")),
                Code = reader.GetString(reader.GetOrdinal("code")),
                MaxUses = reader.IsDBNull(reader.GetOrdinal("max_uses")) ? null : reader.GetInt32(reader.GetOrdinal("max_uses")),
                UsedCount = reader.GetInt32(reader.GetOrdinal("used_count")),
                ExpiresAt = reader.IsDBNull(reader.GetOrdinal("expires_at")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("expires_at")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at"))
            },
            parameters,
            cancellationToken);

        return result;
    }

    public async Task UpdateAsync(InvitationCode invitationCode, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("id", NpgsqlDbType.Bigint) { Value = invitationCode.Id },
            new NpgsqlParameter("status", NpgsqlDbType.Varchar) { Value = invitationCode.Status },
            new NpgsqlParameter("used_count", NpgsqlDbType.Integer) { Value = invitationCode.UsedCount }
        };

        await _sqlExecutor.ExecuteAsync(_updateInvitationCodeQuery, parameters, cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("id", NpgsqlDbType.Bigint) { Value = id }
        };

        await _sqlExecutor.ExecuteAsync(_deleteInvitationCodeQuery, parameters, cancellationToken);
    }
}

