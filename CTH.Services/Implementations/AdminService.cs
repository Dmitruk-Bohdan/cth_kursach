using CTH.Common.Helpers;
using CTH.Database.Abstractions;
using CTH.Database.Repositories.Interfaces;
using CTH.Services.Interfaces;
using Admin = CTH.Services.Models.Dto.Admin;
using Tasks = CTH.Services.Models.Dto.Tasks;
using CTH.Services.Models.Dto.Tests;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using PropTechPeople.Services.Models.ResultApiModels;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CTH.Services.Implementations;

public class AdminService : IAdminService
{
    private readonly ISqlExecutor _sqlExecutor;
    private readonly ISqlQueryProvider _sqlQueryProvider;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ITestRepository _testRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IInvitationCodeRepository _invitationCodeRepository;
    private readonly ILogger<AdminService> _logger;

    // Query strings
    private readonly string _getAllUsersQuery;
    private readonly string _getAllSubjectsQuery;
    private readonly string _getAllTopicsQuery;
    private readonly string _getAllTasksQuery;
    private readonly string _getAllTestsQuery;
    private readonly string _getAllInvitationCodesQuery;
    
    // Command strings
    private readonly string _createUserQuery;
    private readonly string _updateUserQuery;
    private readonly string _blockUserQuery;
    private readonly string _deleteUserQuery;
    private readonly string _createSubjectQuery;
    private readonly string _updateSubjectQuery;
    private readonly string _deleteSubjectQuery;
    private readonly string _createTopicQuery;
    private readonly string _updateTopicQuery;
    private readonly string _deleteTopicQuery;
    private readonly string _activateTaskQuery;
    private readonly string _deactivateTaskQuery;
    private readonly string _deleteTaskQuery;
    private readonly string _createInvitationCodeQuery;
    private readonly string _updateInvitationCodeQuery;
    private readonly string _deleteInvitationCodeQuery;

    public AdminService(
        ISqlExecutor sqlExecutor,
        ISqlQueryProvider sqlQueryProvider,
        IUserAccountRepository userAccountRepository,
        ITaskRepository taskRepository,
        ITestRepository testRepository,
        IUserSessionRepository userSessionRepository,
        IInvitationCodeRepository invitationCodeRepository,
        ILogger<AdminService> logger)
    {
        _sqlExecutor = sqlExecutor;
        _sqlQueryProvider = sqlQueryProvider;
        _userAccountRepository = userAccountRepository;
        _taskRepository = taskRepository;
        _testRepository = testRepository;
        _userSessionRepository = userSessionRepository;
        _invitationCodeRepository = invitationCodeRepository;
        _logger = logger;

        // Load queries
        _getAllUsersQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Queries/GetAllUsers");
        _getAllSubjectsQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Queries/GetAllSubjects");
        _getAllTopicsQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Queries/GetAllTopics");
        _getAllTasksQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Queries/GetAllTasks");
        _getAllTestsQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Queries/GetAllTests");
        _getAllInvitationCodesQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Queries/GetAllInvitationCodes");
        
        _createUserQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/CreateUser");
        _updateUserQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/UpdateUser");
        _blockUserQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/BlockUser");
        _deleteUserQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/DeleteUser");
        _createSubjectQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/CreateSubject");
        _updateSubjectQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/UpdateSubject");
        _deleteSubjectQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/DeleteSubject");
        _createTopicQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/CreateTopic");
        _updateTopicQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/UpdateTopic");
        _deleteTopicQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/DeleteTopic");
        _activateTaskQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/ActivateTask");
        _deactivateTaskQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/DeactivateTask");
        _deleteTaskQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/DeleteTask");
        _createInvitationCodeQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/CreateInvitationCode");
        _updateInvitationCodeQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/UpdateInvitationCode");
        _deleteInvitationCodeQuery = _sqlQueryProvider.GetQuery("AdminUseCases/Commands/DeleteInvitationCode");
    }

