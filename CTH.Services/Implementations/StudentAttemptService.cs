using CTH.Database.Repositories.Interfaces;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Attempts;
using Microsoft.Extensions.Logging;
using PropTechPeople.Services.Models.ResultApiModels;
using System.Net;
using System.Linq;
using System.Text.Json;

namespace CTH.Services.Implementations;

public class StudentAttemptService : IStudentAttemptService
{
    private readonly IAttemptRepository _attemptRepository;
    private readonly IUserAnswerRepository _userAnswerRepository;
    private readonly ITestRepository _testRepository;
    private readonly ILogger<StudentAttemptService> _logger;

    public StudentAttemptService(
        IAttemptRepository attemptRepository,
        IUserAnswerRepository userAnswerRepository,
        ITestRepository testRepository,
        ILogger<StudentAttemptService> logger)
    {
        _attemptRepository = attemptRepository;
        _userAnswerRepository = userAnswerRepository;
        _testRepository = testRepository;
        _logger = logger;
    }

    public async Task<HttpOperationResult<StartAttemptResponseDto>> StartAttemptAsync(long userId, long testId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} requested attempt start for test {TestId}", userId, testId);
        var test = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
        if (test == null || !test.IsPublished || (!test.IsPublic && !test.IsStateArchive))
        {
            _logger.LogWarning("Test {TestId} is not accessible for user {UserId}", testId, userId);
            return new HttpOperationResult<StartAttemptResponseDto>
            {
                Status = HttpStatusCode.NotFound,
                Error = $"Test {testId} not found"
            };
        }

        var attempt = await _attemptRepository.CreateAsync(userId, testId, null, cancellationToken);
        _logger.LogInformation("Attempt {AttemptId} created for user {UserId}", attempt.Id, userId);
        var result = new StartAttemptResponseDto
        {
            AttemptId = attempt.Id,
            StartedAt = attempt.StartedAt,
            Status = attempt.Status
        };

