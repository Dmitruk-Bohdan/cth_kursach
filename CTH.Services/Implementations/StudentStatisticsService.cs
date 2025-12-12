using CTH.Database.Abstractions;
using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Statistics;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using PropTechPeople.Services.Models.ResultApiModels;
using System.Net;

namespace CTH.Services.Implementations;

public class StudentStatisticsService : IStudentStatisticsService
{
    private readonly IUserStatsRepository _userStatsRepository;
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ISqlQueryProvider _sqlQueryProvider;
    private readonly ILogger<StudentStatisticsService> _logger;

    public StudentStatisticsService(
        IUserStatsRepository userStatsRepository,
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        ILogger<StudentStatisticsService> logger)
    {
        _userStatsRepository = userStatsRepository;
        _sqlExecutor = sqlExecutor;
        _sqlQueryProvider = sqlQueryProvider;
        _logger = logger;
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<SubjectDto>>> GetAllSubjectsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Requested all subjects");
        
        var query = _sqlQueryProvider.GetQuery("StatisticsUseCases/Queries/GetAllSubjects");
        var subjects = await _sqlExecutor.QueryAsync(
            query,
            reader => new SubjectDto
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                SubjectCode = reader.GetString(reader.GetOrdinal("subject_code")),
                SubjectName = reader.GetString(reader.GetOrdinal("subject_name"))
            },
            Array.Empty<NpgsqlParameter>(),
            cancellationToken);

        _logger.LogInformation("Found {Count} subjects", subjects.Count);
        return new HttpOperationResult<IReadOnlyCollection<SubjectDto>>(subjects, HttpStatusCode.OK);
    }

    public async Task<HttpOperationResult<SubjectStatisticsDto>> GetSubjectStatisticsAsync(long userId, long subjectId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} requested statistics for subject {SubjectId}", userId, subjectId);
        
        var stats = await _userStatsRepository.GetSubjectStatisticsWithTopicsAsync(userId, subjectId, cancellationToken);
        
        // Первая запись - общая статистика по предмету (topic_id IS NULL)
        var subjectStat = stats.FirstOrDefault(s => s.TopicId == null);
        
        // Топ 3 темы с ошибками (первые 3 после общей статистики)
        var top3ErrorTopics = stats
            .Where(s => s.TopicId.HasValue && s.AttemptsTotal > 0)
            .OrderByDescending(s => s.AttemptsTotal - s.CorrectTotal) // По количеству ошибок
            .ThenBy(s => s.CorrectTotal * 100.0m / s.AttemptsTotal) // По проценту успешности
            .Take(3)
            .Select(s => new TopicStatisticsDto
            {
                TopicId = s.TopicId,
                TopicName = s.Topic?.TopicName ?? "Unknown",
                AttemptsTotal = s.AttemptsTotal,
                CorrectTotal = s.CorrectTotal,
                ErrorsCount = s.AttemptsTotal - s.CorrectTotal,
                AccuracyPercentage = s.AttemptsTotal > 0 ? (decimal)s.CorrectTotal / s.AttemptsTotal * 100 : null,
                LastAttemptAt = s.LastAttemptAt
            })
            .ToArray();

        var top3TopicIds = top3ErrorTopics.Select(t => t.TopicId).ToHashSet();

        // Остальные темы с статистикой, отсортированные по возрастанию процента успешности
        var otherTopics = stats
            .Where(s => s.TopicId.HasValue && s.AttemptsTotal > 0 && !top3TopicIds.Contains(s.TopicId))
            .OrderBy(s => s.CorrectTotal * 100.0m / s.AttemptsTotal) // По проценту успешности (от низкого к высокому)
            .ThenByDescending(s => s.AttemptsTotal - s.CorrectTotal) // По количеству ошибок
            .Select(s => new TopicStatisticsDto
            {
                TopicId = s.TopicId,
                TopicName = s.Topic?.TopicName ?? "Unknown",
                AttemptsTotal = s.AttemptsTotal,
                CorrectTotal = s.CorrectTotal,
                ErrorsCount = s.AttemptsTotal - s.CorrectTotal,
                AccuracyPercentage = s.AttemptsTotal > 0 ? (decimal)s.CorrectTotal / s.AttemptsTotal * 100 : null,
                LastAttemptAt = s.LastAttemptAt
            })
            .ToArray();

        // Темы без статистики
        var unattemptedTopics = stats
            .Where(s => s.TopicId.HasValue && s.AttemptsTotal == 0)
            .Select(s => new TopicStatisticsDto
            {
                TopicId = s.TopicId,
                TopicName = s.Topic?.TopicName ?? "Unknown",
                AttemptsTotal = 0,
                CorrectTotal = 0,
                ErrorsCount = 0,
                AccuracyPercentage = null,
                LastAttemptAt = null
            })
            .ToArray();

        var result = new SubjectStatisticsDto
        {
            OverallAccuracyPercentage = subjectStat != null && subjectStat.AttemptsTotal > 0
                ? (decimal)subjectStat.CorrectTotal / subjectStat.AttemptsTotal * 100
                : null,
            OverallAttemptsTotal = subjectStat?.AttemptsTotal ?? 0,
            OverallCorrectTotal = subjectStat?.CorrectTotal ?? 0,
            Top3ErrorTopics = top3ErrorTopics,
            OtherTopics = otherTopics,
            UnattemptedTopics = unattemptedTopics
        };

        _logger.LogInformation("Prepared statistics for user {UserId}, subject {SubjectId}: {Top3Count} top errors, {OtherCount} other topics, {UnattemptedCount} unattempted", 
            userId, subjectId, top3ErrorTopics.Length, otherTopics.Length, unattemptedTopics.Length);
        
        return new HttpOperationResult<SubjectStatisticsDto>(result, HttpStatusCode.OK);
    }
}