    // Users
    public async Task<HttpOperationResult<IReadOnlyCollection<Admin.UserListItemDto>>> GetAllUsersAsync(CancellationToken cancellationToken)
    {
        try
        {
            var users = await _sqlExecutor.QueryAsync(
                _getAllUsersQuery,
                reader => new Admin.UserListItemDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    UserName = reader.GetString(reader.GetOrdinal("user_name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    RoleName = reader.GetString(reader.GetOrdinal("role_name")),
                    RoleId = reader.GetInt32(reader.GetOrdinal("role_id")),
                    LastLoginAt = reader.IsDBNull(reader.GetOrdinal("last_login_at"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("last_login_at")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                },
                null,
                cancellationToken);

            return new HttpOperationResult<IReadOnlyCollection<Admin.UserListItemDto>>
            {
                Result = users,
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all users");
            return new HttpOperationResult<IReadOnlyCollection<Admin.UserListItemDto>>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to retrieve users"
            };
        }
    }

    public async Task<HttpOperationResult<Admin.UserDetailsDto>> CreateUserAsync(Admin.CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if user exists
            var existingUser = await _userAccountRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existingUser != null)
            {
                return new HttpOperationResult<Admin.UserDetailsDto>
                {
                    Status = HttpStatusCode.Conflict,
                    Error = $"User with email {request.Email} already exists"
                };
            }

            var passwordHash = PasswordHelper.HashPassword(request.Password);
            var parameters = new[]
            {
                new NpgsqlParameter("user_name", NpgsqlDbType.Varchar) { Value = request.UserName },
                new NpgsqlParameter("email", NpgsqlDbType.Varchar) { Value = request.Email },
                new NpgsqlParameter("password_hash", NpgsqlDbType.Varchar) { Value = passwordHash },
                new NpgsqlParameter("role_type_id", NpgsqlDbType.Integer) { Value = request.RoleTypeId }
            };

            var userId = await _sqlExecutor.QuerySingleAsync(
                _createUserQuery,
                reader => reader.GetInt64(0),
                parameters,
                cancellationToken);

            if (userId == 0)
            {
                return new HttpOperationResult<Admin.UserDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Failed to create user"
                };
            }

            // Get created user details
            var users = await GetAllUsersAsync(cancellationToken);
            var user = users.Result?.FirstOrDefault(u => u.Id == userId);
            
            if (user == null)
            {
                return new HttpOperationResult<Admin.UserDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "User created but failed to retrieve details"
                };
            }

            return new HttpOperationResult<Admin.UserDetailsDto>
            {
                Result = new Admin.UserDetailsDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    RoleName = user.RoleName,
                    RoleId = user.RoleId,
                    LastLoginAt = user.LastLoginAt,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.CreatedAt
                },
                Status = HttpStatusCode.Created
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user");
            return new HttpOperationResult<Admin.UserDetailsDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to create user"
            };
        }
    }

    public async Task<HttpOperationResult<Admin.UserDetailsDto>> UpdateUserAsync(long userId, Admin.UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new List<NpgsqlParameter>
            {
                new("user_id", NpgsqlDbType.Bigint) { Value = userId }
            };

            if (!string.IsNullOrWhiteSpace(request.UserName))
            {
                parameters.Add(new NpgsqlParameter("user_name", NpgsqlDbType.Varchar) { Value = request.UserName });
            }
            else
            {
                parameters.Add(new NpgsqlParameter("user_name", NpgsqlDbType.Varchar) { Value = DBNull.Value });
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                // Check if email is already taken by another user
                var existingUser = await _userAccountRepository.GetByEmailAsync(request.Email, cancellationToken);
                if (existingUser != null && existingUser.Id != userId)
                {
                return new HttpOperationResult<Admin.UserDetailsDto>
                {
                    Status = HttpStatusCode.Conflict,
                    Error = $"Email {request.Email} is already taken"
                };
                }
                parameters.Add(new NpgsqlParameter("email", NpgsqlDbType.Varchar) { Value = request.Email });
            }
            else
            {
                parameters.Add(new NpgsqlParameter("email", NpgsqlDbType.Varchar) { Value = DBNull.Value });
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var passwordHash = PasswordHelper.HashPassword(request.Password);
                parameters.Add(new NpgsqlParameter("password_hash", NpgsqlDbType.Varchar) { Value = passwordHash });
            }
            else
            {
                parameters.Add(new NpgsqlParameter("password_hash", NpgsqlDbType.Varchar) { Value = DBNull.Value });
            }

            if (request.RoleTypeId.HasValue)
            {
                parameters.Add(new NpgsqlParameter("role_type_id", NpgsqlDbType.Integer) { Value = request.RoleTypeId.Value });
            }
            else
            {
                parameters.Add(new NpgsqlParameter("role_type_id", NpgsqlDbType.Integer) { Value = DBNull.Value });
            }

            var updatedId = await _sqlExecutor.QuerySingleAsync(
                _updateUserQuery,
                reader => reader.GetInt64(0),
                parameters,
                cancellationToken);

            if (updatedId == 0)
            {
                return new HttpOperationResult<Admin.UserDetailsDto>
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "User not found"
                };
            }

            // Get updated user details
            var users = await GetAllUsersAsync(cancellationToken);
            var user = users.Result?.FirstOrDefault(u => u.Id == userId);
            
            if (user == null)
            {
                return new HttpOperationResult<Admin.UserDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "User updated but failed to retrieve details"
                };
            }

            return new HttpOperationResult<Admin.UserDetailsDto>
            {
                Result = new Admin.UserDetailsDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    RoleName = user.RoleName,
                    RoleId = user.RoleId,
                    LastLoginAt = user.LastLoginAt,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                },
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}", userId);
            return new HttpOperationResult<Admin.UserDetailsDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to update user"
            };
        }
    }

    public async Task<HttpOperationResult> BlockUserAsync(long userId, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new[]
            {
                new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId }
            };

            await _sqlExecutor.ExecuteAsync(_blockUserQuery, parameters, cancellationToken);

            return new HttpOperationResult
            {
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to block user {UserId}", userId);
            return new HttpOperationResult
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to block user"
            };
        }
    }

    public async Task<HttpOperationResult> UnblockUserAsync(long userId, CancellationToken cancellationToken)
    {
        // Unblocking is done by not revoking sessions - sessions will expire naturally
        // This is a no-op, but we return success for API consistency
        return new HttpOperationResult
        {
            Status = HttpStatusCode.OK
        };
    }

    public async Task<HttpOperationResult> DeleteUserAsync(long userId, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new[]
            {
                new NpgsqlParameter("user_id", NpgsqlDbType.Bigint) { Value = userId }
            };

            var affectedRows = await _sqlExecutor.ExecuteAsync(_deleteUserQuery, parameters, cancellationToken);

            if (affectedRows == 0)
            {
                return new HttpOperationResult
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "User not found"
                };
            }

            return new HttpOperationResult
            {
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId}", userId);
            return new HttpOperationResult
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to delete user. Check for dependencies."
            };
        }
    }

    // Continue with other methods...
    // Due to length, I'll continue in the next part

    public async Task<HttpOperationResult<IReadOnlyCollection<Admin.SubjectListItemDto>>> GetAllSubjectsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var subjects = await _sqlExecutor.QueryAsync(
                _getAllSubjectsQuery,
                reader => new Admin.SubjectListItemDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    SubjectCode = reader.GetString(reader.GetOrdinal("subject_code")),
                    SubjectName = reader.GetString(reader.GetOrdinal("subject_name")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                },
                null,
                cancellationToken);

            return new HttpOperationResult<IReadOnlyCollection<Admin.SubjectListItemDto>>
            {
                Result = subjects,
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all subjects");
            return new HttpOperationResult<IReadOnlyCollection<Admin.SubjectListItemDto>>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to retrieve subjects"
            };
        }
    }

    public async Task<HttpOperationResult<Admin.SubjectDetailsDto>> CreateSubjectAsync(Admin.CreateSubjectRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new[]
            {
                new NpgsqlParameter("subject_code", NpgsqlDbType.Varchar) { Value = request.SubjectCode },
                new NpgsqlParameter("subject_name", NpgsqlDbType.Varchar) { Value = request.SubjectName },
                new NpgsqlParameter("is_active", NpgsqlDbType.Boolean) { Value = request.IsActive }
            };

            var subjectId = await _sqlExecutor.QuerySingleAsync(
                _createSubjectQuery,
                reader => reader.GetInt64(0),
                parameters,
                cancellationToken);

            if (subjectId == 0)
            {
                return new HttpOperationResult<Admin.SubjectDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Failed to create subject"
                };
            }

            var subjects = await GetAllSubjectsAsync(cancellationToken);
            var subject = subjects.Result?.FirstOrDefault(s => s.Id == subjectId);
            
            if (subject == null)
            {
                return new HttpOperationResult<Admin.SubjectDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Subject created but failed to retrieve details"
                };
            }

            return new HttpOperationResult<Admin.SubjectDetailsDto>
            {
                Result = new Admin.SubjectDetailsDto
                {
                    Id = subject.Id,
                    SubjectCode = subject.SubjectCode,
                    SubjectName = subject.SubjectName,
                    IsActive = subject.IsActive,
                    CreatedAt = subject.CreatedAt,
                    UpdatedAt = subject.UpdatedAt
                },
                Status = HttpStatusCode.Created
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create subject");
            return new HttpOperationResult<Admin.SubjectDetailsDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to create subject"
            };
        }
    }

    public async Task<HttpOperationResult<Admin.SubjectDetailsDto>> UpdateSubjectAsync(long subjectId, Admin.UpdateSubjectRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new List<NpgsqlParameter>
            {
                new("subject_id", NpgsqlDbType.Bigint) { Value = subjectId },
                new("subject_code", NpgsqlDbType.Varchar) { Value = (object?)request.SubjectCode ?? DBNull.Value },
                new("subject_name", NpgsqlDbType.Varchar) { Value = (object?)request.SubjectName ?? DBNull.Value },
                new("is_active", NpgsqlDbType.Boolean) { Value = (object?)request.IsActive ?? DBNull.Value }
            };

            var updatedId = await _sqlExecutor.QuerySingleAsync(
                _updateSubjectQuery,
                reader => reader.GetInt64(0),
                parameters,
                cancellationToken);

            if (updatedId == 0)
            {
                return new HttpOperationResult<Admin.SubjectDetailsDto>
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "Subject not found"
                };
            }

            var subjects = await GetAllSubjectsAsync(cancellationToken);
            var subject = subjects.Result?.FirstOrDefault(s => s.Id == subjectId);
            
            if (subject == null)
            {
                return new HttpOperationResult<Admin.SubjectDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Subject updated but failed to retrieve details"
                };
            }

            return new HttpOperationResult<Admin.SubjectDetailsDto>
            {
                Result = new Admin.SubjectDetailsDto
                {
                    Id = subject.Id,
                    SubjectCode = subject.SubjectCode,
                    SubjectName = subject.SubjectName,
                    IsActive = subject.IsActive,
                    CreatedAt = subject.CreatedAt,
                    UpdatedAt = subject.UpdatedAt
                },
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update subject {SubjectId}", subjectId);
            return new HttpOperationResult<Admin.SubjectDetailsDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to update subject"
            };
        }
    }

    public async Task<HttpOperationResult> DeleteSubjectAsync(long subjectId, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new[]
            {
                new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = subjectId }
            };

            var affectedRows = await _sqlExecutor.ExecuteAsync(_deleteSubjectQuery, parameters, cancellationToken);

            if (affectedRows == 0)
            {
                return new HttpOperationResult
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "Subject not found"
                };
            }

            return new HttpOperationResult
            {
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete subject {SubjectId}", subjectId);
            return new HttpOperationResult
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to delete subject. Check for dependencies."
            };
        }
    }

    // Topics
    public async Task<HttpOperationResult<IReadOnlyCollection<Admin.TopicListItemDto>>> GetAllTopicsAsync(long? subjectId, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new[]
            {
                new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = (object?)subjectId ?? DBNull.Value }
            };

            var topics = await _sqlExecutor.QueryAsync(
                _getAllTopicsQuery,
                reader => new Admin.TopicListItemDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                    SubjectName = reader.GetString(reader.GetOrdinal("subject_name")),
                    TopicName = reader.GetString(reader.GetOrdinal("topic_name")),
                    TopicCode = reader.IsDBNull(reader.GetOrdinal("topic_code")) ? null : reader.GetString(reader.GetOrdinal("topic_code")),
                    TopicParentId = reader.IsDBNull(reader.GetOrdinal("topic_parent_id")) ? null : reader.GetInt64(reader.GetOrdinal("topic_parent_id")),
                    ParentTopicName = reader.IsDBNull(reader.GetOrdinal("parent_topic_name")) ? null : reader.GetString(reader.GetOrdinal("parent_topic_name")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                },
                parameters,
                cancellationToken);

            return new HttpOperationResult<IReadOnlyCollection<Admin.TopicListItemDto>>
            {
                Result = topics,
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all topics");
            return new HttpOperationResult<IReadOnlyCollection<Admin.TopicListItemDto>>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to retrieve topics"
            };
        }
    }

    public async Task<HttpOperationResult<Admin.TopicDetailsDto>> CreateTopicAsync(Admin.CreateTopicRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new[]
            {
                new NpgsqlParameter("subject_id", NpgsqlDbType.Bigint) { Value = request.SubjectId },
                new NpgsqlParameter("topic_name", NpgsqlDbType.Varchar) { Value = request.TopicName },
                new NpgsqlParameter("topic_code", NpgsqlDbType.Varchar) { Value = (object?)request.TopicCode ?? DBNull.Value },
                new NpgsqlParameter("topic_parent_id", NpgsqlDbType.Bigint) { Value = (object?)request.TopicParentId ?? DBNull.Value },
                new NpgsqlParameter("is_active", NpgsqlDbType.Boolean) { Value = request.IsActive }
            };

            var topicId = await _sqlExecutor.QuerySingleAsync(
                _createTopicQuery,
                reader => reader.GetInt64(0),
                parameters,
                cancellationToken);

            if (topicId == 0)
            {
                return new HttpOperationResult<Admin.TopicDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Failed to create topic"
                };
            }

            var topics = await GetAllTopicsAsync(request.SubjectId, cancellationToken);
            var topic = topics.Result?.FirstOrDefault(t => t.Id == topicId);
            
            if (topic == null)
            {
                return new HttpOperationResult<Admin.TopicDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Topic created but failed to retrieve details"
                };
            }

            return new HttpOperationResult<Admin.TopicDetailsDto>
            {
                Result = new Admin.TopicDetailsDto
                {
                    Id = topic.Id,
                    SubjectId = topic.SubjectId,
                    SubjectName = topic.SubjectName,
                    TopicName = topic.TopicName,
                    TopicCode = topic.TopicCode,
                    TopicParentId = topic.TopicParentId,
                    ParentTopicName = topic.ParentTopicName,
                    IsActive = topic.IsActive,
                    CreatedAt = topic.CreatedAt,
                    UpdatedAt = topic.UpdatedAt
                },
                Status = HttpStatusCode.Created
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create topic");
            return new HttpOperationResult<Admin.TopicDetailsDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to create topic"
            };
        }
    }

    public async Task<HttpOperationResult<Admin.TopicDetailsDto>> UpdateTopicAsync(long topicId, Admin.UpdateTopicRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            // First get current topic to know subject_id if not provided
            var allTopics = await GetAllTopicsAsync(null, cancellationToken);
            var currentTopic = allTopics.Result?.FirstOrDefault(t => t.Id == topicId);
            
            if (currentTopic == null)
            {
                return new HttpOperationResult<Admin.TopicDetailsDto>
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "Topic not found"
                };
            }

            var subjectId = request.SubjectId ?? currentTopic.SubjectId;

            var parameters = new List<NpgsqlParameter>
            {
                new("topic_id", NpgsqlDbType.Bigint) { Value = topicId },
                new("subject_id", NpgsqlDbType.Bigint) { Value = (object?)subjectId ?? DBNull.Value },
                new("topic_name", NpgsqlDbType.Varchar) { Value = (object?)request.TopicName ?? DBNull.Value },
                new("topic_code", NpgsqlDbType.Varchar) { Value = (object?)request.TopicCode ?? DBNull.Value },
                new("topic_parent_id", NpgsqlDbType.Bigint) { Value = (object?)request.TopicParentId ?? DBNull.Value },
                new("is_active", NpgsqlDbType.Boolean) { Value = (object?)request.IsActive ?? DBNull.Value }
            };

            var updatedId = await _sqlExecutor.QuerySingleAsync(
                _updateTopicQuery,
                reader => reader.GetInt64(0),
                parameters,
                cancellationToken);

            if (updatedId == 0)
            {
                return new HttpOperationResult<Admin.TopicDetailsDto>
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "Topic not found"
                };
            }

            var topics = await GetAllTopicsAsync(null, cancellationToken);
            var topic = topics.Result?.FirstOrDefault(t => t.Id == topicId);
            
            if (topic == null)
            {
                return new HttpOperationResult<Admin.TopicDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Topic updated but failed to retrieve details"
                };
            }

            return new HttpOperationResult<Admin.TopicDetailsDto>
            {
                Result = new Admin.TopicDetailsDto
                {
                    Id = topic.Id,
                    SubjectId = topic.SubjectId,
                    SubjectName = topic.SubjectName,
                    TopicName = topic.TopicName,
                    TopicCode = topic.TopicCode,
                    TopicParentId = topic.TopicParentId,
                    ParentTopicName = topic.ParentTopicName,
                    IsActive = topic.IsActive,
                    CreatedAt = topic.CreatedAt,
                    UpdatedAt = topic.UpdatedAt
                },
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update topic {TopicId}", topicId);
            return new HttpOperationResult<Admin.TopicDetailsDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to update topic"
            };
        }
    }

    public async Task<HttpOperationResult> DeleteTopicAsync(long topicId, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new[]
            {
                new NpgsqlParameter("topic_id", NpgsqlDbType.Bigint) { Value = topicId }
            };

            var affectedRows = await _sqlExecutor.ExecuteAsync(_deleteTopicQuery, parameters, cancellationToken);

            if (affectedRows == 0)
            {
                return new HttpOperationResult
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "Topic not found"
                };
            }

            return new HttpOperationResult
            {
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete topic {TopicId}", topicId);
            return new HttpOperationResult
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to delete topic. Check for dependencies (child topics, tasks)."
            };
        }
    }

    // Tasks
    public async Task<HttpOperationResult<IReadOnlyCollection<Tasks.TaskListItemDto>>> GetAllTasksAsync(Admin.TaskFilterDto? filter, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new List<NpgsqlParameter>
            {
                new("subject_id", NpgsqlDbType.Bigint) { Value = (object?)filter?.SubjectId ?? DBNull.Value },
                new("topic_id", NpgsqlDbType.Bigint) { Value = (object?)filter?.TopicId ?? DBNull.Value },
                new("task_type", NpgsqlDbType.Varchar) { Value = (object?)filter?.TaskType ?? DBNull.Value },
                new("difficulty", NpgsqlDbType.Smallint) { Value = (object?)filter?.Difficulty ?? DBNull.Value },
                new("is_active", NpgsqlDbType.Boolean) { Value = (object?)filter?.IsActive ?? DBNull.Value },
                new("search", NpgsqlDbType.Varchar) { Value = (object?)filter?.Search ?? DBNull.Value }
            };

            var tasks = await _sqlExecutor.QueryAsync(
                _getAllTasksQuery,
                reader =>
                {
                    return new Tasks.TaskListItemDto
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("id")),
                        SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                        TopicId = reader.IsDBNull(reader.GetOrdinal("topic_id")) ? null : reader.GetInt64(reader.GetOrdinal("topic_id")),
                        TopicName = reader.IsDBNull(reader.GetOrdinal("topic_name")) ? null : reader.GetString(reader.GetOrdinal("topic_name")),
                        TopicCode = null, // TopicCode не в запросе GetAllTasks
                        TaskType = reader.GetString(reader.GetOrdinal("task_type")),
                        Difficulty = reader.GetInt16(reader.GetOrdinal("difficulty")),
                        Statement = reader.GetString(reader.GetOrdinal("statement")),
                        CorrectAnswer = reader.IsDBNull(reader.GetOrdinal("correct_answer")) ? null : reader.GetString(reader.GetOrdinal("correct_answer")),
                        Explanation = reader.IsDBNull(reader.GetOrdinal("explanation")) ? null : reader.GetString(reader.GetOrdinal("explanation"))
                    };
                },
                parameters,
                cancellationToken);

            return new HttpOperationResult<IReadOnlyCollection<Tasks.TaskListItemDto>>
            {
                Result = tasks,
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all tasks");
            return new HttpOperationResult<IReadOnlyCollection<Tasks.TaskListItemDto>>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to retrieve tasks"
            };
        }
    }

    public async Task<HttpOperationResult<Admin.TaskDetailsDto>> CreateTaskAsync(Tasks.CreateTaskRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            // Валидация
            if (request.Difficulty < 1 || request.Difficulty > 5)
            {
                return new HttpOperationResult<Admin.TaskDetailsDto>
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = "Difficulty must be between 1 and 5"
                };
            }

            var validTaskTypes = new[] { "numeric", "text" };
            if (!validTaskTypes.Contains(request.TaskType.ToLower()))
            {
                return new HttpOperationResult<Admin.TaskDetailsDto>
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = $"Task type must be one of: {string.Join(", ", validTaskTypes)}"
                };
            }

            // Используем существующий репозиторий для создания
            var newTask = new CTH.Database.Entities.Public.TaskItem
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

            // Получаем созданное задание
            var createdTask = await _taskRepository.GetTaskByIdAsync(taskId, cancellationToken);
            
            if (createdTask == null)
            {
                return new HttpOperationResult<Admin.TaskDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Task created but failed to retrieve details"
                };
            }

            // Получаем название предмета и темы
            var subjects = await GetAllSubjectsAsync(cancellationToken);
            var subject = subjects.Result?.FirstOrDefault(s => s.Id == createdTask.SubjectId);
            var subjectName = subject?.SubjectName ?? string.Empty;

            string? topicName = null;
            if (createdTask.TopicId.HasValue)
            {
                var topics = await GetAllTopicsAsync(createdTask.SubjectId, cancellationToken);
                var topic = topics.Result?.FirstOrDefault(t => t.Id == createdTask.TopicId.Value);
                topicName = topic?.TopicName;
            }

            return new HttpOperationResult<Admin.TaskDetailsDto>
            {
                Result = new Admin.TaskDetailsDto
                {
                    Id = createdTask.Id,
                    SubjectId = createdTask.SubjectId,
                    SubjectName = subjectName,
                    TopicId = createdTask.TopicId,
                    TopicName = topicName,
                    TaskType = createdTask.TaskType,
                    Difficulty = createdTask.Difficulty,
                    Statement = createdTask.Statement,
                    CorrectAnswer = createdTask.CorrectAnswer,
                    Explanation = createdTask.Explanation,
                    IsActive = createdTask.IsActive,
                    CreatedAt = createdTask.CreatedAt.DateTime,
                    UpdatedAt = createdTask.UpdatedAt.DateTime
                },
                Status = HttpStatusCode.Created
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task");
            return new HttpOperationResult<Admin.TaskDetailsDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to create task"
            };
        }
    }

    public async Task<HttpOperationResult<Admin.TaskDetailsDto>> UpdateTaskAsync(long taskId, Tasks.UpdateTaskRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            // Получаем текущее задание
            var currentTask = await _taskRepository.GetTaskByIdAsync(taskId, cancellationToken);
            if (currentTask == null)
            {
                return new HttpOperationResult<Admin.TaskDetailsDto>
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "Task not found"
                };
            }

            // Валидация difficulty если указана
            if (request.Difficulty.HasValue && (request.Difficulty < 1 || request.Difficulty > 5))
            {
                return new HttpOperationResult<Admin.TaskDetailsDto>
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = "Difficulty must be between 1 and 5"
                };
            }

            // Валидация task_type если указан
            if (!string.IsNullOrWhiteSpace(request.TaskType))
            {
                var validTaskTypes = new[] { "numeric", "text" };
                if (!validTaskTypes.Contains(request.TaskType.ToLower()))
                {
                    return new HttpOperationResult<Admin.TaskDetailsDto>
                    {
                        Status = HttpStatusCode.BadRequest,
                        Error = $"Task type must be one of: {string.Join(", ", validTaskTypes)}"
                    };
                }
            }

            // Обновляем задание через репозиторий
            var updatedTask = new CTH.Database.Entities.Public.TaskItem
            {
                Id = taskId,
                TopicId = request.TopicId ?? currentTask.TopicId,
                TaskType = request.TaskType ?? currentTask.TaskType,
                Difficulty = request.Difficulty ?? currentTask.Difficulty,
                Statement = request.Statement ?? currentTask.Statement,
                CorrectAnswer = request.CorrectAnswer ?? currentTask.CorrectAnswer,
                Explanation = request.Explanation ?? currentTask.Explanation,
                IsActive = request.IsActive ?? currentTask.IsActive
            };

            await _taskRepository.UpdateAsync(updatedTask, cancellationToken);

            // Получаем обновленное задание
            var task = await _taskRepository.GetTaskByIdAsync(taskId, cancellationToken);
            if (task == null)
            {
                return new HttpOperationResult<Admin.TaskDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Task updated but failed to retrieve details"
                };
            }

            // Получаем название предмета и темы
            var subjects = await GetAllSubjectsAsync(cancellationToken);
            var subject = subjects.Result?.FirstOrDefault(s => s.Id == task.SubjectId);
            var subjectName = subject?.SubjectName ?? string.Empty;

            string? topicName = null;
            if (task.TopicId.HasValue)
            {
                var topics = await GetAllTopicsAsync(task.SubjectId, cancellationToken);
                var topic = topics.Result?.FirstOrDefault(t => t.Id == task.TopicId.Value);
                topicName = topic?.TopicName;
            }

            return new HttpOperationResult<Admin.TaskDetailsDto>
            {
                Result = new Admin.TaskDetailsDto
                {
                    Id = task.Id,
                    SubjectId = task.SubjectId,
                    SubjectName = subjectName,
                    TopicId = task.TopicId,
                    TopicName = topicName,
                    TaskType = task.TaskType,
                    Difficulty = task.Difficulty,
                    Statement = task.Statement,
                    CorrectAnswer = task.CorrectAnswer,
                    Explanation = task.Explanation,
                    IsActive = task.IsActive,
                    CreatedAt = task.CreatedAt.DateTime,
                    UpdatedAt = task.UpdatedAt.DateTime
                },
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update task {TaskId}", taskId);
            return new HttpOperationResult<Admin.TaskDetailsDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to update task"
            };
        }
    }

    public async Task<HttpOperationResult> ActivateTaskAsync(long taskId, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new[]
            {
                new NpgsqlParameter("task_id", NpgsqlDbType.Bigint) { Value = taskId }
            };

            var updatedId = await _sqlExecutor.QuerySingleAsync(
                _activateTaskQuery,
                reader => reader.GetInt64(0),
                parameters,
                cancellationToken);

            if (updatedId == 0)
            {
                return new HttpOperationResult
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "Task not found"
                };
            }

            return new HttpOperationResult
            {
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate task {TaskId}", taskId);
            return new HttpOperationResult
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to activate task"
            };
        }
    }

    public async Task<HttpOperationResult> DeactivateTaskAsync(long taskId, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new[]
            {
                new NpgsqlParameter("task_id", NpgsqlDbType.Bigint) { Value = taskId }
            };

            var updatedId = await _sqlExecutor.QuerySingleAsync(
                _deactivateTaskQuery,
                reader => reader.GetInt64(0),
                parameters,
                cancellationToken);

            if (updatedId == 0)
            {
                return new HttpOperationResult
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "Task not found"
                };
            }

            return new HttpOperationResult
            {
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate task {TaskId}", taskId);
            return new HttpOperationResult
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to deactivate task"
            };
        }
    }

    public async Task<HttpOperationResult> DeleteTaskAsync(long taskId, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new[]
            {
                new NpgsqlParameter("task_id", NpgsqlDbType.Bigint) { Value = taskId }
            };

            var affectedRows = await _sqlExecutor.ExecuteAsync(_deleteTaskQuery, parameters, cancellationToken);

            if (affectedRows == 0)
            {
                return new HttpOperationResult
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "Task not found"
                };
            }

            return new HttpOperationResult
            {
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete task {TaskId}", taskId);
            return new HttpOperationResult
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to delete task. Check for dependencies (tests using this task)."
            };
        }
    }

    // Tests
    public async Task<HttpOperationResult<IReadOnlyCollection<Admin.TestListItemDto>>> GetAllTestsAsync(Admin.TestFilterDto? filter, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new List<NpgsqlParameter>
            {
                new("subject_id", NpgsqlDbType.Bigint) { Value = (object?)filter?.SubjectId ?? DBNull.Value },
                new("test_kind", NpgsqlDbType.Varchar) { Value = (object?)filter?.TestKind ?? DBNull.Value },
                new("author_id", NpgsqlDbType.Bigint) { Value = (object?)filter?.AuthorId ?? DBNull.Value },
                new("is_published", NpgsqlDbType.Boolean) { Value = (object?)filter?.IsPublished ?? DBNull.Value },
                new("is_state_archive", NpgsqlDbType.Boolean) { Value = (object?)filter?.IsStateArchive ?? DBNull.Value },
                new("search", NpgsqlDbType.Varchar) { Value = (object?)filter?.Search ?? DBNull.Value }
            };

            var tests = await _sqlExecutor.QueryAsync(
                _getAllTestsQuery,
                reader => new Admin.TestListItemDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    SubjectId = reader.GetInt64(reader.GetOrdinal("subject_id")),
                    SubjectName = reader.GetString(reader.GetOrdinal("subject_name")),
                    TestKind = reader.GetString(reader.GetOrdinal("test_kind")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    AuthorId = reader.IsDBNull(reader.GetOrdinal("author_id")) ? null : reader.GetInt64(reader.GetOrdinal("author_id")),
                    AuthorName = reader.IsDBNull(reader.GetOrdinal("author_name")) ? null : reader.GetString(reader.GetOrdinal("author_name")),
                    IsPublished = reader.GetBoolean(reader.GetOrdinal("is_published")),
                    IsPublic = reader.GetBoolean(reader.GetOrdinal("is_public")),
                    IsStateArchive = reader.GetBoolean(reader.GetOrdinal("is_state_archive")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                },
                parameters,
                cancellationToken);

            return new HttpOperationResult<IReadOnlyCollection<Admin.TestListItemDto>>
            {
                Result = tests,
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all tests");
            return new HttpOperationResult<IReadOnlyCollection<Admin.TestListItemDto>>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to retrieve tests"
            };
        }
    }

    public async Task<HttpOperationResult<TestDetailsDto>> CreateTestAsync(CreateTestRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            // Админ может создавать любые тесты, включая state archive
            // Для state archive тестов author_id = null, для обычных тоже null (админ создает от имени системы)
            var newTest = new CTH.Database.Entities.Public.Test
            {
                SubjectId = request.SubjectId,
                TestKind = request.TestKind,
                Title = request.Title,
                AuthorId = null, // Админ создает тесты от имени системы
                TimeLimitSec = request.TimeLimitSec,
                AttemptsAllowed = request.AttemptsAllowed,
                Mode = request.Mode,
                IsPublished = request.IsPublished,
                IsPublic = request.IsStateArchive || request.IsPublic, // State archive тесты всегда публичные
                IsStateArchive = request.IsStateArchive
            };

            var testId = await _testRepository.CreateAsync(newTest, cancellationToken);

            // Добавляем задания в тест
            if (request.Tasks != null && request.Tasks.Count > 0)
            {
                var tasks = request.Tasks
                    .OrderBy(t => t.Position)
                    .Select(t => new CTH.Database.Entities.Public.TestTask
                    {
                        TestId = testId,
                        TaskId = t.TaskId,
                        Position = t.Position,
                        Weight = t.Weight
                    }).ToArray();

                await _testRepository.ReplaceTasksAsync(testId, tasks, cancellationToken);
            }

            // Получаем созданный тест
            var testDetails = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
            var testTasks = await _testRepository.GetTestTasksAsync(testId, cancellationToken);

            if (testDetails == null)
            {
                return new HttpOperationResult<TestDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Test created but failed to retrieve details"
                };
            }

            var dto = new TestDetailsDto
            {
                Id = testId,
                Title = testDetails.Title,
                TestKind = testDetails.TestKind,
                SubjectId = testDetails.SubjectId,
                TimeLimitSec = testDetails.TimeLimitSec,
                AttemptsAllowed = testDetails.AttemptsAllowed,
                Mode = testDetails.Mode,
                IsPublished = testDetails.IsPublished,
                IsPublic = testDetails.IsPublic,
                IsStateArchive = testDetails.IsStateArchive,
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

            return new HttpOperationResult<TestDetailsDto>
            {
                Result = dto,
                Status = HttpStatusCode.Created
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test");
            return new HttpOperationResult<TestDetailsDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to create test"
            };
        }
    }

    public async Task<HttpOperationResult<TestDetailsDto>> UpdateTestAsync(long testId, UpdateTestRequestDto request, CancellationToken cancellationToken)
    {
        try
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

            // Админ может обновлять любые тесты
            // Обновляем только те поля, которые можно изменить через UpdateTest
            var updatedTest = new CTH.Database.Entities.Public.Test
            {
                Id = testId,
                SubjectId = request.SubjectId,
                TestKind = request.TestKind,
                Title = request.Title,
                AuthorId = existing.AuthorId, // Сохраняем существующего автора (не обновляется через UpdateTest)
                TimeLimitSec = request.TimeLimitSec,
                AttemptsAllowed = request.AttemptsAllowed,
                Mode = request.Mode,
                IsPublished = request.IsPublished,
                IsPublic = request.IsStateArchive || request.IsPublic, // State archive тесты всегда публичные
                IsStateArchive = request.IsStateArchive
            };

            await _testRepository.UpdateAsync(updatedTest, cancellationToken);

            // Обновляем задания в тесте
            if (request.Tasks != null && request.Tasks.Count > 0)
            {
                var tasks = request.Tasks
                    .OrderBy(t => t.Position)
                    .Select(t => new CTH.Database.Entities.Public.TestTask
                    {
                        TestId = testId,
                        TaskId = t.TaskId,
                        Position = t.Position,
                        Weight = t.Weight
                    }).ToArray();

                await _testRepository.ReplaceTasksAsync(testId, tasks, cancellationToken);
            }

            // Получаем обновленный тест
            var testDetails = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
            var testTasks = await _testRepository.GetTestTasksAsync(testId, cancellationToken);

            if (testDetails == null)
            {
                return new HttpOperationResult<TestDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Test updated but failed to retrieve details"
                };
            }

            var dto = new TestDetailsDto
            {
                Id = testId,
                Title = testDetails.Title,
                TestKind = testDetails.TestKind,
                SubjectId = testDetails.SubjectId,
                TimeLimitSec = testDetails.TimeLimitSec,
                AttemptsAllowed = testDetails.AttemptsAllowed,
                Mode = testDetails.Mode,
                IsPublished = testDetails.IsPublished,
                IsPublic = testDetails.IsPublic,
                IsStateArchive = testDetails.IsStateArchive,
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

            return new HttpOperationResult<TestDetailsDto>
            {
                Result = dto,
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update test {TestId}", testId);
            return new HttpOperationResult<TestDetailsDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to update test"
            };
        }
    }

    public async Task<HttpOperationResult> DeleteTestAsync(long testId, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
            if (existing == null)
            {
                return new HttpOperationResult
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "Test not found"
                };
            }

            await _testRepository.DeleteAsync(testId, cancellationToken);

            return new HttpOperationResult
            {
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete test {TestId}", testId);
            return new HttpOperationResult
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to delete test. Check for dependencies (attempts, assignments)."
            };
        }
    }

    // Invitation Codes
    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Исключаем похожие символы (0, O, I, 1)
    private const int CodeLength = 32; // XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX = 32 символа

    public async Task<HttpOperationResult<IReadOnlyCollection<Admin.InvitationCodeListItemDto>>> GetAllInvitationCodesAsync(long? teacherId, string? status, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new List<NpgsqlParameter>
            {
                new("teacher_id", NpgsqlDbType.Bigint) { Value = (object?)teacherId ?? DBNull.Value },
                new("status", NpgsqlDbType.Varchar) { Value = (object?)status ?? DBNull.Value }
            };

            var codes = await _sqlExecutor.QueryAsync(
                _getAllInvitationCodesQuery,
                reader => new Admin.InvitationCodeListItemDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    TeacherId = reader.GetInt64(reader.GetOrdinal("teacher_id")),
                    TeacherName = reader.GetString(reader.GetOrdinal("teacher_name")),
                    TeacherEmail = reader.GetString(reader.GetOrdinal("teacher_email")),
                    Code = reader.GetString(reader.GetOrdinal("code")),
                    MaxUses = reader.IsDBNull(reader.GetOrdinal("max_uses")) ? null : reader.GetInt32(reader.GetOrdinal("max_uses")),
                    UsedCount = reader.GetInt32(reader.GetOrdinal("used_count")),
                    ExpiresAt = reader.IsDBNull(reader.GetOrdinal("expires_at")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("expires_at")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                },
                parameters,
                cancellationToken);

            return new HttpOperationResult<IReadOnlyCollection<Admin.InvitationCodeListItemDto>>
            {
                Result = codes,
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all invitation codes");
            return new HttpOperationResult<IReadOnlyCollection<Admin.InvitationCodeListItemDto>>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to retrieve invitation codes"
            };
        }
    }

    public async Task<HttpOperationResult<Admin.InvitationCodeDetailsDto>> CreateInvitationCodeAsync(Admin.CreateInvitationCodeRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            // Проверяем, что учитель существует
            var allUsers = await GetAllUsersAsync(cancellationToken);
            var teacher = allUsers.Result?.FirstOrDefault(u => u.Id == request.TeacherId);
            if (teacher == null)
            {
                return new HttpOperationResult<Admin.InvitationCodeDetailsDto>
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "Teacher not found"
                };
            }

            // Генерируем уникальный код
            string code;
            int attempts = 0;
            const int maxAttempts = 10;

            do
            {
                code = GenerateInvitationCode();
                var existing = await _invitationCodeRepository.GetByCodeAsync(code, cancellationToken);
                if (existing == null)
                {
                    break;
                }
                attempts++;
            } while (attempts < maxAttempts);

            if (attempts >= maxAttempts)
            {
                return new HttpOperationResult<Admin.InvitationCodeDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Failed to generate unique invitation code"
                };
            }

            // Форматируем код как GUID: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
            code = FormatCode(code);

            var parameters = new List<NpgsqlParameter>
            {
                new("teacher_id", NpgsqlDbType.Bigint) { Value = request.TeacherId },
                new("code", NpgsqlDbType.Varchar) { Value = code },
                new("max_uses", NpgsqlDbType.Integer) { Value = (object?)request.MaxUses ?? DBNull.Value },
                new("expires_at", NpgsqlDbType.TimestampTz) { Value = (object?)request.ExpiresAt ?? DBNull.Value },
                new("status", NpgsqlDbType.Varchar) { Value = "active" }
            };

            var createdId = await _sqlExecutor.QuerySingleAsync(
                _createInvitationCodeQuery,
                reader => reader.GetInt64(0),
                parameters,
                cancellationToken);

            if (createdId == 0)
            {
                return new HttpOperationResult<Admin.InvitationCodeDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Failed to create invitation code"
                };
            }

            // Получаем созданный код
            var codes = await GetAllInvitationCodesAsync(null, null, cancellationToken);
            var createdCode = codes.Result?.FirstOrDefault(c => c.Id == createdId);

            if (createdCode == null)
            {
                return new HttpOperationResult<Admin.InvitationCodeDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Invitation code created but failed to retrieve details"
                };
            }

            return new HttpOperationResult<Admin.InvitationCodeDetailsDto>
            {
                Result = new Admin.InvitationCodeDetailsDto
                {
                    Id = createdCode.Id,
                    TeacherId = createdCode.TeacherId,
                    TeacherName = createdCode.TeacherName,
                    TeacherEmail = createdCode.TeacherEmail,
                    Code = createdCode.Code,
                    MaxUses = createdCode.MaxUses,
                    UsedCount = createdCode.UsedCount,
                    ExpiresAt = createdCode.ExpiresAt,
                    Status = createdCode.Status,
                    CreatedAt = createdCode.CreatedAt
                },
                Status = HttpStatusCode.Created
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invitation code");
            return new HttpOperationResult<Admin.InvitationCodeDetailsDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to create invitation code"
            };
        }
    }

    public async Task<HttpOperationResult<Admin.InvitationCodeDetailsDto>> UpdateInvitationCodeAsync(long invitationCodeId, Admin.UpdateInvitationCodeRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new List<NpgsqlParameter>
            {
                new("id", NpgsqlDbType.Bigint) { Value = invitationCodeId },
                new("max_uses", NpgsqlDbType.Integer) { Value = (object?)request.MaxUses ?? DBNull.Value },
                new("expires_at", NpgsqlDbType.TimestampTz) { Value = (object?)request.ExpiresAt ?? DBNull.Value },
                new("status", NpgsqlDbType.Varchar) { Value = (object?)request.Status ?? DBNull.Value }
            };

            var updatedId = await _sqlExecutor.QuerySingleAsync(
                _updateInvitationCodeQuery,
                reader => reader.GetInt64(0),
                parameters,
                cancellationToken);

            if (updatedId == 0)
            {
                return new HttpOperationResult<Admin.InvitationCodeDetailsDto>
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "Invitation code not found"
                };
            }

            // Получаем обновленный код
            var codes = await GetAllInvitationCodesAsync(null, null, cancellationToken);
            var updatedCode = codes.Result?.FirstOrDefault(c => c.Id == invitationCodeId);

            if (updatedCode == null)
            {
                return new HttpOperationResult<Admin.InvitationCodeDetailsDto>
                {
                    Status = HttpStatusCode.InternalServerError,
                    Error = "Invitation code updated but failed to retrieve details"
                };
            }

            return new HttpOperationResult<Admin.InvitationCodeDetailsDto>
            {
                Result = new Admin.InvitationCodeDetailsDto
                {
                    Id = updatedCode.Id,
                    TeacherId = updatedCode.TeacherId,
                    TeacherName = updatedCode.TeacherName,
                    TeacherEmail = updatedCode.TeacherEmail,
                    Code = updatedCode.Code,
                    MaxUses = updatedCode.MaxUses,
                    UsedCount = updatedCode.UsedCount,
                    ExpiresAt = updatedCode.ExpiresAt,
                    Status = updatedCode.Status,
                    CreatedAt = updatedCode.CreatedAt
                },
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update invitation code {InvitationCodeId}", invitationCodeId);
            return new HttpOperationResult<Admin.InvitationCodeDetailsDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to update invitation code"
            };
        }
    }

    public async Task<HttpOperationResult> DeleteInvitationCodeAsync(long invitationCodeId, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new[]
            {
                new NpgsqlParameter("id", NpgsqlDbType.Bigint) { Value = invitationCodeId }
            };

            var affectedRows = await _sqlExecutor.ExecuteAsync(_deleteInvitationCodeQuery, parameters, cancellationToken);

            if (affectedRows == 0)
            {
                return new HttpOperationResult
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "Invitation code not found"
                };
            }

            return new HttpOperationResult
            {
                Status = HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete invitation code {InvitationCodeId}", invitationCodeId);
            return new HttpOperationResult
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to delete invitation code"
            };
        }
    }

    private static string GenerateInvitationCode()
    {
        var random = RandomNumberGenerator.GetBytes(CodeLength);
        var code = new StringBuilder(CodeLength);

        for (int i = 0; i < CodeLength; i++)
        {
            code.Append(CodeChars[random[i] % CodeChars.Length]);
        }

        return code.ToString();
    }

    private static string FormatCode(string code)
    {
        // Форматируем как GUID: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
        if (code.Length == CodeLength)
        {
            return $"{code[0..8]}-{code[8..12]}-{code[12..16]}-{code[16..20]}-{code[20..32]}";
        }
        return code;
    }

}

