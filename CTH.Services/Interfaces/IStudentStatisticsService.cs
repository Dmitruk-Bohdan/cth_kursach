using CTH.Services.Models.Dto.Statistics;
using PropTechPeople.Services.Models.ResultApiModels;

namespace CTH.Services.Interfaces;

public interface IStudentStatisticsService
{
    Task<HttpOperationResult<IReadOnlyCollection<SubjectDto>>> GetAllSubjectsAsync(CancellationToken cancellationToken);
    Task<HttpOperationResult<SubjectStatisticsDto>> GetSubjectStatisticsAsync(long userId, long subjectId, CancellationToken cancellationToken);
}

