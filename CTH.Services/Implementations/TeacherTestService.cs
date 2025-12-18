using CTH.Common.Enums;
using CTH.Database.Abstractions;
using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Tasks;
using CTH.Services.Models.Dto.Tests;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using PropTechPeople.Services.Models.ResultApiModels;
using System.Net;
using System.Linq;

namespace CTH.Services.Implementations;

public class TeacherTestService : ITeacherTestService
{
    private readonly ITestRepository _testRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ISqlQueryProvider _sqlQueryProvider;

    public TeacherTestService(
        ITestRepository testRepository, 
        ITaskRepository taskRepository,
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider)
    {
        _testRepository = testRepository;
        _taskRepository = taskRepository;
        _sqlExecutor = sqlExecutor;
        _sqlQueryProvider = sqlQueryProvider;
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
            Mode = existing.Mode,
            IsPublished = existing.IsPublished,
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

    public async Task<HttpOperationResult<IReadOnlyCollection<TestListItemDto>>> GetMyTestsAsync(long userId, long subjectId, CancellationToken cancellationToken)
    {
        var tests = await _testRepository.GetTestsByAuthorAndSubjectAsync(userId, subjectId, cancellationToken);
        var dtos = tests.Select(t => new TestListItemDto
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

        return new HttpOperationResult<IReadOnlyCollection<TestListItemDto>>(dtos, HttpStatusCode.OK);
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<TaskListItemDto>>> GetTasksBySubjectAsync(long subjectId, string? searchQuery, CancellationToken cancellationToken)
    {
        var tasks = await _taskRepository.GetTasksBySubjectAsync(subjectId, searchQuery, cancellationToken);
        
        var dto = tasks.Select(t => new TaskListItemDto
        {
            Id = t.Id,
            SubjectId = t.SubjectId,
            TopicId = t.TopicId,
            TopicName = t.Topic?.TopicName,
            TopicCode = t.Topic?.TopicCode,
            TaskType = t.TaskType,
            Difficulty = t.Difficulty,
            Statement = t.Statement,
            Explanation = t.Explanation,
            CorrectAnswer = t.CorrectAnswer
        }).ToArray();

        return new HttpOperationResult<IReadOnlyCollection<TaskListItemDto>>(dto, HttpStatusCode.OK);
    }

    public async Task<HttpOperationResult<TaskListItemDto>> CreateTaskAsync(long userId, bool isAdmin, CreateTaskRequestDto request, CancellationToken cancellationToken)
    {
        
        if (request.Difficulty < 1 || request.Difficulty > 5)
        {
            return new HttpOperationResult<TaskListItemDto>
            {
                Status = HttpStatusCode.BadRequest,
                Error = "Difficulty must be between 1 and 5"
            };
        }

        var validTaskTypes = new[] { "numeric", "text" };
        if (!validTaskTypes.Contains(request.TaskType.ToLower()))
        {
            return new HttpOperationResult<TaskListItemDto>
            {
                Status = HttpStatusCode.BadRequest,
                Error = $"Task type must be one of: {string.Join(", ", validTaskTypes)}"
            };
        }

        var newTask = new TaskItem
        {
            SubjectId = request.SubjectId,
            TopicId = request.TopicId,
            TaskType = request.TaskType.ToLower(),
            Difficulty = request.Difficulty,
            Statement = request.Statement,
            CorrectAnswer = request.CorrectAnswer,
            Explanation = request.Explanation,
            IsActive = request.IsActive
        };

        var taskId = await _taskRepository.CreateAsync(newTask, cancellationToken);

        
        var tasks = await _taskRepository.GetTasksBySubjectAsync(request.SubjectId, taskId.ToString(), cancellationToken);
        var createdTask = tasks.FirstOrDefault(t => t.Id == taskId);

        if (createdTask == null)
        {
            return new HttpOperationResult<TaskListItemDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to retrieve created task"
            };
        }

        var dto = new TaskListItemDto
        {
            Id = createdTask.Id,
            SubjectId = createdTask.SubjectId,
            TopicId = createdTask.TopicId,
            TopicName = createdTask.Topic?.TopicName,
            TopicCode = createdTask.Topic?.TopicCode,
            TaskType = createdTask.TaskType,
            Difficulty = createdTask.Difficulty,
            Statement = createdTask.Statement,
            Explanation = createdTask.Explanation
        };

        return new HttpOperationResult<TaskListItemDto>(dto, HttpStatusCode.Created);
    }

    public async Task<HttpOperationResult<TaskListItemDto>> UpdateTaskAsync(long userId, bool isAdmin, long taskId, UpdateTaskRequestDto request, CancellationToken cancellationToken)
    {
        
        var existingTask = await _taskRepository.GetTaskByIdAsync(taskId, cancellationToken);
        if (existingTask == null)
        {
            return new HttpOperationResult<TaskListItemDto>
            {
                Status = HttpStatusCode.NotFound,
                Error = $"Task {taskId} not found"
            };
        }

        
        if (!isAdmin)
        {
            var isUsedInTeacherTests = await _taskRepository.IsTaskUsedInTeacherTestsAsync(taskId, userId, cancellationToken);
            if (!isUsedInTeacherTests)
            {
                return new HttpOperationResult<TaskListItemDto>
                {
                    Status = HttpStatusCode.Forbidden,
                    Error = "You can only edit tasks that are used in your tests"
                };
            }
        }

        
        if (request.Difficulty.HasValue && (request.Difficulty.Value < 1 || request.Difficulty.Value > 5))
        {
            return new HttpOperationResult<TaskListItemDto>
            {
                Status = HttpStatusCode.BadRequest,
                Error = "Difficulty must be between 1 and 5"
            };
        }

        
        if (!string.IsNullOrWhiteSpace(request.TaskType))
        {
            var validTaskTypes = new[] { "numeric", "text" };
            if (!validTaskTypes.Contains(request.TaskType.ToLower()))
            {
                return new HttpOperationResult<TaskListItemDto>
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = $"Task type must be one of: {string.Join(", ", validTaskTypes)}"
                };
            }
        }

        
        var updatedTask = new TaskItem
        {
            Id = existingTask.Id,
            SubjectId = existingTask.SubjectId,
            TopicId = request.TopicId ?? existingTask.TopicId,
            TaskType = !string.IsNullOrWhiteSpace(request.TaskType) ? request.TaskType.ToLower() : existingTask.TaskType,
            Difficulty = request.Difficulty ?? existingTask.Difficulty,
            Statement = !string.IsNullOrWhiteSpace(request.Statement) ? request.Statement : existingTask.Statement,
            CorrectAnswer = !string.IsNullOrWhiteSpace(request.CorrectAnswer) ? request.CorrectAnswer : existingTask.CorrectAnswer,
            Explanation = request.Explanation ?? existingTask.Explanation,
            IsActive = request.IsActive ?? existingTask.IsActive
        };

        await _taskRepository.UpdateAsync(updatedTask, cancellationToken);

        
        var tasks = await _taskRepository.GetTasksBySubjectAsync(existingTask.SubjectId, taskId.ToString(), cancellationToken);
        var updatedTaskItem = tasks.FirstOrDefault(t => t.Id == taskId);

        if (updatedTaskItem == null)
        {
            return new HttpOperationResult<TaskListItemDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to retrieve updated task"
            };
        }

        var dto = new TaskListItemDto
        {
            Id = updatedTaskItem.Id,
            SubjectId = updatedTaskItem.SubjectId,
            TopicId = updatedTaskItem.TopicId,
            TopicName = updatedTaskItem.Topic?.TopicName,
            TopicCode = updatedTaskItem.Topic?.TopicCode,
            TaskType = updatedTaskItem.TaskType,
            Difficulty = updatedTaskItem.Difficulty,
            Statement = updatedTaskItem.Statement,
            Explanation = updatedTaskItem.Explanation
        };

        return new HttpOperationResult<TaskListItemDto>(dto, HttpStatusCode.OK);
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<TopicListItemDto>>> GetTopicsBySubjectAsync(long subjectId, CancellationToken cancellationToken)
    {
        var query = _sqlQueryProvider.GetQuery("StatisticsUseCases/Queries/GetAllTopicsBySubject");
        var parameters = new[]
        {
            new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = subjectId }
        };

        var topics = await _sqlExecutor.QueryAsync(
            query,
            reader => new TopicListItemDto
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                TopicName = reader.GetString(reader.GetOrdinal("topic_name")),
                TopicCode = reader.IsDBNull(reader.GetOrdinal("topic_code")) ? null : reader.GetString(reader.GetOrdinal("topic_code"))
            },
            parameters,
            cancellationToken);

        return new HttpOperationResult<IReadOnlyCollection<TopicListItemDto>>(topics.ToArray(), HttpStatusCode.OK);
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
