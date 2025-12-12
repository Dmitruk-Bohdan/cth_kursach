using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CTH.MobileClient;

public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private string? _accessToken;

    public ApiClient(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute)
        };
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

    public async Task<Result<LoginResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/auth/register", request, _jsonOptions, cancellationToken);
        return await HandleLoginResponse(response, cancellationToken);
    }

    public async Task<Result<LoginResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/auth/authorize", new AuthorizeRequest
        {
            Email = email,
            Password = password
        }, _jsonOptions, cancellationToken);

        return await HandleLoginResponse(response, cancellationToken);
    }

    private async Task<Result<LoginResponse>> HandleLoginResponse(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result<LoginResponse>.Fail(error);
        }

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions, cancellationToken);
        if (login == null)
        {
            return Result<LoginResponse>.Fail("Failed to parse login response.");
        }

        _accessToken = login.AccessToken;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        return Result<LoginResponse>.Ok(login);
    }

    public async Task<Result<IReadOnlyCollection<TestListItem>>> GetPublishedTestsAsync(long? subjectId, CancellationToken cancellationToken)
    {
        var uri = subjectId.HasValue ? $"/student/tests?subjectId={subjectId}" : "/student/tests";
        var response = await _httpClient.GetAsync(uri, cancellationToken);
        var wrapped = await ReadResponseModelAsync<List<TestListItem>>(response, cancellationToken);
        return wrapped.Success
            ? Result<IReadOnlyCollection<TestListItem>>.Ok(wrapped.Value!)
            : Result<IReadOnlyCollection<TestListItem>>.Fail(wrapped.Error ?? "Не удалось получить список тестов.");
    }

    public async Task<Result<TestDetails>> GetTestDetailsAsync(long testId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/student/tests/{testId}", cancellationToken);
        return await ReadResponseModelAsync<TestDetails>(response, cancellationToken);
    }

    public async Task<Result<StartAttemptResponse>> StartAttemptAsync(long testId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync($"/student/tests/{testId}/attempts", null, cancellationToken);
        return await ReadResponseModelAsync<StartAttemptResponse>(response, cancellationToken);
    }

    public async Task<Result> SubmitAnswerAsync(long attemptId, SubmitAnswerRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync($"/student/attempts/{attemptId}/answers", request, _jsonOptions, cancellationToken);
        return await ToResult(response, cancellationToken);
    }

    public async Task<Result> CompleteAttemptAsync(long attemptId, CompleteAttemptRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync($"/student/attempts/{attemptId}/complete", request, _jsonOptions, cancellationToken);
        return await ToResult(response, cancellationToken);
    }

    public async Task<Result> AbortAttemptAsync(long attemptId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync($"/student/attempts/{attemptId}/abort", null, cancellationToken);
        return await ToResult(response, cancellationToken);
    }

    public async Task<Result> ResumeAttemptAsync(long attemptId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync($"/student/attempts/{attemptId}/resume", null, cancellationToken);
        return await ToResult(response, cancellationToken);
    }

    public async Task<Result<IReadOnlyCollection<AttemptListItem>>> GetAttemptsAsync(string? status, int limit, int offset, CancellationToken cancellationToken)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(status))
        {
            queryParams.Add($"status={Uri.EscapeDataString(status)}");
        }
        queryParams.Add($"limit={limit}");
        queryParams.Add($"offset={offset}");
        
        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var response = await _httpClient.GetAsync($"/student/attempts{queryString}", cancellationToken);
        var wrapped = await ReadResponseModelAsync<List<AttemptListItem>>(response, cancellationToken);
        return wrapped.Success
            ? Result<IReadOnlyCollection<AttemptListItem>>.Ok(wrapped.Value!)
            : Result<IReadOnlyCollection<AttemptListItem>>.Fail(wrapped.Error ?? "Не удалось получить список попыток.");
    }

    public async Task<Result> LogoutAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync("/auth/logout", null, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _accessToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return Result.Ok();
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result.Fail(string.IsNullOrWhiteSpace(error) ? "Logout failed." : error);
    }

    public async Task<Result<AttemptDetails>> GetAttemptAsync(long attemptId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/student/attempts/{attemptId}", cancellationToken);
        return await ReadResponseModelAsync<AttemptDetails>(response, cancellationToken);
    }

    public async Task<Result<IReadOnlyCollection<AttemptListItem>>> GetInProgressAttemptsAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("/student/attempts/in-progress", cancellationToken);
        var wrapped = await ReadResponseModelAsync<List<AttemptListItem>>(response, cancellationToken);
        return wrapped.Success
            ? Result<IReadOnlyCollection<AttemptListItem>>.Ok(wrapped.Value!)
            : Result<IReadOnlyCollection<AttemptListItem>>.Fail(wrapped.Error ?? "Не удалось получить список попыток.");
    }

    public async Task<Result<IReadOnlyCollection<SubjectListItem>>> GetAllSubjectsAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("/student/statistics/subjects", cancellationToken);
        var wrapped = await ReadResponseModelAsync<List<SubjectListItem>>(response, cancellationToken);
        return wrapped.Success
            ? Result<IReadOnlyCollection<SubjectListItem>>.Ok(wrapped.Value!)
            : Result<IReadOnlyCollection<SubjectListItem>>.Fail(wrapped.Error ?? "Failed to get subjects.");
    }

    public async Task<Result<SubjectStatistics>> GetSubjectStatisticsAsync(long subjectId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/student/statistics/subject/{subjectId}", cancellationToken);
        var wrapped = await ReadResponseModelAsync<SubjectStatistics>(response, cancellationToken);
        return wrapped.Success
            ? Result<SubjectStatistics>.Ok(wrapped.Value!)
            : Result<SubjectStatistics>.Fail(wrapped.Error ?? "Failed to get statistics.");
    }

    private static async Task<Result> ToResult(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return Result.Ok();
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result.Fail(error);
    }

    private async Task<Result<T>> ReadResponseModelAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorPayload = await response.Content.ReadAsStringAsync(cancellationToken);
            var message = string.IsNullOrWhiteSpace(errorPayload)
                ? $"Запрос завершился с кодом {(int)response.StatusCode}."
                : errorPayload;
            return Result<T>.Fail(message);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return Result<T>.Ok(default!);
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return Result<T>.Fail("Сервер вернул пустой ответ.");
        }

        try
        {
            var parseResult = TryParseResponsePayload<T>(payload);
            if (parseResult.Success)
            {
                return Result<T>.Ok(parseResult.Value!);
            }

            return Result<T>.Fail(parseResult.Error ?? "Не удалось обработать ответ сервера.");
        }
        catch (JsonException)
        {
            LogWarning("Ответ сервера имеет неожиданный формат.", payload);
            return Result<T>.Fail($"Не удалось разобрать ответ сервера. Ответ: {Truncate(payload)}");
        }
    }

    private Result<T> TryParseResponsePayload<T>(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("success", out var successProperty))
        {
            var success = successProperty.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => false
            };

            var message = root.TryGetProperty("message", out var messageProperty) && messageProperty.ValueKind == JsonValueKind.String
                ? messageProperty.GetString()
                : null;

            if (!success)
            {
                return Result<T>.Fail(string.IsNullOrWhiteSpace(message)
                    ? "Операция отклонена сервером."
                    : message);
            }

            if (!root.TryGetProperty("result", out var resultProperty) || resultProperty.ValueKind == JsonValueKind.Null)
            {
                return Result<T>.Fail("Сервер вернул пустой результат.");
            }

            var resultValue = JsonSerializer.Deserialize<T>(resultProperty.GetRawText(), _jsonOptions);
            return resultValue == null
                ? Result<T>.Fail("Не удалось преобразовать данные ответа.")
                : Result<T>.Ok(resultValue);
        }

        var direct = JsonSerializer.Deserialize<T>(payload, _jsonOptions);
        return direct == null
            ? Result<T>.Fail("Не удалось разобрать ответ сервера.")
            : Result<T>.Ok(direct);
    }

    private static void LogWarning(string message, string payload)
    {
        Console.WriteLine($"[ApiClient] {message}");
        Console.WriteLine($"[ApiClient] Ответ сервера (обрезано): {Truncate(payload)}");
    }

    private static string Truncate(string value, int maxLength = 500)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private sealed record AuthorizeRequest
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    public sealed record RegisterRequest
    {
        public string Email { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public int RoleTypeId { get; init; }
    }

    public sealed record LoginResponse(string AccessToken, string UserName);

    public sealed record TestListItem(long Id, string Title, string TestKind, long SubjectId, string SubjectName, int? TimeLimitSec, short? AttemptsAllowed, bool IsPublic, bool IsStateArchive);

    public sealed record TestDetails(long Id, string Title, string TestKind, long SubjectId, string SubjectName, int? TimeLimitSec, short? AttemptsAllowed, bool IsPublic, bool IsStateArchive, IReadOnlyCollection<TestTask> Tasks);

    public sealed record TestTask(long TaskId, int Position, string TaskType, string Statement, short Difficulty, string? Explanation);

    public sealed record StartAttemptResponse(long AttemptId, DateTimeOffset StartedAt, string Status);

    public sealed record SubmitAnswerRequest(long TaskId, string GivenAnswer, int? TimeSpentSec);

    public sealed record CompleteAttemptRequest(decimal? RawScore, decimal? ScaledScore, int? DurationSec);

    public sealed record AttemptDetails(long Id, long TestId, string Status, DateTimeOffset StartedAt, DateTimeOffset? FinishedAt, decimal? RawScore, decimal? ScaledScore, int? DurationSec, IReadOnlyCollection<AttemptAnswer> Answers);

    public sealed record AttemptAnswer(long TaskId, string GivenAnswer, bool IsCorrect, int? TimeSpentSec);

    public sealed record AttemptListItem(long Id, long TestId, string TestTitle, long SubjectId, string SubjectName, DateTimeOffset StartedAt, DateTimeOffset? FinishedAt, string Status, decimal? RawScore);

    public sealed record SubjectListItem(long Id, string SubjectCode, string SubjectName);

    public sealed record SubjectStatistics(decimal? OverallAccuracyPercentage, int OverallAttemptsTotal, int OverallCorrectTotal, IReadOnlyCollection<TopicStatistics> Top3ErrorTopics, IReadOnlyCollection<TopicStatistics> OtherTopics, IReadOnlyCollection<TopicStatistics> UnattemptedTopics);

    public sealed record TopicStatistics(long? TopicId, string TopicName, int AttemptsTotal, int CorrectTotal, int ErrorsCount, decimal? AccuracyPercentage, DateTimeOffset? LastAttemptAt);

    public class Result
    {
        public bool Success { get; }
        public string? Error { get; }

        protected Result(bool success, string? error = null)
        {
            Success = success;
            Error = error;
        }

        public static Result Ok() => new(true);
        public static Result Fail(string error) => new(false, error);
    }

    public sealed class Result<T> : Result
    {
        public T? Value { get; }

        private Result(bool success, T? value, string? error)
            : base(success, error)
        {
            Value = value;
        }

        public static Result<T> Ok(T value) => new(true, value, null);
        public new static Result<T> Fail(string error) => new(false, default, error);
    }
}
