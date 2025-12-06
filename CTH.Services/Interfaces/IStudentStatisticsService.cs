using CTH.Services.Models.Dto.Statistics;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface IStudentStatisticsService
{
    Task<HttpOperationResult<IReadOnlyCollection<UserStatisticsDto>>> GetStatisticsBySubjectAsync(long userId, long? subjectId, CancellationToken cancellationToken);
    Task<HttpOperationResult<IReadOnlyCollection<UserStatisticsDto>>> GetStatisticsByTopicAsync(long userId, long? subjectId, CancellationToken cancellationToken);
}

