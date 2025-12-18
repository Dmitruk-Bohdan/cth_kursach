using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;

namespace CTH.AdminPanelClient;

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

    public int? UserRoleId { get; private set; }

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

        if (string.IsNullOrWhiteSpace(login.AccessToken))
        {
            return Result<LoginResponse>.Fail("Access token is missing in response.");
        }

        _accessToken = login.AccessToken;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(_accessToken))
            {
                var token = handler.ReadJwtToken(_accessToken);
                var roleClaim = token.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role);
                if (roleClaim != null && int.TryParse(roleClaim.Value, out var roleId))
                {
                    UserRoleId = roleId;
                }
                else
                {
                    roleClaim = token.Claims.FirstOrDefault(c => c.Type == "Role");
                    if (roleClaim != null && int.TryParse(roleClaim.Value, out roleId))
                    {
                        UserRoleId = roleId;
                    }
                }
            }
        }
        catch (Exception)
        {
        }

        return Result<LoginResponse>.Ok(login);
    }

    public async Task<Result> LogoutAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync("/auth/logout", null, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _accessToken = null;
            UserRoleId = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return Result.Ok();
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result.Fail(string.IsNullOrWhiteSpace(error) ? "Logout failed." : error);
    }

    public async Task<Result<IReadOnlyCollection<UserListItemDto>>> GetAllUsersAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("/admin/users", cancellationToken);
        return await HandleResponse<IReadOnlyCollection<UserListItemDto>>(response, cancellationToken);
    }

    public async Task<Result<UserDetailsDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/admin/users", request, _jsonOptions, cancellationToken);
        return await HandleResponse<UserDetailsDto>(response, cancellationToken);
    }

    public async Task<Result<UserDetailsDto>> UpdateUserAsync(long userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsJsonAsync($"/admin/users/{userId}", request, _jsonOptions, cancellationToken);
        return await HandleResponse<UserDetailsDto>(response, cancellationToken);
    }

    public async Task<Result> BlockUserAsync(long userId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsync($"/admin/users/{userId}/block", null, cancellationToken);
        return await HandleResponse(response, cancellationToken);
    }

    public async Task<Result> UnblockUserAsync(long userId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsync($"/admin/users/{userId}/unblock", null, cancellationToken);
        return await HandleResponse(response, cancellationToken);
    }

    public async Task<Result> DeleteUserAsync(long userId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync($"/admin/users/{userId}", cancellationToken);
        return await HandleResponse(response, cancellationToken);
    }

    public async Task<Result<IReadOnlyCollection<SubjectListItemDto>>> GetAllSubjectsAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("/admin/subjects", cancellationToken);
        return await HandleResponse<IReadOnlyCollection<SubjectListItemDto>>(response, cancellationToken);
    }

    public async Task<Result<SubjectDetailsDto>> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/admin/subjects", request, _jsonOptions, cancellationToken);
        return await HandleResponse<SubjectDetailsDto>(response, cancellationToken);
    }

    public async Task<Result<SubjectDetailsDto>> UpdateSubjectAsync(long subjectId, UpdateSubjectRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsJsonAsync($"/admin/subjects/{subjectId}", request, _jsonOptions, cancellationToken);
        return await HandleResponse<SubjectDetailsDto>(response, cancellationToken);
    }

    public async Task<Result> DeleteSubjectAsync(long subjectId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync($"/admin/subjects/{subjectId}", cancellationToken);
        return await HandleResponse(response, cancellationToken);
    }

    public async Task<Result<IReadOnlyCollection<TopicListItemDto>>> GetAllTopicsAsync(long? subjectId, CancellationToken cancellationToken)
    {
        var url = "/admin/topics";
        if (subjectId.HasValue)
        {
            url += $"?subjectId={subjectId.Value}";
        }
        var response = await _httpClient.GetAsync(url, cancellationToken);
        return await HandleResponse<IReadOnlyCollection<TopicListItemDto>>(response, cancellationToken);
    }

    public async Task<Result<TopicDetailsDto>> CreateTopicAsync(CreateTopicRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/admin/topics", request, _jsonOptions, cancellationToken);
        return await HandleResponse<TopicDetailsDto>(response, cancellationToken);
    }

    public async Task<Result<TopicDetailsDto>> UpdateTopicAsync(long topicId, UpdateTopicRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsJsonAsync($"/admin/topics/{topicId}", request, _jsonOptions, cancellationToken);
        return await HandleResponse<TopicDetailsDto>(response, cancellationToken);
    }

    public async Task<Result> DeleteTopicAsync(long topicId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync($"/admin/topics/{topicId}", cancellationToken);
        return await HandleResponse(response, cancellationToken);
    }

    public async Task<Result<IReadOnlyCollection<TaskListItemDto>>> GetAllTasksAsync(TaskFilterDto? filter, CancellationToken cancellationToken)
    {
        var url = "/admin/tasks";
        if (filter != null)
        {
            var queryParams = new List<string>();
            if (filter.SubjectId.HasValue) queryParams.Add($"subjectId={filter.SubjectId.Value}");
            if (filter.TopicId.HasValue) queryParams.Add($"topicId={filter.TopicId.Value}");
            if (!string.IsNullOrWhiteSpace(filter.TaskType)) queryParams.Add($"taskType={Uri.EscapeDataString(filter.TaskType)}");
            if (filter.Difficulty.HasValue) queryParams.Add($"difficulty={filter.Difficulty.Value}");
            if (filter.IsActive.HasValue) queryParams.Add($"isActive={filter.IsActive.Value}");
            if (!string.IsNullOrWhiteSpace(filter.Search)) queryParams.Add($"search={Uri.EscapeDataString(filter.Search)}");
            
            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }
        }
        var response = await _httpClient.GetAsync(url, cancellationToken);
        return await HandleResponse<IReadOnlyCollection<TaskListItemDto>>(response, cancellationToken);
    }

    public async Task<Result<TaskDetailsDto>> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/admin/tasks", request, _jsonOptions, cancellationToken);
        return await HandleResponse<TaskDetailsDto>(response, cancellationToken);
    }

    public async Task<Result<TaskDetailsDto>> UpdateTaskAsync(long taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsJsonAsync($"/admin/tasks/{taskId}", request, _jsonOptions, cancellationToken);
        return await HandleResponse<TaskDetailsDto>(response, cancellationToken);
    }

    public async Task<Result> ActivateTaskAsync(long taskId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsync($"/admin/tasks/{taskId}/activate", null, cancellationToken);
        return await HandleResponse(response, cancellationToken);
    }

    public async Task<Result> DeactivateTaskAsync(long taskId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsync($"/admin/tasks/{taskId}/deactivate", null, cancellationToken);
        return await HandleResponse(response, cancellationToken);
    }

    public async Task<Result> DeleteTaskAsync(long taskId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync($"/admin/tasks/{taskId}", cancellationToken);
        return await HandleResponse(response, cancellationToken);
    }

    public async Task<Result<IReadOnlyCollection<TestListItemDto>>> GetAllTestsAsync(TestFilterDto? filter, CancellationToken cancellationToken)
    {
        var url = "/admin/tests";
        if (filter != null)
        {
            var queryParams = new List<string>();
            if (filter.SubjectId.HasValue) queryParams.Add($"subjectId={filter.SubjectId.Value}");
            if (!string.IsNullOrWhiteSpace(filter.TestKind)) queryParams.Add($"testKind={Uri.EscapeDataString(filter.TestKind)}");
            if (filter.AuthorId.HasValue) queryParams.Add($"authorId={filter.AuthorId.Value}");
            if (filter.IsPublished.HasValue) queryParams.Add($"isPublished={filter.IsPublished.Value}");
            if (filter.IsStateArchive.HasValue) queryParams.Add($"isStateArchive={filter.IsStateArchive.Value}");
            if (!string.IsNullOrWhiteSpace(filter.Search)) queryParams.Add($"search={Uri.EscapeDataString(filter.Search)}");
            
            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }
        }
        var response = await _httpClient.GetAsync(url, cancellationToken);
        return await HandleResponse<IReadOnlyCollection<TestListItemDto>>(response, cancellationToken);
    }

    public async Task<Result<TestDetailsDto>> CreateTestAsync(CreateTestRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/admin/tests", request, _jsonOptions, cancellationToken);
        return await HandleResponse<TestDetailsDto>(response, cancellationToken);
    }

    public async Task<Result<TestDetailsDto>> UpdateTestAsync(long testId, UpdateTestRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsJsonAsync($"/admin/tests/{testId}", request, _jsonOptions, cancellationToken);
        return await HandleResponse<TestDetailsDto>(response, cancellationToken);
    }

    public async Task<Result> DeleteTestAsync(long testId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync($"/admin/tests/{testId}", cancellationToken);
        return await HandleResponse(response, cancellationToken);
    }

    public async Task<Result<IReadOnlyCollection<InvitationCodeListItemDto>>> GetAllInvitationCodesAsync(long? teacherId, string? status, CancellationToken cancellationToken)
    {
        var url = "/admin/invitation-codes";
        var queryParams = new List<string>();
        if (teacherId.HasValue)
        {
            queryParams.Add($"teacherId={teacherId.Value}");
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            queryParams.Add($"status={Uri.EscapeDataString(status)}");
        }
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }
        var response = await _httpClient.GetAsync(url, cancellationToken);
        return await HandleResponse<IReadOnlyCollection<InvitationCodeListItemDto>>(response, cancellationToken);
    }

    public async Task<Result<InvitationCodeDetailsDto>> CreateInvitationCodeAsync(CreateInvitationCodeRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/admin/invitation-codes", request, _jsonOptions, cancellationToken);
        return await HandleResponse<InvitationCodeDetailsDto>(response, cancellationToken);
    }

    public async Task<Result<InvitationCodeDetailsDto>> UpdateInvitationCodeAsync(long invitationCodeId, UpdateInvitationCodeRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsJsonAsync($"/admin/invitation-codes/{invitationCodeId}", request, _jsonOptions, cancellationToken);
        return await HandleResponse<InvitationCodeDetailsDto>(response, cancellationToken);
    }

    public async Task<Result> DeleteInvitationCodeAsync(long invitationCodeId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync($"/admin/invitation-codes/{invitationCodeId}", cancellationToken);
        return await HandleResponse(response, cancellationToken);
    }

    private async Task<Result<T>> HandleResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result<T>.Fail(error);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return Result<T>.Ok(default!);
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return Result<T>.Fail("Server returned empty response.");
        }

        try
        {
            var parseResult = TryParseResponsePayload<T>(payload);
            if (parseResult.Success)
            {
                return Result<T>.Ok(parseResult.Value!);
            }

            return Result<T>.Fail(parseResult.Error ?? "Failed to process server response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return Result<T>.Fail($"Failed to parse server response. Response: {Truncate(payload)}");
        }
    }

    private Result<T> TryParseResponsePayload<T>(string payload)
    {
        using var document = System.Text.Json.JsonDocument.Parse(payload);
        var root = document.RootElement;

        if (root.ValueKind == System.Text.Json.JsonValueKind.Object && root.TryGetProperty("success", out var successProperty))
        {
            var success = successProperty.ValueKind switch
            {
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                _ => false
            };

            var message = root.TryGetProperty("message", out var messageProperty) && messageProperty.ValueKind == System.Text.Json.JsonValueKind.String
                ? messageProperty.GetString()
                : null;

            if (!success)
            {
                return Result<T>.Fail(string.IsNullOrWhiteSpace(message)
                    ? "Operation rejected by server."
                    : message);
            }

            if (!root.TryGetProperty("result", out var resultProperty) || resultProperty.ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                return Result<T>.Fail("Server returned empty result.");
            }

            var resultValue = System.Text.Json.JsonSerializer.Deserialize<T>(resultProperty.GetRawText(), _jsonOptions);
            return resultValue == null
                ? Result<T>.Fail("Failed to convert response data.")
                : Result<T>.Ok(resultValue);
        }

        var direct = System.Text.Json.JsonSerializer.Deserialize<T>(payload, _jsonOptions);
        return direct == null
            ? Result<T>.Fail("Failed to parse server response.")
            : Result<T>.Ok(direct);
    }

    private static string Truncate(string value, int maxLength = 500)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";

    private async Task<Result> HandleResponse(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Fail(string.IsNullOrWhiteSpace(error) ? "Request failed." : error);
        }

        return Result.Ok();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    public sealed record RegisterRequest
    {
        public required string Email { get; init; }
        public required string UserName { get; init; }
        public required string Password { get; init; }
        public int RoleTypeId { get; init; }
    }

    public sealed record AuthorizeRequest
    {
        public required string Email { get; init; }
        public required string Password { get; init; }
    }

    public sealed record LoginResponse
    {
        public required string AccessToken { get; init; }
        public string? UserName { get; init; }
    }

    public sealed class Result<T>
    {
        public bool Success { get; private set; }
        public T? Value { get; private set; }
        public string? Error { get; private set; }

        private Result(bool success, T? value, string? error)
        {
            Success = success;
            Value = value;
            Error = error;
        }

        public static Result<T> Ok(T value) => new(true, value, null);
        public static Result<T> Fail(string error) => new(false, default, error);
    }

    public sealed class Result
    {
        public bool Success { get; private set; }
        public string? Error { get; private set; }

        private Result(bool success, string? error)
        {
            Success = success;
            Error = error;
        }

        public static Result Ok() => new(true, null);
        public static Result Fail(string error) => new(false, error);
    }

    public sealed record UserListItemDto
    {
        public long Id { get; init; }
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string RoleName { get; init; } = string.Empty;
        public int RoleId { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    public sealed record UserDetailsDto
    {
        public long Id { get; init; }
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string RoleName { get; init; } = string.Empty;
        public int RoleId { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    public sealed record CreateUserRequest
    {
        public required string UserName { get; init; }
        public required string Email { get; init; }
        public required string Password { get; init; }
        public int RoleTypeId { get; init; }
    }

    public sealed record UpdateUserRequest
    {
        public string? UserName { get; init; }
        public string? Email { get; init; }
        public string? Password { get; init; }
        public int? RoleTypeId { get; init; }
    }

    public sealed record SubjectListItemDto
    {
        public long Id { get; init; }
        public string SubjectCode { get; init; } = string.Empty;
        public string SubjectName { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    public sealed record SubjectDetailsDto
    {
        public long Id { get; init; }
        public string SubjectCode { get; init; } = string.Empty;
        public string SubjectName { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    public sealed record CreateSubjectRequest
    {
        public required string SubjectCode { get; init; }
        public required string SubjectName { get; init; }
        public bool IsActive { get; init; }
    }

    public sealed record UpdateSubjectRequest
    {
        public string? SubjectCode { get; init; }
        public string? SubjectName { get; init; }
        public bool? IsActive { get; init; }
    }

    public sealed record TopicListItemDto
    {
        public long Id { get; init; }
        public long SubjectId { get; init; }
        public string SubjectName { get; init; } = string.Empty;
        public string TopicName { get; init; } = string.Empty;
        public string? TopicCode { get; init; }
        public long? TopicParentId { get; init; }
        public string? ParentTopicName { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    public sealed record TopicDetailsDto
    {
        public long Id { get; init; }
        public long SubjectId { get; init; }
        public string SubjectName { get; init; } = string.Empty;
        public string TopicName { get; init; } = string.Empty;
        public string? TopicCode { get; init; }
        public long? TopicParentId { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    public sealed record CreateTopicRequest
    {
        public long SubjectId { get; init; }
        public required string TopicName { get; init; }
        public string? TopicCode { get; init; }
        public long? TopicParentId { get; init; }
        public bool IsActive { get; init; }
    }

    public sealed record UpdateTopicRequest
    {
        public long? SubjectId { get; init; }
        public string? TopicName { get; init; }
        public string? TopicCode { get; init; }
        public long? TopicParentId { get; init; }
        public bool? IsActive { get; init; }
    }

    public sealed record TaskListItemDto
    {
        public long Id { get; init; }
        public long SubjectId { get; init; }
        public long? TopicId { get; init; }
        public string? TopicName { get; init; }
        public string? TopicCode { get; init; }
        public string TaskType { get; init; } = string.Empty;
        public short Difficulty { get; init; }
        public string Statement { get; init; } = string.Empty;
        public string? CorrectAnswer { get; init; }
        public string? Explanation { get; init; }
    }

    public sealed record TaskDetailsDto
    {
        public long Id { get; init; }
        public long SubjectId { get; init; }
        public string SubjectName { get; init; } = string.Empty;
        public long? TopicId { get; init; }
        public string? TopicName { get; init; }
        public string TaskType { get; init; } = string.Empty;
        public short Difficulty { get; init; }
        public string Statement { get; init; } = string.Empty;
        public string? CorrectAnswer { get; init; }
        public string? Explanation { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    public sealed record TaskFilterDto
    {
        public long? SubjectId { get; init; }
        public long? TopicId { get; init; }
        public string? TaskType { get; init; }
        public short? Difficulty { get; init; }
        public bool? IsActive { get; init; }
        public string? Search { get; init; }
    }

    public sealed record CreateTaskRequest
    {
        public long SubjectId { get; init; }
        public long? TopicId { get; init; }
        public required string TaskType { get; init; }
        public short Difficulty { get; init; }
        public required string Statement { get; init; }
        public required string CorrectAnswer { get; init; }
        public string? Explanation { get; init; }
        public bool IsActive { get; init; }
    }

    public sealed record UpdateTaskRequest
    {
        public long? TopicId { get; init; }
        public string? TaskType { get; init; }
        public short? Difficulty { get; init; }
        public string? Statement { get; init; }
        public string? CorrectAnswer { get; init; }
        public string? Explanation { get; init; }
        public bool? IsActive { get; init; }
    }

    
    public sealed record TestListItemDto
    {
        public long Id { get; init; }
        public long SubjectId { get; init; }
        public string SubjectName { get; init; } = string.Empty;
        public string TestKind { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public long? AuthorId { get; init; }
        public string? AuthorName { get; init; }
        public bool IsPublished { get; init; }
        public bool IsPublic { get; init; }
        public bool IsStateArchive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    public sealed record TestDetailsDto
    {
        public long Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string TestKind { get; init; } = string.Empty;
        public long SubjectId { get; init; }
        public int? TimeLimitSec { get; init; }
        public short? AttemptsAllowed { get; init; }
        public string? Mode { get; init; }
        public bool IsPublished { get; init; }
        public bool IsPublic { get; init; }
        public bool IsStateArchive { get; init; }
        public IReadOnlyCollection<TestTaskDto> Tasks { get; init; } = Array.Empty<TestTaskDto>();
    }

    public sealed record TestTaskDto
    {
        public long TaskId { get; init; }
        public int Position { get; init; }
        public string TaskType { get; init; } = string.Empty;
        public string Statement { get; init; } = string.Empty;
        public short Difficulty { get; init; }
        public string? Explanation { get; init; }
    }

    public sealed record TestFilterDto
    {
        public long? SubjectId { get; init; }
        public string? TestKind { get; init; }
        public long? AuthorId { get; init; }
        public bool? IsPublished { get; init; }
        public bool? IsStateArchive { get; init; }
        public string? Search { get; init; }
    }

    public sealed record CreateTestRequest
    {
        public long SubjectId { get; init; }
        public required string TestKind { get; init; }
        public required string Title { get; init; }
        public int? TimeLimitSec { get; init; }
        public short? AttemptsAllowed { get; init; }
        public string? Mode { get; init; }
        public bool IsPublished { get; init; }
        public bool IsPublic { get; init; }
        public bool IsStateArchive { get; init; }
        public IReadOnlyCollection<TestTaskUpdateDto> Tasks { get; init; } = Array.Empty<TestTaskUpdateDto>();
    }

    public sealed record TestTaskUpdateDto
    {
        public long TaskId { get; init; }
        public int Position { get; init; }
        public decimal? Weight { get; init; }
    }

    public sealed record UpdateTestRequest
    {
        public required string Title { get; init; }
        public required string TestKind { get; init; }
        public long SubjectId { get; init; }
        public int? TimeLimitSec { get; init; }
        public short? AttemptsAllowed { get; init; }
        public string? Mode { get; init; }
        public bool IsPublished { get; init; }
        public bool IsPublic { get; init; }
        public bool IsStateArchive { get; init; }
        public IReadOnlyCollection<TestTaskUpdateDto> Tasks { get; init; } = Array.Empty<TestTaskUpdateDto>();
    }

    public sealed record InvitationCodeListItemDto
    {
        public long Id { get; init; }
        public long TeacherId { get; init; }
        public string TeacherName { get; init; } = string.Empty;
        public string TeacherEmail { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public int? MaxUses { get; init; }
        public int UsedCount { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

    public sealed record InvitationCodeDetailsDto
    {
        public long Id { get; init; }
        public long TeacherId { get; init; }
        public string TeacherName { get; init; } = string.Empty;
        public string TeacherEmail { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public int? MaxUses { get; init; }
        public int UsedCount { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

    public sealed record CreateInvitationCodeRequest
    {
        public long TeacherId { get; init; }
        public int? MaxUses { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
    }

    public sealed record UpdateInvitationCodeRequest
    {
        public int? MaxUses { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public string? Status { get; init; }
    }
}