        return new HttpOperationResult<StartAttemptResponseDto>(result, HttpStatusCode.OK);
    }

    public async Task<HttpOperationResult> SubmitAnswerAsync(long userId, long attemptId, SubmitAnswerRequestDto request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} submits answer for attempt {AttemptId}, task {TaskId}", userId, attemptId, request.TaskId);
        var attempt = await _attemptRepository.GetByIdAsync(attemptId, userId, cancellationToken);
        if (attempt == null)
        {
            _logger.LogWarning("Attempt {AttemptId} not found for user {UserId}", attemptId, userId);
            return new HttpOperationResult(HttpStatusCode.NotFound)
            {
                Error = $"Attempt {attemptId} not found"
            };
        }

        if (!string.Equals(attempt.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Attempt {AttemptId} is not in progress. Current status: {Status}", attemptId, attempt.Status);
            return new HttpOperationResult(HttpStatusCode.BadRequest)
            {
                Error = "Attempt already completed"
            };
        }

        var testTasks = await _testRepository.GetTestTasksAsync(attempt.TestId, cancellationToken);
        var testTask = testTasks.FirstOrDefault(t => t.TaskId == request.TaskId);
        if (testTask?.Task == null)
        {
            _logger.LogWarning("Task {TaskId} does not belong to test {TestId} (attempt {AttemptId})", request.TaskId, attempt.TestId, attemptId);
            return new HttpOperationResult(HttpStatusCode.BadRequest)
            {
                Error = $"Task {request.TaskId} does not belong to test {attempt.TestId}"
            };
        }

        var isCorrect = EvaluateAnswer(attemptId, request.TaskId, request.GivenAnswer, testTask.Task.CorrectAnswer);

        await _userAnswerRepository.UpsertAsync(
            attemptId,
            request.TaskId,
            request.GivenAnswer,
            isCorrect,
            request.TimeSpentSec,
            cancellationToken);
        _logger.LogInformation("Stored answer for attempt {AttemptId}, task {TaskId}. Result: {Result}", attemptId, request.TaskId, isCorrect);

        return new HttpOperationResult(HttpStatusCode.NoContent);
    }

    public async Task<HttpOperationResult> CompleteAttemptAsync(long userId, long attemptId, CompleteAttemptRequestDto request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} attempts to complete attempt {AttemptId}", userId, attemptId);
        var attempt = await _attemptRepository.GetByIdAsync(attemptId, userId, cancellationToken);
        if (attempt == null)
        {
            _logger.LogWarning("Attempt {AttemptId} not found for completion.", attemptId);
            return new HttpOperationResult(HttpStatusCode.NotFound)
            {
                Error = $"Attempt {attemptId} not found"
            };
        }

        var updated = await _attemptRepository.CompleteAsync(
            attemptId,
            userId,
            request.RawScore,
            request.ScaledScore,
            request.DurationSec,
            cancellationToken);

        if (!updated)
        {
            _logger.LogWarning("Unable to complete attempt {AttemptId} for user {UserId}", attemptId, userId);
            return new HttpOperationResult(HttpStatusCode.BadRequest)
            {
                Error = "Unable to complete attempt"
            };
        }

        _logger.LogInformation("Attempt {AttemptId} completed for user {UserId}", attemptId, userId);
        return new HttpOperationResult(HttpStatusCode.NoContent);
    }

    public async Task<HttpOperationResult<AttemptDetailsDto>> GetAttemptAsync(long userId, long attemptId, CancellationToken cancellationToken)
    {
        var attempt = await _attemptRepository.GetByIdAsync(attemptId, userId, cancellationToken);
        if (attempt == null)
        {
            return new HttpOperationResult<AttemptDetailsDto>
            {
                Status = HttpStatusCode.NotFound,
                Error = $"Attempt {attemptId} not found"
            };
        }

        var answers = await _userAnswerRepository.GetByAttemptIdAsync(attemptId, cancellationToken);

        var dto = new AttemptDetailsDto
        {
            Id = attempt.Id,
            TestId = attempt.TestId,
            Status = attempt.Status,
            StartedAt = attempt.StartedAt,
            FinishedAt = attempt.FinishedAt,
            RawScore = attempt.RawScore,
            ScaledScore = attempt.ScaledScore,
            DurationSec = attempt.DurationSec,
            Answers = answers.Select(a => new AttemptAnswerDto
            {
                TaskId = a.TaskId,
                GivenAnswer = a.GivenAnswer.ToString(),
                IsCorrect = a.IsCorrect,
                TimeSpentSec = a.TimeSpentSec
            }).ToArray()
        };

        return new HttpOperationResult<AttemptDetailsDto>(dto, HttpStatusCode.OK);
    }

    private bool EvaluateAnswer(long attemptId, long taskId, string givenAnswerJson, string correctAnswerJson)
    {
        var givenValue = ExtractAnswerValue(givenAnswerJson);
        var expectedValue = ExtractAnswerValue(correctAnswerJson);

        if (string.IsNullOrWhiteSpace(givenValue) || string.IsNullOrWhiteSpace(expectedValue))
        {
            _logger.LogInformation("Evaluation skipped due to missing values. Attempt {AttemptId}, task {TaskId}", attemptId, taskId);
            return false;
        }

        var normalizedGiven = givenValue.Trim();
        var normalizedExpected = expectedValue.Trim();

        var result = string.Equals(normalizedGiven, normalizedExpected, StringComparison.OrdinalIgnoreCase);
        _logger.LogInformation(
            "Evaluated answer for attempt {AttemptId}, task {TaskId}. Given '{Given}' vs Expected '{Expected}'. Result={Result}",
            attemptId,
            taskId,
            normalizedGiven,
            normalizedExpected,
            result);

        return result;
    }

    private string? ExtractAnswerValue(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("value", out var property))
            {
                return property.ValueKind == JsonValueKind.String
                    ? property.GetString()
                    : property.ToString();
            }

            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                return document.RootElement.GetString();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse answer json: {Json}", json);
            return json;
        }

        return null;
    }
}
