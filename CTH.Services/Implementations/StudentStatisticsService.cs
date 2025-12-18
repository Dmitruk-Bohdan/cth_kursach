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
        
        
        var subjectStat = stats.FirstOrDefault(s => s.TopicId == null);
        
        
        var top3ErrorTopics = stats
            .Where(s => s.TopicId.HasValue && s.AttemptsTotal > 0)
            .OrderByDescending(s => s.AttemptsTotal - s.CorrectTotal) 
            .ThenBy(s => s.CorrectTotal * 100.0m / s.AttemptsTotal) 
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

        
        var otherTopics = stats
            .Where(s => s.TopicId.HasValue && s.AttemptsTotal > 0 && !top3TopicIds.Contains(s.TopicId))
            .OrderBy(s => s.CorrectTotal * 100.0m / s.AttemptsTotal) 
            .ThenByDescending(s => s.AttemptsTotal - s.CorrectTotal) 
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

        
        var getAllTopicsQuery = _sqlQueryProvider.GetQuery("StatisticsUseCases/Queries/GetAllTopicsBySubject");
        var allTopicsFromDb = await _sqlExecutor.QueryAsync(
            getAllTopicsQuery,
            reader => new Topic
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                TopicName = reader.GetString(reader.GetOrdinal("topic_name")),
                SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active"))
            },
            new[] { new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = subjectId } },
            cancellationToken);

        
        var allTopics = allTopicsFromDb
            .GroupBy(t => t.Id)
            .Select(g => g.First())
            .ToList();

        
        
        var topicsWithAnswers = new HashSet<long>();
        foreach (var stat in stats)
        {
            if (stat.TopicId.HasValue && stat.AttemptsTotal > 0)
            {
                topicsWithAnswers.Add(stat.TopicId.Value);
            }
        }

        
        foreach (var topic in top3ErrorTopics)
        {
            if (topic.TopicId.HasValue)
            {
                topicsWithAnswers.Add(topic.TopicId.Value);
            }
        }
        foreach (var topic in otherTopics)
        {
            if (topic.TopicId.HasValue)
            {
                topicsWithAnswers.Add(topic.TopicId.Value);
            }
        }

        _logger.LogInformation("Found {Count} topics with answers for user {UserId}, subject {SubjectId}: {TopicIds}", 
            topicsWithAnswers.Count, userId, subjectId, string.Join(", ", topicsWithAnswers));
        _logger.LogInformation("Found {Count} total topics for subject {SubjectId}: {TopicNames}", 
            allTopics.Count, subjectId, string.Join(", ", allTopics.Select(t => $"{t.Id}:{t.TopicName}")));

        
        var unattemptedTopics = allTopics
            .Where(t => !topicsWithAnswers.Contains(t.Id))
            .Select(t => new TopicStatisticsDto
            {
                TopicId = t.Id,
                TopicName = t.TopicName,
                AttemptsTotal = 0,
                CorrectTotal = 0,
                ErrorsCount = 0,
                AccuracyPercentage = null,
                LastAttemptAt = null
            })
            .OrderBy(t => t.TopicName)
            .ToArray();

        _logger.LogInformation("Found {Count} unattempted topics: {TopicNames}", 
            unattemptedTopics.Length, string.Join(", ", unattemptedTopics.Select(t => t.TopicName)));

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

