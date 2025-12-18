using CTH.Database.Abstractions;
using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace CTH.Database.Repositories;

public class TeacherStudentRepository : ITeacherStudentRepository
{
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ILogger<TeacherStudentRepository> _logger;
    private readonly string _createTeacherStudentQuery;
    private readonly string _getTeachersByStudentQuery;
    private readonly string _getStudentsByTeacherQuery;
    private readonly string _getByTeacherAndStudentQuery;
    private readonly string _deleteTeacherStudentQuery;

    public TeacherStudentRepository(
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        ILogger<TeacherStudentRepository> logger)
    {
        _sqlExecutor = sqlExecutor;
        _logger = logger;
        _createTeacherStudentQuery = sqlQueryProvider.GetQuery("TeacherStudentUseCases/Commands/CreateTeacherStudent");
        _getTeachersByStudentQuery = sqlQueryProvider.GetQuery("TeacherStudentUseCases/Queries/GetTeachersByStudent");
        _getStudentsByTeacherQuery = sqlQueryProvider.GetQuery("TeacherStudentUseCases/Queries/GetStudentsByTeacher");
        _getByTeacherAndStudentQuery = sqlQueryProvider.GetQuery("TeacherStudentUseCases/Queries/GetByTeacherAndStudent");
        _deleteTeacherStudentQuery = sqlQueryProvider.GetQuery("TeacherStudentUseCases/Commands/DeleteTeacherStudent");
    }

    public async Task<long> CreateAsync(TeacherStudent teacherStudent, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("teacher_id", NpgsqlDbType.Bigint) { Value = teacherStudent.TeacherId },
            new NpgsqlParameter("student_id", NpgsqlDbType.Bigint) { Value = teacherStudent.StudentId },
            new NpgsqlParameter("status", NpgsqlDbType.Varchar) { Value = teacherStudent.Status },
            new NpgsqlParameter("established_at", NpgsqlDbType.TimestampTz) { Value = (object?)teacherStudent.EstablishedAt ?? DBNull.Value }
        };

        var id = await _sqlExecutor.QuerySingleAsync(
            _createTeacherStudentQuery,
            reader => reader.GetInt64(0),
            parameters,
            cancellationToken);

        if (id == 0)
        {
            throw new InvalidOperationException("Failed to create teacher-student relationship");
        }

        return id;
    }

    public async Task<IReadOnlyCollection<TeacherStudent>> GetTeachersByStudentIdAsync(long studentId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("student_id", NpgsqlDbType.Bigint) { Value = studentId }
        };

        var result = await _sqlExecutor.QueryAsync(
            _getTeachersByStudentQuery,
            reader => new TeacherStudent
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                TeacherId = reader.GetInt64(reader.GetOrdinal("teacher_id")),
                StudentId = reader.GetInt64(reader.GetOrdinal("student_id")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                EstablishedAt = reader.IsDBNull(reader.GetOrdinal("established_at")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("established_at")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")),
                Teacher = new UserAccount
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

    public async Task<IReadOnlyCollection<TeacherStudent>> GetStudentsByTeacherIdAsync(long teacherId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("teacher_id", NpgsqlDbType.Bigint) { Value = teacherId }
        };

        var result = await _sqlExecutor.QueryAsync(
            _getStudentsByTeacherQuery,
            reader => new TeacherStudent
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                TeacherId = reader.GetInt64(reader.GetOrdinal("teacher_id")),
                StudentId = reader.GetInt64(reader.GetOrdinal("student_id")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                EstablishedAt = reader.IsDBNull(reader.GetOrdinal("established_at")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("established_at")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")),
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

    public async Task<TeacherStudent?> GetByTeacherAndStudentAsync(long teacherId, long studentId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("teacher_id", NpgsqlDbType.Bigint) { Value = teacherId },
            new NpgsqlParameter("student_id", NpgsqlDbType.Bigint) { Value = studentId }
        };

        var result = await _sqlExecutor.QuerySingleAsync(
            _getByTeacherAndStudentQuery,
            reader => new TeacherStudent
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                TeacherId = reader.GetInt64(reader.GetOrdinal("teacher_id")),
                StudentId = reader.GetInt64(reader.GetOrdinal("student_id")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                EstablishedAt = reader.IsDBNull(reader.GetOrdinal("established_at")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("established_at")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at"))
            },
            parameters,
            cancellationToken);

        return result;
    }

    public async Task DeleteAsync(long teacherId, long studentId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("teacher_id", NpgsqlDbType.Bigint) { Value = teacherId },
            new NpgsqlParameter("student_id", NpgsqlDbType.Bigint) { Value = studentId }
        };

        await _sqlExecutor.ExecuteAsync(_deleteTeacherStudentQuery, parameters, cancellationToken);
    }
}

