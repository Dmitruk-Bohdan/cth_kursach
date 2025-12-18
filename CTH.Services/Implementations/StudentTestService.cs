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
    private readonly ITaskRepository _taskRepository;
    private readonly IUserStatsRepository _userStatsRepository;

    public StudentTestService(
        ITestRepository testRepository,
        ITaskRepository taskRepository,
        IUserStatsRepository userStatsRepository)
    {
        _testRepository = testRepository;
        _taskRepository = taskRepository;
        _userStatsRepository = userStatsRepository;
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

    public async Task<HttpOperationResult<TestDetailsDto>> GetTestDetailsAsync(long testId, long userId, CancellationToken cancellationToken)
    {
        var test = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
        if (test == null)
        {
            return new HttpOperationResult<TestDetailsDto>
            {
                Status = HttpStatusCode.NotFound,
                Error = $"Test {testId} not found"
            };
        }

        
        if (!IsTestAccessible(test, userId))
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

    public async Task<HttpOperationResult<TestDetailsDto>> GenerateMixedTestAsync(long userId, GenerateMixedTestRequestDto request, CancellationToken cancellationToken)
    {
        
        var topicsToUse = request.Topics.ToList();
        var topicNames = new Dictionary<long, string>(); 
        
        
        if (topicsToUse.Count == 0)
        {
            var stats = await _userStatsRepository.GetSubjectStatisticsWithTopicsAsync(userId, request.SubjectId, cancellationToken);
            var topErrorTopics = stats
                .Where(s => s.TopicId.HasValue && s.AttemptsTotal > 0)
                .Select(s => new
                {
                    Stats = s,
                    ErrorsCount = s.AttemptsTotal - s.CorrectTotal,
                    AccuracyPercentage = s.AttemptsTotal > 0 ? (decimal?)s.CorrectTotal / s.AttemptsTotal * 100 : null
                })
                .Where(x => x.ErrorsCount > 0)
                .OrderByDescending(x => x.ErrorsCount)
                .ThenBy(x => x.AccuracyPercentage ?? 0)
                .Take(3)
                .ToList();

            if (topErrorTopics.Count == 0)
            {
                return new HttpOperationResult<TestDetailsDto>
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = "No topics with errors found. Please specify topics manually."
                };
            }

            topicsToUse = topErrorTopics.Select(x => new TopicSelectionDto
            {
                TopicId = x.Stats.TopicId!.Value,
                TaskCount = 10,
                DesiredDifficulty = 3 
            }).ToList();

            
            foreach (var topErrorTopic in topErrorTopics)
            {
                if (topErrorTopic.Stats.TopicId.HasValue && topErrorTopic.Stats.Topic != null)
                {
                    topicNames[topErrorTopic.Stats.TopicId.Value] = topErrorTopic.Stats.Topic.TopicName;
                }
            }
        }

        
        var allTestTasks = new List<TestTask>();
        var position = 1;

        foreach (var topicSelection in topicsToUse)
        {
            var tasks = await SelectTasksForTopicAsync(
                topicSelection.TopicId,
                topicSelection.TaskCount,
                topicSelection.DesiredDifficulty,
                cancellationToken);

            
            if (!topicNames.ContainsKey(topicSelection.TopicId) && tasks.Count > 0)
            {
                var firstTask = tasks.FirstOrDefault();
                if (firstTask?.Topic != null)
                {
                    topicNames[topicSelection.TopicId] = firstTask.Topic.TopicName;
                }
            }

            foreach (var task in tasks)
            {
                allTestTasks.Add(new TestTask
                {
                    TestId = 0, 
                    TaskId = task.Id,
                    Position = position++,
                    Weight = null
                });
            }
        }

        if (allTestTasks.Count == 0)
        {
            return new HttpOperationResult<TestDetailsDto>
            {
                Status = HttpStatusCode.BadRequest,
                Error = "No tasks found for selected topics and difficulty levels."
            };
        }

        
        string testTitle;
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            testTitle = request.Title;
        }
        else
        {
            
            var firstThreeTopics = topicsToUse.Take(3).ToList();
            var topicNameParts = new List<string>();
            
            foreach (var topicSelection in firstThreeTopics)
            {
                if (topicNames.TryGetValue(topicSelection.TopicId, out var topicName))
                {
                    topicNameParts.Add(topicName);
                }
            }

            var topicsPart = topicNameParts.Count > 0 
                ? string.Join(", ", topicNameParts)
                : "Mixed Test";
            
            var dateTimePart = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm");
            testTitle = $"{topicsPart} - {dateTimePart}";
        }

        
        if (testTitle.Length > 200)
        {
            testTitle = testTitle.Substring(0, 197) + "...";
        }

        
        var test = new Test
        {
            SubjectId = request.SubjectId,
            TestKind = "MIXED",
            Title = testTitle,
            AuthorId = userId,
            TimeLimitSec = request.TimeLimitSec,
            AttemptsAllowed = null, 
            Mode = "training",
            IsPublished = true,
            IsPublic = false,
            IsStateArchive = false
        };

        var testId = await _testRepository.CreateAsync(test, cancellationToken);

        
        foreach (var task in allTestTasks)
        {
            task.TestId = testId;
        }

        
        await _testRepository.ReplaceTasksAsync(testId, allTestTasks, cancellationToken);

        
        var createdTest = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
        var testTasks = await _testRepository.GetTestTasksAsync(testId, cancellationToken);

        var dto = new TestDetailsDto
        {
            Id = testId,
            Title = createdTest?.Title ?? test.Title,
            TestKind = createdTest?.TestKind ?? test.TestKind,
            SubjectId = createdTest?.SubjectId ?? test.SubjectId,
            TimeLimitSec = createdTest?.TimeLimitSec ?? test.TimeLimitSec,
            AttemptsAllowed = createdTest?.AttemptsAllowed ?? test.AttemptsAllowed,
            IsPublic = createdTest?.IsPublic ?? test.IsPublic,
            IsStateArchive = createdTest?.IsStateArchive ?? test.IsStateArchive,
            Tasks = testTasks
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

    private async Task<IReadOnlyCollection<TaskItem>> SelectTasksForTopicAsync(
        long topicId,
        int taskCount,
        short desiredDifficulty,
        CancellationToken cancellationToken)
    {
        
        var difficulties = GetDifficultyDistribution(desiredDifficulty, taskCount);
        
        var allTasks = new List<TaskItem>();
        
        
        foreach (var (difficulty, count) in difficulties)
        {
            if (count > 0)
            {
                var tasks = await _taskRepository.GetTasksByTopicsAndDifficultyAsync(
                    new[] { topicId },
                    new[] { difficulty },
                    count,
                    cancellationToken);
                
                allTasks.AddRange(tasks.Take(count));
            }
        }

        
        if (allTasks.Count < taskCount)
        {
            var remaining = taskCount - allTasks.Count;
            var allAvailableTasks = await _taskRepository.GetTasksByTopicsAndDifficultyAsync(
                new[] { topicId },
                new short[] { 1, 2, 3, 4, 5 },
                remaining * 2, 
                cancellationToken);

            var usedTaskIds = allTasks.Select(t => t.Id).ToHashSet();
            var additionalTasks = allAvailableTasks
                .Where(t => !usedTaskIds.Contains(t.Id))
                .Take(remaining)
                .ToList();

            allTasks.AddRange(additionalTasks);
        }

        
        var random = new Random();
        return allTasks.OrderBy(_ => random.Next()).Take(taskCount).ToList();
    }

    private static IReadOnlyCollection<(short difficulty, int count)> GetDifficultyDistribution(short desiredDifficulty, int totalCount)
    {
        var result = new List<(short, int)>();

        if (desiredDifficulty == 1) 
        {
            result.Add((1, (int)(totalCount * 0.7))); 
            result.Add((2, (int)(totalCount * 0.2))); 
            result.Add((3, (int)(totalCount * 0.1))); 
        }
        else if (desiredDifficulty == 5) 
        {
            result.Add((5, (int)(totalCount * 0.7))); 
            result.Add((4, (int)(totalCount * 0.2))); 
            result.Add((3, (int)(totalCount * 0.1))); 
        }
        else
        {
            result.Add((desiredDifficulty, (int)(totalCount * 0.7))); 
            result.Add((desiredDifficulty > 1 ? (short)(desiredDifficulty - 1) : desiredDifficulty, (int)(totalCount * 0.2))); 
            result.Add((desiredDifficulty < 5 ? (short)(desiredDifficulty + 1) : desiredDifficulty, (int)(totalCount * 0.1))); 
        }

        
        var sum = result.Sum(r => r.Item2);
        if (sum < totalCount)
        {
            var diff = totalCount - sum;
            var mainIndex = result.FindIndex(r => r.Item1 == desiredDifficulty);
            if (mainIndex >= 0)
            {
                result[mainIndex] = (result[mainIndex].Item1, result[mainIndex].Item2 + diff);
            }
        }

        return result;
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<TestListItemDto>>> GetMyMixedTestsAsync(long userId, long subjectId, CancellationToken cancellationToken)
    {
        var tests = await _testRepository.GetMixedTestsByAuthorAndSubjectAsync(userId, subjectId, cancellationToken);
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

    public async Task<HttpOperationResult> DeleteMixedTestAsync(long userId, long testId, CancellationToken cancellationToken)
    {
        var test = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
        
        if (test == null)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.NotFound,
                Error = $"Test {testId} not found"
            };
        }

        if (test.AuthorId != userId)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.Forbidden,
                Error = "You can only delete your own mixed tests"
            };
        }

        if (test.TestKind != "MIXED")
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.BadRequest,
                Error = "Only MIXED tests can be deleted through this endpoint"
            };
        }

        await _testRepository.DeleteAsync(testId, cancellationToken);
        return new HttpOperationResult(HttpStatusCode.OK);
    }

    private static bool IsTestAccessible(Test test, long? userId = null)
    {
        if (!test.IsPublished)
        {
            return false;
        }

        
        if (test.IsPublic || test.IsStateArchive)
        {
            return true;
        }

        
        if (test.TestKind == "MIXED" && userId.HasValue && test.AuthorId == userId.Value)
        {
            return true;
        }

        return false;
    }
}
