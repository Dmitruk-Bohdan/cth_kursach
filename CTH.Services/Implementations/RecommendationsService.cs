using CTH.Database.Abstractions;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Recommendations;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using PropTechPeople.Services.Models.ResultApiModels;
using System.Net;

namespace CTH.Services.Implementations;

public class RecommendationsService : IRecommendationsService
{
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ISqlQueryProvider _sqlQueryProvider;
    private readonly ILogger<RecommendationsService> _logger;

    public RecommendationsService(
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        ILogger<RecommendationsService> logger)
    {
        _sqlExecutor = sqlExecutor;
        _sqlQueryProvider = sqlQueryProvider;
        _logger = logger;
    }

    public async Task<HttpOperationResult<RecommendationsDto>> GetRecommendationsAsync(
        long userId, 
        long subjectId, 
        int criticalThreshold = 80, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User {UserId} requested recommendations for subject {SubjectId} with threshold {Threshold}", 
            userId, subjectId, criticalThreshold);

        try
        {
            // Получаем критические темы
        var criticalTopicsQuery = _sqlQueryProvider.GetQuery("RecommendationsUseCases/Queries/GetCriticalTopics");
        var criticalTopics = await _sqlExecutor.QueryAsync(
            criticalTopicsQuery,
            reader =>
            {
                var topicNameOrdinal = reader.GetOrdinal("topic_name");
                var topicCodeOrdinal = reader.GetOrdinal("topic_code");
                var lastAttemptAtOrdinal = reader.GetOrdinal("last_attempt_at");
                var attemptsTotal = reader.GetInt32(reader.GetOrdinal("attempts_total"));
                var correctTotal = reader.GetInt32(reader.GetOrdinal("correct_total"));
                return new TopicRecommendationDto
                {
                    TopicId = reader.GetInt64(reader.GetOrdinal("topic_id")),
                    TopicName = reader.IsDBNull(topicNameOrdinal) ? string.Empty : reader.GetString(topicNameOrdinal),
                    TopicCode = reader.IsDBNull(topicCodeOrdinal) ? null : reader.GetString(topicCodeOrdinal),
                    AttemptsTotal = attemptsTotal == 0 ? null : attemptsTotal,
                    CorrectTotal = correctTotal == 0 ? null : correctTotal,
                    AccuracyPercentage = reader.IsDBNull(reader.GetOrdinal("accuracy_percentage")) ? null : reader.GetDecimal(reader.GetOrdinal("accuracy_percentage")),
                    LastAttemptAt = reader.IsDBNull(lastAttemptAtOrdinal) ? null : reader.GetFieldValue<DateTimeOffset>(lastAttemptAtOrdinal)
                };
            },
            new[]
            {
                new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId },
                new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = subjectId },
                new NpgsqlParameter("critical_threshold", NpgsqlDbType.Integer) { Value = criticalThreshold }
            },
            cancellationToken);

        // Получаем темы для повторения по Лейтнеру
        var leitnerTopicsQuery = _sqlQueryProvider.GetQuery("RecommendationsUseCases/Queries/GetLeitnerTopics");
        var leitnerTopics = await _sqlExecutor.QueryAsync(
            leitnerTopicsQuery,
            reader =>
            {
                var topicNameOrdinal = reader.GetOrdinal("topic_name");
                var topicCodeOrdinal = reader.GetOrdinal("topic_code");
                var lastAttemptAtOrdinal = reader.GetOrdinal("last_attempt_at");
                return new TopicRecommendationDto
                {
                    TopicId = reader.GetInt64(reader.GetOrdinal("topic_id")),
                    TopicName = reader.IsDBNull(topicNameOrdinal) ? string.Empty : reader.GetString(topicNameOrdinal),
                    TopicCode = reader.IsDBNull(topicCodeOrdinal) ? null : reader.GetString(topicCodeOrdinal),
                    SuccessfulRepetitions = reader.IsDBNull(reader.GetOrdinal("successful_repetitions")) ? null : reader.GetInt32(reader.GetOrdinal("successful_repetitions")),
                    RepetitionIntervalDays = reader.IsDBNull(reader.GetOrdinal("repetition_interval_days")) ? null : reader.GetInt32(reader.GetOrdinal("repetition_interval_days")),
                    LastAttemptAt = reader.IsDBNull(lastAttemptAtOrdinal) ? null : reader.GetFieldValue<DateTimeOffset>(lastAttemptAtOrdinal)
                };
            },
            new[]
            {
                new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId },
                new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = subjectId }
            },
            cancellationToken);

        // Получаем неизученные темы
        var unstudiedTopicsQuery = _sqlQueryProvider.GetQuery("RecommendationsUseCases/Queries/GetUnstudiedTopics");
        var unstudiedTopics = await _sqlExecutor.QueryAsync(
            unstudiedTopicsQuery,
            reader =>
            {
                var topicNameOrdinal = reader.GetOrdinal("topic_name");
                var topicCodeOrdinal = reader.GetOrdinal("topic_code");
                return new TopicRecommendationDto
                {
                    TopicId = reader.GetInt64(reader.GetOrdinal("topic_id")),
                    TopicName = reader.IsDBNull(topicNameOrdinal) ? string.Empty : reader.GetString(topicNameOrdinal),
                    TopicCode = reader.IsDBNull(topicCodeOrdinal) ? null : reader.GetString(topicCodeOrdinal)
                };
            },
            new[]
            {
                new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId },
                new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = subjectId }
            },
            cancellationToken);

            var result = new RecommendationsDto
            {
                CriticalTopics = criticalTopics,
                LeitnerTopics = leitnerTopics,
                UnstudiedTopics = unstudiedTopics,
                CriticalThreshold = criticalThreshold
            };

            _logger.LogInformation("Prepared recommendations for user {UserId}, subject {SubjectId}: {CriticalCount} critical, {LeitnerCount} leitner, {UnstudiedCount} unstudied", 
                userId, subjectId, criticalTopics.Count, leitnerTopics.Count, unstudiedTopics.Count);

            return new HttpOperationResult<RecommendationsDto>(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for user {UserId}, subject {SubjectId}", userId, subjectId);
            return new HttpOperationResult<RecommendationsDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = $"Error getting recommendations: {ex.Message}"
            };
        }
    }

    public async Task<HttpOperationResult> UpdateCriticalThresholdAsync(
        long userId, 
        int newThreshold, 
        CancellationToken cancellationToken = default)
    {
        // TODO: Сохранить порог в настройках пользователя (пока просто возвращаем успех)
        _logger.LogInformation("User {UserId} updated critical threshold to {Threshold}", userId, newThreshold);
        return new HttpOperationResult(HttpStatusCode.OK);
    }
}

