using CTH.Common.Enums;
using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Tests;
using PropTechPeople.Services.Models.ResultApiModels;
using System.Net;
using System.Linq;

namespace CTH.Services.Implementations;

public class TeacherTestService : ITeacherTestService
{
    private readonly ITestRepository _testRepository;

    public TeacherTestService(ITestRepository testRepository)
    {
        _testRepository = testRepository;
    }

    public async Task<HttpOperationResult<TestDetailsDto>> CreateTestAsync(long userId, bool isAdmin, CreateTestRequestDto request, CancellationToken cancellationToken)
    {
        if (request.IsStateArchive && !isAdmin)
        {
            return new HttpOperationResult<TestDetailsDto>
            {
                Status = HttpStatusCode.Forbidden,
                Error = "Only admin can create state archive tests"
            };
        }

        var newTest = new Test
        {
            SubjectId = request.SubjectId,
            TestKind = request.TestKind,
            Title = request.Title,
            AuthorId = request.IsStateArchive ? null : userId,
            TimeLimitSec = request.TimeLimitSec,
            AttemptsAllowed = request.AttemptsAllowed,
            Mode = request.Mode,
            IsPublished = request.IsPublished,
            IsPublic = request.IsStateArchive || request.IsPublic,
            IsStateArchive = request.IsStateArchive
        };

        var testId = await _testRepository.CreateAsync(newTest, cancellationToken);

        var tasks = request.Tasks
            .OrderBy(t => t.Position)
            .Select(t => new TestTask
            {
                TestId = testId,
                TaskId = t.TaskId,
                Position = t.Position,
                Weight = t.Weight
            }).ToArray();

        if (tasks.Length > 0)
        {
            await _testRepository.ReplaceTasksAsync(testId, tasks, cancellationToken);
        }

        var testDetails = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
        var testTasks = await _testRepository.GetTestTasksAsync(testId, cancellationToken);

        var dto = new TestDetailsDto
        {
            Id = testId,
            Title = testDetails?.Title ?? newTest.Title,
            TestKind = testDetails?.TestKind ?? newTest.TestKind,
            SubjectId = testDetails?.SubjectId ?? newTest.SubjectId,
            TimeLimitSec = testDetails?.TimeLimitSec ?? newTest.TimeLimitSec,
            AttemptsAllowed = testDetails?.AttemptsAllowed ?? newTest.AttemptsAllowed,
            IsPublic = newTest.IsPublic,
            IsStateArchive = newTest.IsStateArchive,
            Tasks = testTasks.Select(t => new TestTaskDto
            {
                TaskId = t.TaskId,
                Position = t.Position,
                TaskType = t.Task?.TaskType ?? string.Empty,
                Statement = t.Task?.Statement ?? string.Empty,
                Difficulty = t.Task?.Difficulty ?? default,
                Explanation = t.Task?.Explanation
            }).ToArray()
        };

        return new HttpOperationResult<TestDetailsDto>(dto, HttpStatusCode.Created);
    }

    public async Task<HttpOperationResult> UpdateTestAsync(long userId, bool isAdmin, long testId, UpdateTestRequestDto request, CancellationToken cancellationToken)
    {
        var existing = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
        if (existing == null)
        {
            return new HttpOperationResult(HttpStatusCode.NotFound)
            {
                Error = $"Test {testId} not found"
            };
        }

        if (existing.IsStateArchive && !isAdmin)
        {
            return new HttpOperationResult(HttpStatusCode.Forbidden)
            {
                Error = "Only admin can modify state archive tests"
            };
        }

        if (request.IsStateArchive && !isAdmin)
        {
            return new HttpOperationResult(HttpStatusCode.Forbidden)
            {
                Error = "Only admin can mark test as state archive"
            };
        }

        if (!CanManage(existing, userId, isAdmin))
        {
            return new HttpOperationResult(HttpStatusCode.Forbidden)
            {
                Error = "Not enough permissions to modify this test"
            };
        }

        existing.SubjectId = request.SubjectId;
        existing.Title = request.Title;
        existing.TestKind = request.TestKind;
        existing.TimeLimitSec = request.TimeLimitSec;
        existing.AttemptsAllowed = request.AttemptsAllowed;
        existing.Mode = request.Mode;
        existing.IsPublished = request.IsPublished;
        existing.IsPublic = request.IsStateArchive || request.IsPublic;
        existing.IsStateArchive = request.IsStateArchive;
        if (existing.IsStateArchive)
        {
            existing.AuthorId = null;
        }
        else if (existing.AuthorId == null)
        {
            existing.AuthorId = userId;
        }

        await _testRepository.UpdateAsync(existing, cancellationToken);

        var tasks = request.Tasks
            .OrderBy(t => t.Position)
            .Select(t => new TestTask
            {
                TestId = testId,
                TaskId = t.TaskId,
                Position = t.Position,
                Weight = t.Weight
            }).ToArray();

        await _testRepository.ReplaceTasksAsync(testId, tasks, cancellationToken);

        return new HttpOperationResult(HttpStatusCode.NoContent);
    }

    public async Task<HttpOperationResult> DeleteTestAsync(long userId, bool isAdmin, long testId, CancellationToken cancellationToken)
    {
        var existing = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
        if (existing == null)
        {
            return new HttpOperationResult(HttpStatusCode.NotFound)
            {
                Error = $"Test {testId} not found"
            };
        }

        if (existing.IsStateArchive && !isAdmin)
        {
            return new HttpOperationResult(HttpStatusCode.Forbidden)
            {
                Error = "Only admin can delete state archive tests"
            };
        }

        if (!CanManage(existing, userId, isAdmin))
        {
            return new HttpOperationResult(HttpStatusCode.Forbidden)
            {
                Error = "Not enough permissions to delete this test"
            };
        }

        await _testRepository.DeleteAsync(testId, cancellationToken);
        return new HttpOperationResult(HttpStatusCode.NoContent);
    }

    public async Task<HttpOperationResult<TestDetailsDto>> GetTestAsync(long userId, bool isAdmin, long testId, CancellationToken cancellationToken)
    {
        var existing = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
        if (existing == null)
        {
            return new HttpOperationResult<TestDetailsDto>
            {
                Status = HttpStatusCode.NotFound,
                Error = $"Test {testId} not found"
            };
        }

        if (!(existing.IsPublic || existing.IsStateArchive) && !CanManage(existing, userId, isAdmin))
        {
            return new HttpOperationResult<TestDetailsDto>
            {
                Status = HttpStatusCode.Forbidden,
                Error = "Not enough permissions to view this test"
            };
        }

        var tasks = await _testRepository.GetTestTasksAsync(testId, cancellationToken);
        var dto = new TestDetailsDto
        {
            Id = existing.Id,
            Title = existing.Title,
            TestKind = existing.TestKind,
            SubjectId = existing.SubjectId,
            TimeLimitSec = existing.TimeLimitSec,
            AttemptsAllowed = existing.AttemptsAllowed,
            IsPublic = existing.IsPublic,
            IsStateArchive = existing.IsStateArchive,
            Tasks = tasks.Select(t => new TestTaskDto
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

    private static bool CanManage(Test test, long userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return true;
        }

        return test.AuthorId == userId;
    }
}
