using CTH.Database.Abstractions;
using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace CTH.Database.Repositories;

public class TestStudentAccessRepository : ITestStudentAccessRepository
{
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ILogger<TestStudentAccessRepository> _logger;
    private readonly string _addStudentAccessQuery;
    private readonly string _removeStudentAccessQuery;
    private readonly string _removeAllStudentAccessQuery;
    private readonly string _getStudentsByTestQuery;

    public TestStudentAccessRepository(
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        ILogger<TestStudentAccessRepository> logger)
    {
        _sqlExecutor = sqlExecutor;
        _logger = logger;
        _addStudentAccessQuery = sqlQueryProvider.GetQuery("TestStudentAccessUseCases/Commands/AddStudentAccess");
        _removeStudentAccessQuery = sqlQueryProvider.GetQuery("TestStudentAccessUseCases/Commands/RemoveStudentAccess");
        _removeAllStudentAccessQuery = sqlQueryProvider.GetQuery("TestStudentAccessUseCases/Commands/RemoveAllStudentAccess");
        _getStudentsByTestQuery = sqlQueryProvider.GetQuery("TestStudentAccessUseCases/Queries/GetStudentsByTest");
    }

    public async Task<long> AddStudentAccessAsync(long testId, long studentId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("test_id", NpgsqlDbType.Bigint) { Value = testId },
            new NpgsqlParameter("student_id", NpgsqlDbType.Bigint) { Value = studentId }
        };

        var id = await _sqlExecutor.QuerySingleAsync(
            _addStudentAccessQuery,
            reader => reader.GetInt64(0),
            parameters,
            cancellationToken);

        return id;
    }

    public async Task RemoveStudentAccessAsync(long testId, long studentId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("test_id", NpgsqlDbType.Bigint) { Value = testId },
            new NpgsqlParameter("student_id", NpgsqlDbType.Bigint) { Value = studentId }
        };

        await _sqlExecutor.ExecuteAsync(_removeStudentAccessQuery, parameters, cancellationToken);
    }

    public async Task RemoveAllStudentAccessAsync(long testId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("test_id", NpgsqlDbType.Bigint) { Value = testId }
        };

        await _sqlExecutor.ExecuteAsync(_removeAllStudentAccessQuery, parameters, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TestStudentAccess>> GetStudentsByTestIdAsync(long testId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("test_id", NpgsqlDbType.Bigint) { Value = testId }
        };

        var result = await _sqlExecutor.QueryAsync(
            _getStudentsByTestQuery,
            reader => new TestStudentAccess
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                TestId = reader.GetInt64(reader.GetOrdinal("test_id")),
                StudentId = reader.GetInt64(reader.GetOrdinal("student_id")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                Student = new UserAccount
                {
                    Id = reader.GetInt64(reader.GetOrdinal("user_id")),
                    UserName = reader.GetString(reader.GetOrdinal("user_name")),
                    Email = reader.GetString(reader.GetOrdinal("email"))
                }
            },
            parameters,
            cancellationToken);

        return result;
    }
}

