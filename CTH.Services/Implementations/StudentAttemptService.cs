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
        
        // Проверяем доступность теста: публичный/государственный или MIXED тест автора
        var isAccessible = test != null 
            && test.IsPublished 
            && (test.IsPublic 
                || test.IsStateArchive 
                || (test.TestKind == "MIXED" && test.AuthorId == userId));
        
        if (!isAccessible)
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
        _logger.LogInformation("[SubmitAnswer] Starting answer submission. UserId={UserId}, AttemptId={AttemptId}, TaskId={TaskId}, GivenAnswerJson={GivenAnswerJson}",
            userId, attemptId, request.TaskId, request.GivenAnswer);
        
        _logger.LogInformation("[SubmitAnswer] Step 1: Fetching attempt {AttemptId} for user {UserId}", attemptId, userId);
        var attempt = await _attemptRepository.GetByIdAsync(attemptId, userId, cancellationToken);
        if (attempt == null)
        {
            _logger.LogWarning("[SubmitAnswer] Attempt {AttemptId} not found for user {UserId}", attemptId, userId);
            return new HttpOperationResult(HttpStatusCode.NotFound)
            {
                Error = $"Attempt {attemptId} not found"
            };
        }
        _logger.LogInformation("[SubmitAnswer] Step 1 completed. Attempt found: Status={Status}, TestId={TestId}", attempt.Status, attempt.TestId);

        if (!string.Equals(attempt.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[SubmitAnswer] Attempt {AttemptId} is not in progress. Current status: {Status}", attemptId, attempt.Status);
            return new HttpOperationResult(HttpStatusCode.BadRequest)
            {
                Error = "Attempt already completed"
            };
        }

        _logger.LogInformation("[SubmitAnswer] Step 2: Fetching test tasks for test {TestId}", attempt.TestId);
        var testTasks = await _testRepository.GetTestTasksAsync(attempt.TestId, cancellationToken);
        _logger.LogInformation("[SubmitAnswer] Step 2 completed. Found {TaskCount} tasks in test", testTasks.Count);
        
        var testTask = testTasks.FirstOrDefault(t => t.TaskId == request.TaskId);
        if (testTask?.Task == null)
        {
            _logger.LogWarning("[SubmitAnswer] Task {TaskId} does not belong to test {TestId} (attempt {AttemptId})", request.TaskId, attempt.TestId, attemptId);
            return new HttpOperationResult(HttpStatusCode.BadRequest)
            {
                Error = $"Task {request.TaskId} does not belong to test {attempt.TestId}"
            };
        }
        _logger.LogInformation("[SubmitAnswer] Step 2 completed. Task found: TaskId={TaskId}, CorrectAnswerJson={CorrectAnswerJson}",
            testTask.TaskId, testTask.Task.CorrectAnswer);

        _logger.LogInformation("[SubmitAnswer] Step 3: Evaluating answer correctness");
        var isCorrect = EvaluateAnswer(attemptId, request.TaskId, request.GivenAnswer, testTask.Task.CorrectAnswer);
        _logger.LogInformation("[SubmitAnswer] Step 3 completed. Evaluation result: IsCorrect={IsCorrect}", isCorrect);

        _logger.LogInformation("[SubmitAnswer] Step 4: Storing answer in database");
        await _userAnswerRepository.UpsertAsync(
            attemptId,
            request.TaskId,
            request.GivenAnswer,
            isCorrect,
            request.TimeSpentSec,
            cancellationToken);
        _logger.LogInformation("[SubmitAnswer] Step 4 completed. Answer stored successfully. AttemptId={AttemptId}, TaskId={TaskId}, IsCorrect={IsCorrect}",
            attemptId, request.TaskId, isCorrect);

        _logger.LogInformation("[SubmitAnswer] Answer submission completed successfully for attempt {AttemptId}, task {TaskId}", attemptId, request.TaskId);
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

    public async Task<HttpOperationResult> AbortAttemptAsync(long userId, long attemptId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} attempts to abort attempt {AttemptId}", userId, attemptId);
        var attempt = await _attemptRepository.GetByIdAsync(attemptId, userId, cancellationToken);
        if (attempt == null)
        {
            _logger.LogWarning("Attempt {AttemptId} not found for abort.", attemptId);
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
                Error = "Attempt is not in progress"
            };
        }

        var updated = await _attemptRepository.AbortAsync(attemptId, userId, cancellationToken);
        if (!updated)
        {
            _logger.LogWarning("Unable to abort attempt {AttemptId} for user {UserId}", attemptId, userId);
            return new HttpOperationResult(HttpStatusCode.BadRequest)
            {
                Error = "Unable to abort attempt"
            };
        }

        _logger.LogInformation("Attempt {AttemptId} aborted for user {UserId}", attemptId, userId);
        return new HttpOperationResult(HttpStatusCode.NoContent);
    }

    public async Task<HttpOperationResult> ResumeAttemptAsync(long userId, long attemptId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} attempts to resume attempt {AttemptId}", userId, attemptId);
        var attempt = await _attemptRepository.GetByIdAsync(attemptId, userId, cancellationToken);
        if (attempt == null)
        {
            _logger.LogWarning("Attempt {AttemptId} not found for resume.", attemptId);
            return new HttpOperationResult(HttpStatusCode.NotFound)
            {
                Error = $"Attempt {attemptId} not found"
            };
        }

        if (!string.Equals(attempt.Status, "aborted", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Attempt {AttemptId} is not aborted. Current status: {Status}", attemptId, attempt.Status);
            return new HttpOperationResult(HttpStatusCode.BadRequest)
            {
                Error = "Attempt is not aborted"
            };
        }

        var updated = await _attemptRepository.ResumeAsync(attemptId, userId, cancellationToken);
        if (!updated)
        {
            _logger.LogWarning("Unable to resume attempt {AttemptId} for user {UserId}", attemptId, userId);
            return new HttpOperationResult(HttpStatusCode.BadRequest)
            {
                Error = "Unable to resume attempt"
            };
        }

        _logger.LogInformation("Attempt {AttemptId} resumed for user {UserId}", attemptId, userId);
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
                GivenAnswer = a.GivenAnswer.RootElement.GetRawText(),
                IsCorrect = a.IsCorrect,
                TimeSpentSec = a.TimeSpentSec
            }).ToArray()
        };

        return new HttpOperationResult<AttemptDetailsDto>(dto, HttpStatusCode.OK);
    }

    public async Task<HttpOperationResult<AttemptDetailsWithTasksDto>> GetAttemptDetailsWithTasksAsync(long userId, long attemptId, CancellationToken cancellationToken)
    {
        var attempt = await _attemptRepository.GetByIdAsync(attemptId, userId, cancellationToken);
        if (attempt == null)
        {
            return new HttpOperationResult<AttemptDetailsWithTasksDto>
            {
                Status = HttpStatusCode.NotFound,
                Error = $"Attempt {attemptId} not found"
            };
        }

        // Получаем задания теста с правильными ответами
        var testTasks = await _testRepository.GetTestTasksAsync(attempt.TestId, cancellationToken);
        
        // Получаем ответы пользователя
        var userAnswers = await _userAnswerRepository.GetByAttemptIdAsync(attemptId, cancellationToken);
        var answersDict = userAnswers.ToDictionary(a => a.TaskId, a => a);

        // Объединяем задания с ответами пользователя
        var tasksWithAnswers = testTasks
            .OrderBy(t => t.Position)
            .Select(t => new AttemptTaskDetailsDto
            {
                TaskId = t.TaskId,
                Position = t.Position,
                TaskType = t.Task?.TaskType ?? string.Empty,
                Statement = t.Task?.Statement ?? string.Empty,
                Difficulty = t.Task?.Difficulty ?? default,
                Explanation = t.Task?.Explanation,
                CorrectAnswer = t.Task?.CorrectAnswer,
                GivenAnswer = answersDict.TryGetValue(t.TaskId, out var answer) 
                    ? answer.GivenAnswer.RootElement.GetRawText() 
                    : null,
                IsCorrect = answersDict.TryGetValue(t.TaskId, out var answer2) 
                    ? answer2.IsCorrect 
                    : null,
                TimeSpentSec = answersDict.TryGetValue(t.TaskId, out var answer3) 
                    ? answer3.TimeSpentSec 
                    : null
            })
            .ToArray();

        var dto = new AttemptDetailsWithTasksDto
        {
            Id = attempt.Id,
            TestId = attempt.TestId,
            TestTitle = attempt.Test?.Title ?? "Unknown",
            Status = attempt.Status,
            StartedAt = attempt.StartedAt,
            FinishedAt = attempt.FinishedAt,
            RawScore = attempt.RawScore,
            ScaledScore = attempt.ScaledScore,
            DurationSec = attempt.DurationSec,
            Tasks = tasksWithAnswers
        };

        return new HttpOperationResult<AttemptDetailsWithTasksDto>(dto, HttpStatusCode.OK);
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<AttemptListItemDto>>> GetInProgressAttemptsAsync(long userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} requested in-progress attempts list", userId);
        var attempts = await _attemptRepository.GetInProgressAttemptsByUserAsync(userId, cancellationToken);
        
        var dtos = attempts.Select(a => new AttemptListItemDto
        {
            Id = a.Id,
            TestId = a.TestId,
            TestTitle = a.Test?.Title ?? "Unknown",
            SubjectId = a.Test?.SubjectId ?? 0,
            SubjectName = a.Test?.Subject?.SubjectName ?? "Unknown",
            StartedAt = a.StartedAt,
            FinishedAt = a.FinishedAt,
            Status = a.Status,
            RawScore = a.RawScore
        }).ToArray();

        _logger.LogInformation("Found {Count} in-progress attempts for user {UserId}", dtos.Length, userId);
        return new HttpOperationResult<IReadOnlyCollection<AttemptListItemDto>>(dtos, HttpStatusCode.OK);
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<AttemptListItemDto>>> GetAttemptsAsync(long userId, string? status, int limit, int offset, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} requested attempts list. Status={Status}, Limit={Limit}, Offset={Offset}", userId, status ?? "all", limit, offset);
        var attempts = await _attemptRepository.GetAttemptsByUserAsync(userId, status, limit, offset, cancellationToken);
        
        var dtos = attempts.Select(a => new AttemptListItemDto
        {
            Id = a.Id,
            TestId = a.TestId,
            TestTitle = a.Test?.Title ?? "Unknown",
            SubjectId = a.Test?.SubjectId ?? 0,
            SubjectName = a.Test?.Subject?.SubjectName ?? "Unknown",
            StartedAt = a.StartedAt,
            FinishedAt = a.FinishedAt,
            Status = a.Status,
            RawScore = a.RawScore
        }).ToArray();

        _logger.LogInformation("Found {Count} attempts for user {UserId}", dtos.Length, userId);
        return new HttpOperationResult<IReadOnlyCollection<AttemptListItemDto>>(dtos, HttpStatusCode.OK);
    }

    private bool EvaluateAnswer(long attemptId, long taskId, string givenAnswerJson, string correctAnswerJson)
    {
        _logger.LogInformation("[EvaluateAnswer] Starting evaluation. AttemptId={AttemptId}, TaskId={TaskId}, GivenAnswerJson={GivenAnswerJson}, CorrectAnswerJson={CorrectAnswerJson}",
            attemptId, taskId, givenAnswerJson, correctAnswerJson);

        _logger.LogInformation("[EvaluateAnswer] Step 1: Extracting value from given answer JSON");
        var givenValue = ExtractAnswerValue(givenAnswerJson);
        _logger.LogInformation("[EvaluateAnswer] Step 1 completed. Extracted given value: '{GivenValue}' (null={IsNull}, empty={IsEmpty})",
            givenValue ?? "<null>", givenValue == null, string.IsNullOrWhiteSpace(givenValue));

        _logger.LogInformation("[EvaluateAnswer] Step 2: Extracting value from correct answer JSON");
        var expectedValue = ExtractAnswerValue(correctAnswerJson);
        _logger.LogInformation("[EvaluateAnswer] Step 2 completed. Extracted expected value: '{ExpectedValue}' (null={IsNull}, empty={IsEmpty})",
            expectedValue ?? "<null>", expectedValue == null, string.IsNullOrWhiteSpace(expectedValue));

        if (string.IsNullOrWhiteSpace(givenValue) || string.IsNullOrWhiteSpace(expectedValue))
        {
            _logger.LogWarning("[EvaluateAnswer] Evaluation skipped due to missing values. AttemptId={AttemptId}, TaskId={TaskId}, GivenValue='{GivenValue}', ExpectedValue='{ExpectedValue}'",
                attemptId, taskId, givenValue ?? "<null>", expectedValue ?? "<null>");
            return false;
        }

        _logger.LogInformation("[EvaluateAnswer] Step 3: Normalizing values (trimming whitespace)");
        var normalizedGiven = givenValue.Trim();
        var normalizedExpected = expectedValue.Trim();
        _logger.LogInformation("[EvaluateAnswer] Step 3 completed. Normalized given: '{NormalizedGiven}' (length={Length}), Normalized expected: '{NormalizedExpected}' (length={Length})",
            normalizedGiven, normalizedGiven.Length, normalizedExpected, normalizedExpected.Length);

        _logger.LogInformation("[EvaluateAnswer] Step 4: Comparing values (case-insensitive comparison)");
        var result = string.Equals(normalizedGiven, normalizedExpected, StringComparison.OrdinalIgnoreCase);
        _logger.LogInformation(
            "[EvaluateAnswer] Step 4 completed. Comparison result: {Result}. Given '{Given}' vs Expected '{Expected}'",
            result,
            normalizedGiven,
            normalizedExpected);

        _logger.LogInformation("[EvaluateAnswer] Evaluation completed. AttemptId={AttemptId}, TaskId={TaskId}, Result={Result}",
            attemptId, taskId, result);
        return result;
    }

    private string? ExtractAnswerValue(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogDebug("[ExtractAnswerValue] Input JSON is null or whitespace");
            return null;
        }

        try
        {
            _logger.LogDebug("[ExtractAnswerValue] Parsing JSON: {Json}", json);
            using var document = JsonDocument.Parse(json);
            _logger.LogDebug("[ExtractAnswerValue] JSON parsed. Root element kind: {ValueKind}", document.RootElement.ValueKind);

            // Приоритет 1: Если это JSON-строка (новый формат)
            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                var result = document.RootElement.GetString();
                _logger.LogDebug("[ExtractAnswerValue] Root element is string. Extracted: '{Result}'", result ?? "<null>");
                return result;
            }

            // Приоритет 2: Если это объект с полем "value" (старый формат для обратной совместимости)
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("value", out var property))
            {
                _logger.LogDebug("[ExtractAnswerValue] Found 'value' property. Property kind: {ValueKind}", property.ValueKind);
                var result = property.ValueKind == JsonValueKind.String
                    ? property.GetString()
                    : property.GetRawText().Trim('"');
                _logger.LogDebug("[ExtractAnswerValue] Extracted value from 'value' property: '{Result}'", result ?? "<null>");
                return result;
            }

            _logger.LogWarning("[ExtractAnswerValue] JSON structure not recognized. Root element kind: {ValueKind}, JSON: {Json}",
                document.RootElement.ValueKind, json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[ExtractAnswerValue] Failed to parse answer json: {Json}", json);
            // Если не удалось распарсить, пытаемся убрать кавычки если это закавыченная строка
            var trimmed = json.Trim();
            if (trimmed.StartsWith("\"", StringComparison.Ordinal) && 
                trimmed.EndsWith("\"", StringComparison.Ordinal) && 
                trimmed.Length > 1)
            {
                return trimmed.Substring(1, trimmed.Length - 2);
            }
            return json;
        }

        _logger.LogWarning("[ExtractAnswerValue] Could not extract value from JSON: {Json}", json);
        return null;
    }
}
