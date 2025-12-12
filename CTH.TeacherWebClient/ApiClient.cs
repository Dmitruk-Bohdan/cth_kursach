using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;

namespace CTH.TeacherWebClient;

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

        // Декодируем токен для получения роли
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
                    // Если роль не найдена, попробуем найти по другому типу claim
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
            // Игнорируем ошибки декодирования токена, но сохраняем токен
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

    public async Task<Result<IReadOnlyCollection<SubjectListItem>>> GetAllSubjectsAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("/student/statistics/subjects", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result<IReadOnlyCollection<SubjectListItem>>.Fail(error);
        }

        // API оборачивает ответ в ResponseModel
        var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel<List<SubjectListItem>>>(_jsonOptions, cancellationToken);
        if (responseModel == null || !responseModel.Success || responseModel.Result == null)
        {
            return Result<IReadOnlyCollection<SubjectListItem>>.Fail(responseModel?.Message ?? "Failed to parse response.");
        }

        return Result<IReadOnlyCollection<SubjectListItem>>.Ok(responseModel.Result);
    }

    public async Task<Result<IReadOnlyCollection<TaskListItem>>> GetTasksBySubjectAsync(long subjectId, string? search = null, CancellationToken cancellationToken = default)
    {
        var uri = $"/teacher/tests/tasks?subjectId={subjectId}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            uri += $"&search={Uri.EscapeDataString(search)}";
        }
        var response = await _httpClient.GetAsync(uri, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result<IReadOnlyCollection<TaskListItem>>.Fail(error);
        }

        // API может возвращать либо ResponseModel, либо напрямую список
        try
        {
            var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel<List<TaskListItem>>>(_jsonOptions, cancellationToken);
            if (responseModel != null && responseModel.Success && responseModel.Result != null)
            {
                return Result<IReadOnlyCollection<TaskListItem>>.Ok(responseModel.Result);
            }
        }
        catch
        {
            // Если не ResponseModel, попробуем напрямую список
        }

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskListItem>>(_jsonOptions, cancellationToken);
        if (tasks == null)
        {
            return Result<IReadOnlyCollection<TaskListItem>>.Fail("Failed to parse response.");
        }

        return Result<IReadOnlyCollection<TaskListItem>>.Ok(tasks);
    }

    public async Task<Result<IReadOnlyCollection<TopicListItem>>> GetTopicsBySubjectAsync(long subjectId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/teacher/tests/topics?subjectId={subjectId}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result<IReadOnlyCollection<TopicListItem>>.Fail(error);
        }

        try
        {
            var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel<List<TopicListItem>>>(_jsonOptions, cancellationToken);
            if (responseModel != null && responseModel.Success && responseModel.Result != null)
            {
                return Result<IReadOnlyCollection<TopicListItem>>.Ok(responseModel.Result);
            }
        }
        catch
        {
        }

        var topics = await response.Content.ReadFromJsonAsync<List<TopicListItem>>(_jsonOptions, cancellationToken);
        if (topics == null)
        {
            return Result<IReadOnlyCollection<TopicListItem>>.Fail("Failed to parse response.");
        }

        return Result<IReadOnlyCollection<TopicListItem>>.Ok(topics);
    }

    public async Task<Result<CreatedTaskResponse>> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/teacher/tests/tasks", request, _jsonOptions, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var taskDetails = await response.Content.ReadFromJsonAsync<TaskListItem>(_jsonOptions, cancellationToken);
            if (taskDetails != null)
            {
                var result = new CreatedTaskResponse(taskDetails.Id, taskDetails.TaskType, taskDetails.Difficulty, taskDetails.Statement);
                return Result<CreatedTaskResponse>.Ok(result);
            }
            return Result<CreatedTaskResponse>.Fail("Failed to parse response.");
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result<CreatedTaskResponse>.Fail(error);
    }

    public async Task<Result<CreatedTestResponse>> CreateTestAsync(CreateTestRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/teacher/tests", request, _jsonOptions, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            // API возвращает TestDetailsDto, но нам нужен только ID и Title
            var testDetails = await response.Content.ReadFromJsonAsync<TestDetailsDto>(_jsonOptions, cancellationToken);
            if (testDetails != null)
            {
                var result = new CreatedTestResponse(testDetails.Id, testDetails.Title, testDetails.TestKind, testDetails.SubjectId);
                return Result<CreatedTestResponse>.Ok(result);
            }
            return Result<CreatedTestResponse>.Fail("Failed to parse response.");
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result<CreatedTestResponse>.Fail(error);
    }

    public async Task<Result<IReadOnlyCollection<TestListItem>>> GetMyTestsAsync(long subjectId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/teacher/tests?subjectId={subjectId}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result<IReadOnlyCollection<TestListItem>>.Fail(error);
        }

        try
        {
            var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel<List<TestListItem>>>(_jsonOptions, cancellationToken);
            if (responseModel != null && responseModel.Success && responseModel.Result != null)
            {
                return Result<IReadOnlyCollection<TestListItem>>.Ok(responseModel.Result);
            }
        }
        catch
        {
        }

        var tests = await response.Content.ReadFromJsonAsync<List<TestListItem>>(_jsonOptions, cancellationToken);
        if (tests == null)
        {
            return Result<IReadOnlyCollection<TestListItem>>.Fail("Failed to parse response.");
        }

        return Result<IReadOnlyCollection<TestListItem>>.Ok(tests);
    }

    public async Task<Result<TestDetailsDto>> GetTestDetailsAsync(long testId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/teacher/tests/{testId}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result<TestDetailsDto>.Fail(error);
        }

        try
        {
            var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel<TestDetailsDto>>(_jsonOptions, cancellationToken);
            if (responseModel != null && responseModel.Success && responseModel.Result != null)
            {
                return Result<TestDetailsDto>.Ok(responseModel.Result);
            }
        }
        catch
        {
        }

        var testDetails = await response.Content.ReadFromJsonAsync<TestDetailsDto>(_jsonOptions, cancellationToken);
        if (testDetails == null)
        {
            return Result<TestDetailsDto>.Fail("Failed to parse response.");
        }

        return Result<TestDetailsDto>.Ok(testDetails);
    }

    public async Task<Result> UpdateTestAsync(long testId, UpdateTestRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsJsonAsync($"/teacher/tests/{testId}", request, _jsonOptions, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            return Result.Ok();
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result.Fail(error);
    }

    public async Task<Result<InvitationCodeItem>> CreateInvitationCodeAsync(CreateInvitationCodeRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/teacher/students/invitations", request, _jsonOptions, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var code = await response.Content.ReadFromJsonAsync<InvitationCodeItem>(_jsonOptions, cancellationToken);
            if (code != null)
            {
                return Result<InvitationCodeItem>.Ok(code);
            }
            return Result<InvitationCodeItem>.Fail("Failed to parse response.");
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result<InvitationCodeItem>.Fail(error);
    }

    public async Task<Result<IReadOnlyCollection<InvitationCodeItem>>> GetInvitationCodesAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("/teacher/students/invitations", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result<IReadOnlyCollection<InvitationCodeItem>>.Fail(error);
        }

        try
        {
            var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel<List<InvitationCodeItem>>>(_jsonOptions, cancellationToken);
            if (responseModel != null && responseModel.Success && responseModel.Result != null)
            {
                return Result<IReadOnlyCollection<InvitationCodeItem>>.Ok(responseModel.Result);
            }
        }
        catch
        {
        }

        var codes = await response.Content.ReadFromJsonAsync<List<InvitationCodeItem>>(_jsonOptions, cancellationToken);
        if (codes == null)
        {
            return Result<IReadOnlyCollection<InvitationCodeItem>>.Fail("Failed to parse response.");
        }

        return Result<IReadOnlyCollection<InvitationCodeItem>>.Ok(codes);
    }

    public async Task<Result> RevokeInvitationCodeAsync(long invitationCodeId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsync($"/teacher/students/invitations/{invitationCodeId}/revoke", null, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            return Result.Ok();
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result.Fail(error);
    }

    public async Task<Result> DeleteInvitationCodeAsync(long invitationCodeId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync($"/teacher/students/invitations/{invitationCodeId}", cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            return Result.Ok();
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result.Fail(error);
    }

    public async Task<Result<IReadOnlyCollection<StudentListItem>>> GetMyStudentsAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("/teacher/students", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result<IReadOnlyCollection<StudentListItem>>.Fail(error);
        }

        try
        {
            var responseModel = await response.Content.ReadFromJsonAsync<ResponseModel<List<StudentListItem>>>(_jsonOptions, cancellationToken);
            if (responseModel != null && responseModel.Success && responseModel.Result != null)
            {
                return Result<IReadOnlyCollection<StudentListItem>>.Ok(responseModel.Result);
            }
        }
        catch
        {
        }

        var students = await response.Content.ReadFromJsonAsync<List<StudentListItem>>(_jsonOptions, cancellationToken);
        if (students == null)
        {
            return Result<IReadOnlyCollection<StudentListItem>>.Fail("Failed to parse response.");
        }

        return Result<IReadOnlyCollection<StudentListItem>>.Ok(students);
    }

    public async Task<Result> RemoveStudentAsync(long studentId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync($"/teacher/students/{studentId}", cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            return Result.Ok();
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result.Fail(error);
    }

    private async Task<Result<T>> ReadResponseModelAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var model = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
            return model != null
                ? Result<T>.Ok(model)
                : Result<T>.Fail("Failed to parse response.");
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result<T>.Fail(error);
    }

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

    public sealed record SubjectListItem(long Id, string SubjectCode, string SubjectName);

    public sealed record TaskListItem(
        long Id,
        long SubjectId,
        long? TopicId,
        string? TopicName,
        string? TopicCode,
        string TaskType,
        short Difficulty,
        string Statement,
        string? Explanation);

    public sealed record TopicListItem(long Id, long SubjectId, string TopicName, string? TopicCode);

    public sealed record TestListItem(
        long Id,
        string Title,
        string TestKind,
        long SubjectId,
        int? TimeLimitSec,
        short? AttemptsAllowed,
        bool IsPublic,
        bool IsStateArchive,
        string? Mode);

    public sealed record CreateTaskRequest(
        long SubjectId,
        long? TopicId,
        string TaskType,
        short Difficulty,
        string Statement,
        string CorrectAnswer,
        string? Explanation,
        bool IsActive);

    public sealed record CreatedTaskResponse(long Id, string TaskType, short Difficulty, string Statement);

    // Класс для десериализации обернутого ответа API
    private sealed class ResponseModel<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Result { get; set; }
    }

    public sealed record CreateTestRequest(
        long SubjectId,
        string TestKind,
        string Title,
        int? TimeLimitSec,
        short? AttemptsAllowed,
        string? Mode,
        bool IsPublished,
        bool IsPublic,
        bool IsStateArchive,
        IReadOnlyCollection<TestTaskRequest> Tasks);

    public sealed record TestTaskRequest(long TaskId, int Position, decimal? Weight);

    public sealed record CreatedTestResponse(long Id, string Title, string TestKind, long SubjectId);

    public sealed record UpdateTestRequest(
        long SubjectId,
        string TestKind,
        string Title,
        int? TimeLimitSec,
        short? AttemptsAllowed,
        string? Mode,
        bool IsPublished,
        bool IsPublic,
        bool IsStateArchive,
        IReadOnlyCollection<TestTaskRequest> Tasks);

    public sealed record InvitationCodeItem(
        long Id,
        long TeacherId,
        string Code,
        int? MaxUses,
        int UsedCount,
        DateTimeOffset? ExpiresAt,
        string Status,
        DateTimeOffset CreatedAt);

    public sealed record CreateInvitationCodeRequest(
        int? MaxUses,
        DateTimeOffset? ExpiresAt);

    public sealed record StudentListItem(
        long Id,
        string UserName,
        string Email,
        DateTimeOffset? EstablishedAt);

    // Класс для десериализации ответа API
    public sealed class TestDetailsDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TestKind { get; set; } = string.Empty;
        public long SubjectId { get; set; }
        public int? TimeLimitSec { get; set; }
        public short? AttemptsAllowed { get; set; }
        public string? Mode { get; set; }
        public bool IsPublished { get; set; }
        public bool IsPublic { get; set; }
        public bool IsStateArchive { get; set; }
        public IReadOnlyCollection<TestTaskDto>? Tasks { get; set; }
    }

    public sealed class TestTaskDto
    {
        public long TaskId { get; set; }
        public int Position { get; set; }
        public string TaskType { get; set; } = string.Empty;
        public string Statement { get; set; } = string.Empty;
        public short Difficulty { get; set; }
        public string? Explanation { get; set; }
    }
}

public sealed class Result
{
    public bool Success { get; private set; }
    public string? Error { get; private set; }

    private Result(bool success, string? error = null)
    {
        Success = success;
        Error = error;
    }

    public static Result Ok() => new(true);
    public static Result Fail(string error) => new(false, error);
}

public sealed class Result<T>
{
    public bool Success { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }

    private Result(bool success, T? value = default, string? error = null)
    {
        Success = success;
        Value = value;
        Error = error;
    }

    public static Result<T> Ok(T value) => new(true, value);
    public static Result<T> Fail(string error) => new(false, default, error);
}

