using CTH.Database.Repositories.Interfaces;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Statistics;
using Microsoft.Extensions.Logging;
using PropTechPeople.Services.Models.ResultApiModels;
using System.Net;

namespace CTH.Services.Implementations;

public class StudentStatisticsService : IStudentStatisticsService
{
    private readonly IUserStatsRepository _userStatsRepository;
    private readonly ILogger<StudentStatisticsService> _logger;

    public StudentStatisticsService(
        IUserStatsRepository userStatsRepository,
        ILogger<StudentStatisticsService> logger)
    {
        _userStatsRepository = userStatsRepository;
        _logger = logger;
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<UserStatisticsDto>>> GetStatisticsBySubjectAsync(long userId, long? subjectId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} requested statistics by subject. SubjectId={SubjectId}", userId, subjectId);
        
        var stats = await _userStatsRepository.GetStatisticsBySubjectAsync(userId, subjectId, cancellationToken);
        
        var dtos = stats.Select(s => new UserStatisticsDto
        {
            Id = s.Id,
            SubjectId = s.SubjectId,
            SubjectName = s.Subject?.SubjectName,
            TopicId = s.TopicId,
            TopicName = s.Topic?.TopicName,
            AttemptsTotal = s.AttemptsTotal,
            CorrectTotal = s.CorrectTotal,
            AccuracyPercentage = s.AttemptsTotal > 0 ? (decimal)s.CorrectTotal / s.AttemptsTotal * 100 : null,
            LastAttemptAt = s.LastAttemptAt,
            AverageScore = s.AverageScore,
            AverageTimeSec = s.AverageTimeSec
        }).ToArray();

        _logger.LogInformation("Found {Count} subject statistics for user {UserId}", dtos.Length, userId);
        return new HttpOperationResult<IReadOnlyCollection<UserStatisticsDto>>(dtos, HttpStatusCode.OK);
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<UserStatisticsDto>>> GetStatisticsByTopicAsync(long userId, long? subjectId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} requested statistics by topic. SubjectId={SubjectId}", userId, subjectId);
        
        var stats = await _userStatsRepository.GetStatisticsByTopicAsync(userId, subjectId, cancellationToken);
        
        var dtos = stats.Select(s => new UserStatisticsDto
        {
            Id = s.Id,
            SubjectId = s.SubjectId,
            SubjectName = s.Subject?.SubjectName,
            TopicId = s.TopicId,
            TopicName = s.Topic?.TopicName,
            AttemptsTotal = s.AttemptsTotal,
            CorrectTotal = s.CorrectTotal,
            AccuracyPercentage = s.AttemptsTotal > 0 ? (decimal)s.CorrectTotal / s.AttemptsTotal * 100 : null,
            LastAttemptAt = s.LastAttemptAt,
            AverageScore = s.AverageScore,
            AverageTimeSec = s.AverageTimeSec
        }).ToArray();

        _logger.LogInformation("Found {Count} topic statistics for user {UserId}", dtos.Length, userId);
        return new HttpOperationResult<IReadOnlyCollection<UserStatisticsDto>>(dtos, HttpStatusCode.OK);
    }
}

