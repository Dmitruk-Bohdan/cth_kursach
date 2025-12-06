using CTH.Database.Abstractions;
using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace CTH.Database.Repositories;

public class UserStatsRepository : IUserStatsRepository
{
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ILogger<UserStatsRepository> _logger;
    private readonly string _getStatisticsBySubjectQuery;
    private readonly string _getStatisticsByTopicQuery;

    public UserStatsRepository(
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        ILogger<UserStatsRepository> logger)
    {
        _sqlExecutor = sqlExecutor;
        _logger = logger;
        _getStatisticsBySubjectQuery = sqlQueryProvider.GetQuery("StatisticsUseCases/Queries/GetUserStatisticsBySubject");
        _getStatisticsByTopicQuery = sqlQueryProvider.GetQuery("StatisticsUseCases/Queries/GetUserStatisticsByTopic");
    }

    public Task<IReadOnlyCollection<UserStats>> GetStatisticsBySubjectAsync(long userId, long? subjectId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId },
            new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = (object?)subjectId ?? DBNull.Value }
        };

        return _sqlExecutor.QueryAsync(
            _getStatisticsBySubjectQuery,
            reader => new UserStats
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
                SubjectId = reader.IsDBNull(reader.GetOrdinal("subject_id")) ? null : reader.GetInt64(reader.GetOrdinal("subject_id")),
                TopicId = reader.IsDBNull(reader.GetOrdinal("topic_id")) ? null : reader.GetInt64(reader.GetOrdinal("topic_id")),
                AttemptsTotal = reader.GetInt32(reader.GetOrdinal("attempts_total")),
                CorrectTotal = reader.GetInt32(reader.GetOrdinal("correct_total")),
                LastAttemptAt = reader.IsDBNull(reader.GetOrdinal("last_attempt_at")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_attempt_at")),
                AverageScore = reader.IsDBNull(reader.GetOrdinal("average_score")) ? null : reader.GetDecimal(reader.GetOrdinal("average_score")),
                AverageTimeSec = reader.IsDBNull(reader.GetOrdinal("average_time_sec")) ? null : reader.GetInt32(reader.GetOrdinal("average_time_sec")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")),
                Subject = reader.IsDBNull(reader.GetOrdinal("subject_id")) ? null : new Subject
                {
                    Id = reader.GetInt64(reader.GetOrdinal("subject_id")),
                    SubjectName = reader.GetString(reader.GetOrdinal("subject_name"))
                }
            },
            parameters,
            cancellationToken);
    }

    public Task<IReadOnlyCollection<UserStats>> GetStatisticsByTopicAsync(long userId, long? subjectId, CancellationToken cancellationToken)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId },
            new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = (object?)subjectId ?? DBNull.Value }
        };

        return _sqlExecutor.QueryAsync(
            _getStatisticsByTopicQuery,
            reader => new UserStats
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
                SubjectId = reader.IsDBNull(reader.GetOrdinal("subject_id")) ? null : reader.GetInt64(reader.GetOrdinal("subject_id")),
                TopicId = reader.IsDBNull(reader.GetOrdinal("topic_id")) ? null : reader.GetInt64(reader.GetOrdinal("topic_id")),
                AttemptsTotal = reader.GetInt32(reader.GetOrdinal("attempts_total")),
                CorrectTotal = reader.GetInt32(reader.GetOrdinal("correct_total")),
                LastAttemptAt = reader.IsDBNull(reader.GetOrdinal("last_attempt_at")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("last_attempt_at")),
                AverageScore = reader.IsDBNull(reader.GetOrdinal("average_score")) ? null : reader.GetDecimal(reader.GetOrdinal("average_score")),
                AverageTimeSec = reader.IsDBNull(reader.GetOrdinal("average_time_sec")) ? null : reader.GetInt32(reader.GetOrdinal("average_time_sec")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")),
                Subject = reader.IsDBNull(reader.GetOrdinal("subject_id")) ? null : new Subject
                {
                    Id = reader.GetInt64(reader.GetOrdinal("subject_id")),
                    SubjectName = reader.GetString(reader.GetOrdinal("subject_name"))
                },
                Topic = reader.IsDBNull(reader.GetOrdinal("topic_id")) ? null : new Topic
                {
                    Id = reader.GetInt64(reader.GetOrdinal("topic_id")),
                    TopicName = reader.GetString(reader.GetOrdinal("topic_name"))
                }
            },
            parameters,
            cancellationToken);
    }
}

