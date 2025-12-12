using CTH.Database.Entities.Public;
using CTH.Database.Models;
using CTH.Database.Repositories.Interfaces;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Tests;
using PropTechPeople.Services.Models.ResultApiModels;
using System.Net;
using System.Linq;

namespace CTH.Services.Implementations;

public class StudentTestService : IStudentTestService
{
    private readonly ITestRepository _testRepository;

    public StudentTestService(ITestRepository testRepository)
    {
        _testRepository = testRepository;
    }

    public async Task<IReadOnlyCollection<TestListItemDto>> GetPublishedTestsAsync(long userId, TestListFilter filter, CancellationToken cancellationToken)
    {
        var tests = await _testRepository.GetPublishedTestsAsync(userId, filter, cancellationToken);
        return tests.Select(t => new TestListItemDto
        {
            Id = t.Id,
            Title = t.Title,
            TestKind = t.TestKind,
            SubjectId = t.SubjectId,
            TimeLimitSec = t.TimeLimitSec,
            AttemptsAllowed = t.AttemptsAllowed,
            IsPublic = t.IsPublic,
            IsStateArchive = t.IsStateArchive,
            Mode = t.Mode
        }).ToArray();
    }

    public async Task<HttpOperationResult<TestDetailsDto>> GetTestDetailsAsync(long testId, CancellationToken cancellationToken)
    {
        var test = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
        if (test == null || !IsTestAccessible(test))
        {
            return new HttpOperationResult<TestDetailsDto>
            {
                Status = HttpStatusCode.NotFound,
                Error = $"Test {testId} not found"
            };
        }

        var tasks = await _testRepository.GetTestTasksAsync(testId, cancellationToken);

        var dto = new TestDetailsDto
        {
            Id = test.Id,
            Title = test.Title,
            TestKind = test.TestKind,
            SubjectId = test.SubjectId,
            AttemptsAllowed = test.AttemptsAllowed,
            TimeLimitSec = test.TimeLimitSec,
            IsPublic = test.IsPublic,
            IsStateArchive = test.IsStateArchive,
            Tasks = tasks
                .OrderBy(t => t.Position)
                .Select(t => new TestTaskDto
                {
                    TaskId = t.TaskId,
                    Position = t.Position,
                    TaskType = t.Task?.TaskType ?? string.Empty,
                    Statement = t.Task?.Statement ?? string.Empty,
                    Difficulty = t.Task?.Difficulty ?? default,
                    Explanation = t.Task?.Explanation
                }).ToArray()
        };

        return new HttpOperationResult<TestDetailsDto>(dto, HttpStatusCode.OK);
    }

    private static bool IsTestAccessible(Test test)
    {
        return test.IsPublished && (test.IsPublic || test.IsStateArchive);
    }
}
