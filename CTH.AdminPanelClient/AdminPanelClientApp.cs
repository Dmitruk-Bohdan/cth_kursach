using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTH.Common.Enums;
using static CTH.AdminPanelClient.ApiClient;

namespace CTH.AdminPanelClient;

public sealed class AdminPanelClientApp : IDisposable
{
    private readonly ApiClient _apiClient;
    private readonly CancellationTokenSource _cts = new();

    public AdminPanelClientApp(string baseUrl)
    {
        _apiClient = new ApiClient(baseUrl);
    }

    public async Task RunAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            if (_apiClient.IsAuthenticated)
            {
                await ShowMainMenuAsync();
            }
            else
            {
                await ShowAuthMenuAsync();
            }
        }
    }

    private async Task ShowAuthMenuAsync()
    {
        Console.WriteLine("=== Admin Panel ===");
        Console.WriteLine("1) Login");
        Console.WriteLine("0) Exit");
        Console.Write("Select option: ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await LoginAsync();
                break;
            case "0":
                _cts.Cancel();
                break;
            default:
                Console.WriteLine("Unknown option.");
                break;
        }

        Console.WriteLine();
    }

    private async Task LoginAsync()
    {
        Console.Write("Email: ");
        var email = Console.ReadLine();
        Console.Write("Password: ");
        var password = ReadPassword();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Email and password are required.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Logging in...");

        var result = await _apiClient.LoginAsync(email, password, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine();
            return;
        }

        if (_apiClient.UserRoleId == null)
        {
            Console.WriteLine("Error: Could not determine user role.");
            await _apiClient.LogoutAsync(_cts.Token);
            Console.WriteLine();
            return;
        }

        if (_apiClient.UserRoleId != (int)RoleTypeEnum.Admin)
        {
            var roleName = GetRoleName(_apiClient.UserRoleId.Value);
            Console.WriteLine($"Access denied. Only admins can use this client. Your role: {roleName}");
            await _apiClient.LogoutAsync(_cts.Token);
            Console.WriteLine();
            return;
        }

        Console.WriteLine($"Welcome, {result.Value!.UserName}!");
        Console.WriteLine();
    }

    private async Task ShowMainMenuAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            Console.WriteLine("=== Admin Panel ===");
            Console.WriteLine("1) Manage users");
            Console.WriteLine("2) Manage subjects");
            Console.WriteLine("3) Manage topics");
            Console.WriteLine("4) Manage tasks");
            Console.WriteLine("5) Manage tests");
            Console.WriteLine("6) Logout");
            Console.WriteLine("0) Exit");
            Console.Write("Select option: ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await ManageUsersAsync();
                    break;
                case "2":
                    await ManageSubjectsAsync();
                    break;
                case "3":
                    await ManageTopicsAsync();
                    break;
                case "4":
                    await ManageTasksAsync();
                    break;
                case "5":
                    await ManageTestsAsync();
                    break;
                case "6":
                    await ManageInvitationCodesAsync();
                    break;
                case "7":
                    await LogoutAsync();
                    return;
                case "0":
                    _cts.Cancel();
                    return;
                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }

            Console.WriteLine();
        }
    }

    private async Task ManageUsersAsync()
    {
        Console.WriteLine("=== Manage Users ===");
        Console.WriteLine("1) List users");
        Console.WriteLine("2) Create user");
        Console.WriteLine("3) Edit user");
        Console.WriteLine("4) Block user");
        Console.WriteLine("5) Unblock user");
        Console.WriteLine("6) Delete user");
        Console.WriteLine("0) Back");
        Console.Write("Select option: ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await ListUsersAsync();
                break;
            case "2":
                await CreateUserAsync();
                break;
            case "3":
                await EditUserAsync();
                break;
            case "4":
                await BlockUserAsync();
                break;
            case "5":
                await UnblockUserAsync();
                break;
            case "6":
                await DeleteUserAsync();
                break;
            case "0":
                return;
            default:
                Console.WriteLine("Unknown option.");
                break;
        }
    }

    private async Task ListUsersAsync()
    {
        Console.WriteLine("=== List Users ===");
        Console.WriteLine("Loading users...");
        
        var result = await _apiClient.GetAllUsersAsync(_cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        var users = result.Value!;
        if (users.Count == 0)
        {
            Console.WriteLine("No users found.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Found {users.Count} user(s):");
        Console.WriteLine(new string('-', 100));
        foreach (var user in users)
        {
            var lastLogin = user.LastLoginAt.HasValue ? user.LastLoginAt.Value.ToString("yyyy-MM-dd HH:mm") : "Never";
            Console.WriteLine($"ID: {user.Id} | {user.UserName} ({user.Email})");
            Console.WriteLine($"  Role: {user.RoleName} | Last login: {lastLogin} | Created: {user.CreatedAt:yyyy-MM-dd HH:mm}");
            Console.WriteLine();
        }
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task CreateUserAsync()
    {
        Console.WriteLine("=== Create User ===");
        
        Console.Write("User name: ");
        var userName = Console.ReadLine()?.Trim();
        Console.Write("Email: ");
        var email = Console.ReadLine()?.Trim();
        Console.Write("Password: ");
        var password = ReadPassword();
        
        Console.WriteLine("Select role:");
        Console.WriteLine($"1) Student ({(int)RoleTypeEnum.Student})");
        Console.WriteLine($"2) Teacher ({(int)RoleTypeEnum.Teacher})");
        Console.WriteLine($"3) Admin ({(int)RoleTypeEnum.Admin})");
        Console.Write("Enter role number: ");
        var roleInput = Console.ReadLine();
        Console.WriteLine();

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Error: User name, email and password are required.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        if (!int.TryParse(roleInput, out var roleNumber) || roleNumber < 1 || roleNumber > 3)
        {
            Console.WriteLine("Error: Invalid role number.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        var roleTypeId = roleNumber switch
        {
            1 => (int)RoleTypeEnum.Student,
            2 => (int)RoleTypeEnum.Teacher,
            3 => (int)RoleTypeEnum.Admin,
            _ => (int)RoleTypeEnum.Student
        };

        Console.WriteLine("Creating user...");
        var request = new CreateUserRequest
        {
            UserName = userName,
            Email = email,
            Password = password,
            RoleTypeId = roleTypeId
        };

        var result = await _apiClient.CreateUserAsync(request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine($"User created successfully! ID: {result.Value!.Id}");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task EditUserAsync()
    {
        Console.WriteLine("=== Edit User ===");
        Console.Write("Enter user ID: ");
        var userIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(userIdInput, out var userId))
        {
            Console.WriteLine("Error: Invalid user ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Leave field empty to keep current value.");
        Console.Write("User name: ");
        var userName = Console.ReadLine()?.Trim();
        Console.Write("Email: ");
        var email = Console.ReadLine()?.Trim();
        Console.Write("Password (leave empty to keep current): ");
        var password = ReadPassword();
        Console.Write("Role Type ID (1=Student, 2=Teacher, 3=Admin, leave empty to keep current): ");
        var roleInput = Console.ReadLine()?.Trim();
        Console.WriteLine();

        var request = new UpdateUserRequest
        {
            UserName = string.IsNullOrWhiteSpace(userName) ? null : userName,
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            Password = string.IsNullOrWhiteSpace(password) ? null : password,
            RoleTypeId = string.IsNullOrWhiteSpace(roleInput) || !int.TryParse(roleInput, out var roleId) ? null : roleId
        };

        Console.WriteLine("Updating user...");
        var result = await _apiClient.UpdateUserAsync(userId, request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("User updated successfully!");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task BlockUserAsync()
    {
        Console.WriteLine("=== Block User ===");
        Console.Write("Enter user ID: ");
        var userIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(userIdInput, out var userId))
        {
            Console.WriteLine("Error: Invalid user ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Blocking user...");
        var result = await _apiClient.BlockUserAsync(userId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("User blocked successfully (all sessions revoked).");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task UnblockUserAsync()
    {
        Console.WriteLine("=== Unblock User ===");
        Console.Write("Enter user ID: ");
        var userIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(userIdInput, out var userId))
        {
            Console.WriteLine("Error: Invalid user ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Unblocking user...");
        var result = await _apiClient.UnblockUserAsync(userId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("User unblocked successfully.");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task DeleteUserAsync()
    {
        Console.WriteLine("=== Delete User ===");
        Console.Write("Enter user ID: ");
        var userIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(userIdInput, out var userId))
        {
            Console.WriteLine("Error: Invalid user ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.Write("Are you sure you want to delete this user? (yes/no): ");
        var confirm = Console.ReadLine()?.Trim().ToLower();
        if (confirm != "yes")
        {
            Console.WriteLine("Deletion cancelled.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Deleting user...");
        var result = await _apiClient.DeleteUserAsync(userId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("User deleted successfully.");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task ManageSubjectsAsync()
    {
        Console.WriteLine("=== Manage Subjects ===");
        Console.WriteLine("1) List subjects");
        Console.WriteLine("2) Create subject");
        Console.WriteLine("3) Edit subject");
        Console.WriteLine("4) Delete subject");
        Console.WriteLine("0) Back");
        Console.Write("Select option: ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await ListSubjectsAsync();
                break;
            case "2":
                await CreateSubjectAsync();
                break;
            case "3":
                await EditSubjectAsync();
                break;
            case "4":
                await DeleteSubjectAsync();
                break;
            case "0":
                return;
            default:
                Console.WriteLine("Unknown option.");
                break;
        }
    }

    private async Task ListSubjectsAsync()
    {
        Console.WriteLine("=== List Subjects ===");
        Console.WriteLine("Loading subjects...");
        
        var result = await _apiClient.GetAllSubjectsAsync(_cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        var subjects = result.Value!;
        if (subjects.Count == 0)
        {
            Console.WriteLine("No subjects found.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Found {subjects.Count} subject(s):");
        Console.WriteLine(new string('-', 100));
        foreach (var subject in subjects)
        {
            var status = subject.IsActive ? "Active" : "Inactive";
            Console.WriteLine($"ID: {subject.Id} | {subject.SubjectName} ({subject.SubjectCode})");
            Console.WriteLine($"  Status: {status} | Created: {subject.CreatedAt:yyyy-MM-dd HH:mm}");
            Console.WriteLine();
        }
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task CreateSubjectAsync()
    {
        Console.WriteLine("=== Create Subject ===");
        
        Console.Write("Subject code: ");
        var subjectCode = Console.ReadLine()?.Trim();
        Console.Write("Subject name: ");
        var subjectName = Console.ReadLine()?.Trim();
        Console.Write("Is active? (y/n, default: y): ");
        var isActiveInput = Console.ReadLine()?.Trim().ToLower();
        var isActive = string.IsNullOrWhiteSpace(isActiveInput) || isActiveInput == "y";
        Console.WriteLine();

        if (string.IsNullOrWhiteSpace(subjectCode) || string.IsNullOrWhiteSpace(subjectName))
        {
            Console.WriteLine("Error: Subject code and name are required.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Creating subject...");
        var request = new CreateSubjectRequest
        {
            SubjectCode = subjectCode,
            SubjectName = subjectName,
            IsActive = isActive
        };

        var result = await _apiClient.CreateSubjectAsync(request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine($"Subject created successfully! ID: {result.Value!.Id}");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task EditSubjectAsync()
    {
        Console.WriteLine("=== Edit Subject ===");
        Console.Write("Enter subject ID: ");
        var subjectIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(subjectIdInput, out var subjectId))
        {
            Console.WriteLine("Error: Invalid subject ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Leave field empty to keep current value.");
        Console.Write("Subject code: ");
        var subjectCode = Console.ReadLine()?.Trim();
        Console.Write("Subject name: ");
        var subjectName = Console.ReadLine()?.Trim();
        Console.Write("Is active? (y/n, leave empty to keep current): ");
        var isActiveInput = Console.ReadLine()?.Trim().ToLower();
        bool? isActive = null;
        if (!string.IsNullOrWhiteSpace(isActiveInput))
        {
            isActive = isActiveInput == "y";
        }
        Console.WriteLine();

        var request = new UpdateSubjectRequest
        {
            SubjectCode = string.IsNullOrWhiteSpace(subjectCode) ? null : subjectCode,
            SubjectName = string.IsNullOrWhiteSpace(subjectName) ? null : subjectName,
            IsActive = isActive
        };

        Console.WriteLine("Updating subject...");
        var result = await _apiClient.UpdateSubjectAsync(subjectId, request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("Subject updated successfully!");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task DeleteSubjectAsync()
    {
        Console.WriteLine("=== Delete Subject ===");
        Console.Write("Enter subject ID: ");
        var subjectIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(subjectIdInput, out var subjectId))
        {
            Console.WriteLine("Error: Invalid subject ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.Write("Are you sure you want to delete this subject? (yes/no): ");
        var confirm = Console.ReadLine()?.Trim().ToLower();
        if (confirm != "yes")
        {
            Console.WriteLine("Deletion cancelled.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Deleting subject...");
        var result = await _apiClient.DeleteSubjectAsync(subjectId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("Subject deleted successfully.");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task ManageTopicsAsync()
    {
        Console.WriteLine("=== Manage Topics ===");
        Console.WriteLine("1) List topics");
        Console.WriteLine("2) Create topic");
        Console.WriteLine("3) Edit topic");
        Console.WriteLine("4) Delete topic");
        Console.WriteLine("0) Back");
        Console.Write("Select option: ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await ListTopicsAsync();
                break;
            case "2":
                await CreateTopicAsync();
                break;
            case "3":
                await EditTopicAsync();
                break;
            case "4":
                await DeleteTopicAsync();
                break;
            case "0":
                return;
            default:
                Console.WriteLine("Unknown option.");
                break;
        }
    }

    private async Task ListTopicsAsync()
    {
        Console.WriteLine("=== List Topics ===");
        Console.Write("Enter subject ID (or leave empty for all topics): ");
        var subjectIdInput = Console.ReadLine()?.Trim();
        long? subjectId = null;
        if (!string.IsNullOrWhiteSpace(subjectIdInput) && long.TryParse(subjectIdInput, out var id))
        {
            subjectId = id;
        }
        Console.WriteLine();

        Console.WriteLine("Loading topics...");
        var result = await _apiClient.GetAllTopicsAsync(subjectId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        var topics = result.Value!;
        if (topics.Count == 0)
        {
            Console.WriteLine("No topics found.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Found {topics.Count} topic(s):");
        Console.WriteLine(new string('-', 100));
        foreach (var topic in topics)
        {
            var status = topic.IsActive ? "Active" : "Inactive";
            var parentInfo = topic.TopicParentId.HasValue ? $" | Parent ID: {topic.TopicParentId.Value}" : "";
            Console.WriteLine($"ID: {topic.Id} | {topic.TopicName} ({topic.SubjectName})");
            Console.WriteLine($"  Code: {topic.TopicCode ?? "N/A"}{parentInfo} | Status: {status} | Created: {topic.CreatedAt:yyyy-MM-dd HH:mm}");
            Console.WriteLine();
        }
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task CreateTopicAsync()
    {
        Console.WriteLine("=== Create Topic ===");
        
        Console.Write("Subject ID: ");
        var subjectIdInput = Console.ReadLine()?.Trim();
        if (!long.TryParse(subjectIdInput, out var subjectId))
        {
            Console.WriteLine("Error: Invalid subject ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.Write("Topic name: ");
        var topicName = Console.ReadLine()?.Trim();
        Console.Write("Topic code (optional): ");
        var topicCode = Console.ReadLine()?.Trim();
        Console.Write("Parent topic ID (optional): ");
        var parentIdInput = Console.ReadLine()?.Trim();
        long? parentId = null;
        if (!string.IsNullOrWhiteSpace(parentIdInput) && long.TryParse(parentIdInput, out var pid))
        {
            parentId = pid;
        }
        Console.Write("Is active? (y/n, default: y): ");
        var isActiveInput = Console.ReadLine()?.Trim().ToLower();
        var isActive = string.IsNullOrWhiteSpace(isActiveInput) || isActiveInput == "y";
        Console.WriteLine();

        if (string.IsNullOrWhiteSpace(topicName))
        {
            Console.WriteLine("Error: Topic name is required.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Creating topic...");
        var request = new CreateTopicRequest
        {
            SubjectId = subjectId,
            TopicName = topicName,
            TopicCode = string.IsNullOrWhiteSpace(topicCode) ? null : topicCode,
            TopicParentId = parentId,
            IsActive = isActive
        };

        var result = await _apiClient.CreateTopicAsync(request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine($"Topic created successfully! ID: {result.Value!.Id}");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task EditTopicAsync()
    {
        Console.WriteLine("=== Edit Topic ===");
        Console.Write("Enter topic ID: ");
        var topicIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(topicIdInput, out var topicId))
        {
            Console.WriteLine("Error: Invalid topic ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Leave field empty to keep current value.");
        Console.Write("Subject ID: ");
        var subjectIdInput = Console.ReadLine()?.Trim();
        Console.Write("Topic name: ");
        var topicName = Console.ReadLine()?.Trim();
        Console.Write("Topic code: ");
        var topicCode = Console.ReadLine()?.Trim();
        Console.Write("Parent topic ID: ");
        var parentIdInput = Console.ReadLine()?.Trim();
        Console.Write("Is active? (y/n, leave empty to keep current): ");
        var isActiveInput = Console.ReadLine()?.Trim().ToLower();
        Console.WriteLine();

        var request = new UpdateTopicRequest
        {
            SubjectId = string.IsNullOrWhiteSpace(subjectIdInput) || !long.TryParse(subjectIdInput, out var sid) ? null : sid,
            TopicName = string.IsNullOrWhiteSpace(topicName) ? null : topicName,
            TopicCode = string.IsNullOrWhiteSpace(topicCode) ? null : topicCode,
            TopicParentId = string.IsNullOrWhiteSpace(parentIdInput) || !long.TryParse(parentIdInput, out var pid) ? null : pid,
            IsActive = string.IsNullOrWhiteSpace(isActiveInput) ? null : (isActiveInput == "y")
        };

        Console.WriteLine("Updating topic...");
        var result = await _apiClient.UpdateTopicAsync(topicId, request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("Topic updated successfully!");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task DeleteTopicAsync()
    {
        Console.WriteLine("=== Delete Topic ===");
        Console.Write("Enter topic ID: ");
        var topicIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(topicIdInput, out var topicId))
        {
            Console.WriteLine("Error: Invalid topic ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.Write("Are you sure you want to delete this topic? (yes/no): ");
        var confirm = Console.ReadLine()?.Trim().ToLower();
        if (confirm != "yes")
        {
            Console.WriteLine("Deletion cancelled.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Deleting topic...");
        var result = await _apiClient.DeleteTopicAsync(topicId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("Topic deleted successfully.");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task ManageTasksAsync()
    {
        Console.WriteLine("=== Manage Tasks ===");
        Console.WriteLine("1) List tasks");
        Console.WriteLine("2) Create task");
        Console.WriteLine("3) Edit task");
        Console.WriteLine("4) Activate task");
        Console.WriteLine("5) Deactivate task");
        Console.WriteLine("6) Delete task");
        Console.WriteLine("0) Back");
        Console.Write("Select option: ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await ListTasksAsync();
                break;
            case "2":
                await CreateTaskAsync();
                break;
            case "3":
                await EditTaskAsync();
                break;
            case "4":
                await ActivateTaskAsync();
                break;
            case "5":
                await DeactivateTaskAsync();
                break;
            case "6":
                await DeleteTaskAsync();
                break;
            case "0":
                return;
            default:
                Console.WriteLine("Unknown option.");
                break;
        }
    }

    private async Task ListTasksAsync()
    {
        Console.WriteLine("=== List Tasks ===");
        Console.WriteLine("Enter filter options (press Enter to skip and show all tasks):");
        Console.Write("Subject ID: ");
        var subjectIdInput = Console.ReadLine()?.Trim();
        Console.Write("Topic ID: ");
        var topicIdInput = Console.ReadLine()?.Trim();
        Console.Write("Task type (numeric/text, or Enter for all): ");
        var taskType = Console.ReadLine()?.Trim();
        Console.Write("Difficulty (1-5, or Enter for all): ");
        var difficultyInput = Console.ReadLine()?.Trim();
        Console.Write("Is active? (y/n, or Enter for all): ");
        var isActiveInput = Console.ReadLine()?.Trim().ToLower();
        Console.Write("Search query (text in statement, or Enter to skip): ");
        var search = Console.ReadLine()?.Trim();
        Console.WriteLine();

        var filter = new TaskFilterDto
        {
            SubjectId = string.IsNullOrWhiteSpace(subjectIdInput) || !long.TryParse(subjectIdInput, out var sid) ? null : sid,
            TopicId = string.IsNullOrWhiteSpace(topicIdInput) || !long.TryParse(topicIdInput, out var tid) ? null : tid,
            TaskType = string.IsNullOrWhiteSpace(taskType) ? null : taskType,
            Difficulty = string.IsNullOrWhiteSpace(difficultyInput) || !short.TryParse(difficultyInput, out var diff) ? null : diff,
            IsActive = string.IsNullOrWhiteSpace(isActiveInput) ? null : (isActiveInput == "y"),
            Search = string.IsNullOrWhiteSpace(search) ? null : search
        };

        Console.WriteLine("Loading tasks...");
        var result = await _apiClient.GetAllTasksAsync(filter, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        var tasks = result.Value!;
        if (tasks.Count == 0)
        {
            Console.WriteLine("No tasks found.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Found {tasks.Count} task(s):");
        Console.WriteLine(new string('-', 100));
        foreach (var task in tasks)
        {
            var topicInfo = task.TopicName != null ? $" | Topic: {task.TopicName}" : "";
            var status = task.Explanation != null ? "Has explanation" : "No explanation";
            Console.WriteLine($"ID: {task.Id} | Type: {task.TaskType} | Difficulty: {task.Difficulty}{topicInfo}");
            Console.WriteLine($"  Statement: {(task.Statement.Length > 80 ? task.Statement.Substring(0, 80) + "..." : task.Statement)}");
            Console.WriteLine($"  {status}");
            Console.WriteLine();
        }
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task CreateTaskAsync()
    {
        Console.WriteLine("=== Create Task ===");
        
        Console.Write("Subject ID: ");
        var subjectIdInput = Console.ReadLine()?.Trim();
        if (!long.TryParse(subjectIdInput, out var subjectId))
        {
            Console.WriteLine("Error: Invalid subject ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.Write("Topic ID (optional): ");
        var topicIdInput = Console.ReadLine()?.Trim();
        long? topicId = null;
        if (!string.IsNullOrWhiteSpace(topicIdInput) && long.TryParse(topicIdInput, out var tid))
        {
            topicId = tid;
        }

        Console.WriteLine("Task type:");
        Console.WriteLine("1) numeric");
        Console.WriteLine("2) text");
        Console.Write("Select type: ");
        var typeInput = Console.ReadLine()?.Trim();
        var taskType = typeInput == "1" ? "numeric" : typeInput == "2" ? "text" : null;
        if (taskType == null)
        {
            Console.WriteLine("Error: Invalid task type.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.Write("Difficulty (1-5): ");
        var difficultyInput = Console.ReadLine()?.Trim();
        if (!short.TryParse(difficultyInput, out var difficulty) || difficulty < 1 || difficulty > 5)
        {
            Console.WriteLine("Error: Difficulty must be between 1 and 5.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.Write("Statement: ");
        var statement = Console.ReadLine()?.Trim();
        Console.Write("Correct answer: ");
        var correctAnswer = Console.ReadLine()?.Trim();
        Console.Write("Explanation (optional): ");
        var explanation = Console.ReadLine()?.Trim();
        Console.Write("Is active? (y/n, default: y): ");
        var isActiveInput = Console.ReadLine()?.Trim().ToLower();
        var isActive = string.IsNullOrWhiteSpace(isActiveInput) || isActiveInput == "y";
        Console.WriteLine();

        if (string.IsNullOrWhiteSpace(statement) || string.IsNullOrWhiteSpace(correctAnswer))
        {
            Console.WriteLine("Error: Statement and correct answer are required.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Creating task...");
        var request = new CreateTaskRequest
        {
            SubjectId = subjectId,
            TopicId = topicId,
            TaskType = taskType,
            Difficulty = difficulty,
            Statement = statement,
            CorrectAnswer = correctAnswer,
            Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation,
            IsActive = isActive
        };

        var result = await _apiClient.CreateTaskAsync(request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine($"Task created successfully! ID: {result.Value!.Id}");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task EditTaskAsync()
    {
        Console.WriteLine("=== Edit Task ===");
        Console.Write("Enter task ID: ");
        var taskIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(taskIdInput, out var taskId))
        {
            Console.WriteLine("Error: Invalid task ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Leave field empty to keep current value.");
        Console.Write("Topic ID: ");
        var topicIdInput = Console.ReadLine()?.Trim();
        Console.Write("Task type (numeric/text): ");
        var taskType = Console.ReadLine()?.Trim();
        Console.Write("Difficulty (1-5): ");
        var difficultyInput = Console.ReadLine()?.Trim();
        Console.Write("Statement: ");
        var statement = Console.ReadLine()?.Trim();
        Console.Write("Correct answer: ");
        var correctAnswer = Console.ReadLine()?.Trim();
        Console.Write("Explanation: ");
        var explanation = Console.ReadLine()?.Trim();
        Console.Write("Is active? (y/n): ");
        var isActiveInput = Console.ReadLine()?.Trim().ToLower();
        Console.WriteLine();

        var request = new UpdateTaskRequest
        {
            TopicId = string.IsNullOrWhiteSpace(topicIdInput) || !long.TryParse(topicIdInput, out var tid) ? null : tid,
            TaskType = string.IsNullOrWhiteSpace(taskType) ? null : taskType,
            Difficulty = string.IsNullOrWhiteSpace(difficultyInput) || !short.TryParse(difficultyInput, out var diff) ? null : diff,
            Statement = string.IsNullOrWhiteSpace(statement) ? null : statement,
            CorrectAnswer = string.IsNullOrWhiteSpace(correctAnswer) ? null : correctAnswer,
            Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation,
            IsActive = string.IsNullOrWhiteSpace(isActiveInput) ? null : (isActiveInput == "y")
        };

        Console.WriteLine("Updating task...");
        var result = await _apiClient.UpdateTaskAsync(taskId, request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("Task updated successfully!");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task ActivateTaskAsync()
    {
        Console.WriteLine("=== Activate Task ===");
        Console.Write("Enter task ID: ");
        var taskIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(taskIdInput, out var taskId))
        {
            Console.WriteLine("Error: Invalid task ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Activating task...");
        var result = await _apiClient.ActivateTaskAsync(taskId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("Task activated successfully!");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task DeactivateTaskAsync()
    {
        Console.WriteLine("=== Deactivate Task ===");
        Console.Write("Enter task ID: ");
        var taskIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(taskIdInput, out var taskId))
        {
            Console.WriteLine("Error: Invalid task ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Deactivating task...");
        var result = await _apiClient.DeactivateTaskAsync(taskId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("Task deactivated successfully!");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task DeleteTaskAsync()
    {
        Console.WriteLine("=== Delete Task ===");
        Console.Write("Enter task ID: ");
        var taskIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(taskIdInput, out var taskId))
        {
            Console.WriteLine("Error: Invalid task ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.Write("Are you sure you want to delete this task? (yes/no): ");
        var confirm = Console.ReadLine()?.Trim().ToLower();
        if (confirm != "yes")
        {
            Console.WriteLine("Deletion cancelled.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Deleting task...");
        var result = await _apiClient.DeleteTaskAsync(taskId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("Task deleted successfully.");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task ManageTestsAsync()
    {
        Console.WriteLine("=== Manage Tests ===");
        Console.WriteLine("1) List tests");
        Console.WriteLine("2) Create test");
        Console.WriteLine("3) Edit test");
        Console.WriteLine("4) Manage test tasks");
        Console.WriteLine("5) Delete test");
        Console.WriteLine("0) Back");
        Console.Write("Select option: ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await ListTestsAsync();
                break;
            case "2":
                await CreateTestAsync();
                break;
            case "3":
                await EditTestAsync();
                break;
            case "4":
                await ManageTestTasksAsync();
                break;
            case "5":
                await DeleteTestAsync();
                break;
            case "0":
                return;
            default:
                Console.WriteLine("Unknown option.");
                break;
        }
    }

    private async Task ListTestsAsync()
    {
        Console.WriteLine("=== List Tests ===");
        Console.WriteLine("Enter filter options (leave empty to skip):");
        Console.Write("Subject ID: ");
        var subjectIdInput = Console.ReadLine()?.Trim();
        Console.Write("Test kind: ");
        var testKind = Console.ReadLine()?.Trim();
        Console.Write("Author ID: ");
        var authorIdInput = Console.ReadLine()?.Trim();
        Console.Write("Is published? (y/n): ");
        var isPublishedInput = Console.ReadLine()?.Trim().ToLower();
        Console.Write("Is state archive? (y/n): ");
        var isStateArchiveInput = Console.ReadLine()?.Trim().ToLower();
        Console.Write("Search query: ");
        var search = Console.ReadLine()?.Trim();
        Console.WriteLine();

        var filter = new TestFilterDto
        {
            SubjectId = string.IsNullOrWhiteSpace(subjectIdInput) || !long.TryParse(subjectIdInput, out var sid) ? null : sid,
            TestKind = string.IsNullOrWhiteSpace(testKind) ? null : testKind,
            AuthorId = string.IsNullOrWhiteSpace(authorIdInput) || !long.TryParse(authorIdInput, out var aid) ? null : aid,
            IsPublished = string.IsNullOrWhiteSpace(isPublishedInput) ? null : (isPublishedInput == "y"),
            IsStateArchive = string.IsNullOrWhiteSpace(isStateArchiveInput) ? null : (isStateArchiveInput == "y"),
            Search = string.IsNullOrWhiteSpace(search) ? null : search
        };

        Console.WriteLine("Loading tests...");
        var result = await _apiClient.GetAllTestsAsync(filter, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        var tests = result.Value!;
        if (tests.Count == 0)
        {
            Console.WriteLine("No tests found.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Found {tests.Count} test(s):");
        Console.WriteLine(new string('-', 100));
        foreach (var test in tests)
        {
            var authorInfo = test.AuthorName != null ? $" | Author: {test.AuthorName}" : "";
            var published = test.IsPublished ? "Published" : "Unpublished";
            var archive = test.IsStateArchive ? " | State Archive" : "";
            Console.WriteLine($"ID: {test.Id} | {test.Title} ({test.TestKind})");
            Console.WriteLine($"  Subject: {test.SubjectName}{authorInfo} | {published}{archive}");
            Console.WriteLine($"  Created: {test.CreatedAt:yyyy-MM-dd HH:mm}");
            Console.WriteLine();
        }
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task CreateTestAsync()
    {
        Console.WriteLine("=== Create Test ===");
        Console.WriteLine("Note: Test creation is complex. This is a simplified version.");
        Console.WriteLine("For full test creation with tasks, use the API directly.");
        Console.WriteLine();
        
        Console.Write("Subject ID: ");
        var subjectIdInput = Console.ReadLine()?.Trim();
        if (!long.TryParse(subjectIdInput, out var subjectId))
        {
            Console.WriteLine("Error: Invalid subject ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.Write("Test kind (CUSTOM/MIXED/STATE): ");
        var testKind = Console.ReadLine()?.Trim();
        Console.Write("Title: ");
        var title = Console.ReadLine()?.Trim();
        Console.Write("Time limit in seconds (optional): ");
        var timeLimitInput = Console.ReadLine()?.Trim();
        Console.Write("Attempts allowed (optional): ");
        var attemptsInput = Console.ReadLine()?.Trim();
        Console.Write("Mode (training/exam, optional): ");
        var mode = Console.ReadLine()?.Trim();
        Console.Write("Is published? (y/n, default: n): ");
        var isPublishedInput = Console.ReadLine()?.Trim().ToLower();
        var isPublished = isPublishedInput == "y";
        Console.Write("Is public? (y/n, default: n): ");
        var isPublicInput = Console.ReadLine()?.Trim().ToLower();
        var isPublic = isPublicInput == "y";
        Console.Write("Is state archive? (y/n, default: n): ");
        var isStateArchiveInput = Console.ReadLine()?.Trim().ToLower();
        var isStateArchive = isStateArchiveInput == "y";
        Console.WriteLine();

        if (string.IsNullOrWhiteSpace(testKind) || string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine("Error: Test kind and title are required.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Creating test (without tasks - add them via Edit Test)...");
        var request = new CreateTestRequest
        {
            SubjectId = subjectId,
            TestKind = testKind,
            Title = title,
            TimeLimitSec = string.IsNullOrWhiteSpace(timeLimitInput) || !int.TryParse(timeLimitInput, out var tl) ? null : tl,
            AttemptsAllowed = string.IsNullOrWhiteSpace(attemptsInput) || !short.TryParse(attemptsInput, out var att) ? null : att,
            Mode = string.IsNullOrWhiteSpace(mode) ? null : mode,
            IsPublished = isPublished,
            IsPublic = isPublic,
            IsStateArchive = isStateArchive,
            Tasks = Array.Empty<TestTaskUpdateDto>()
        };

        var result = await _apiClient.CreateTestAsync(request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine($"Test created successfully! ID: {result.Value!.Id}");
            Console.WriteLine("Note: You can add tasks to this test using Edit Test option.");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task EditTestAsync()
    {
        Console.WriteLine("=== Edit Test ===");
        Console.WriteLine("Note: Editing test tasks is complex. This is a simplified version.");
        Console.Write("Enter test ID: ");
        var testIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(testIdInput, out var testId))
        {
            Console.WriteLine("Error: Invalid test ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Leave field empty to keep current value.");
        Console.Write("Title: ");
        var title = Console.ReadLine()?.Trim();
        Console.Write("Test kind: ");
        var testKind = Console.ReadLine()?.Trim();
        Console.Write("Subject ID: ");
        var subjectIdInput = Console.ReadLine()?.Trim();
        Console.Write("Time limit in seconds: ");
        var timeLimitInput = Console.ReadLine()?.Trim();
        Console.Write("Attempts allowed: ");
        var attemptsInput = Console.ReadLine()?.Trim();
        Console.Write("Mode: ");
        var mode = Console.ReadLine()?.Trim();
        Console.Write("Is published? (y/n): ");
        var isPublishedInput = Console.ReadLine()?.Trim().ToLower();
        Console.Write("Is public? (y/n): ");
        var isPublicInput = Console.ReadLine()?.Trim().ToLower();
        Console.Write("Is state archive? (y/n): ");
        var isStateArchiveInput = Console.ReadLine()?.Trim().ToLower();
        Console.WriteLine();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(testKind) || string.IsNullOrWhiteSpace(subjectIdInput) || !long.TryParse(subjectIdInput, out var subjectId))
        {
            Console.WriteLine("Error: Title, test kind and subject ID are required.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        var request = new UpdateTestRequest
        {
            Title = title,
            TestKind = testKind,
            SubjectId = subjectId,
            TimeLimitSec = string.IsNullOrWhiteSpace(timeLimitInput) || !int.TryParse(timeLimitInput, out var tl) ? null : tl,
            AttemptsAllowed = string.IsNullOrWhiteSpace(attemptsInput) || !short.TryParse(attemptsInput, out var att) ? null : att,
            Mode = string.IsNullOrWhiteSpace(mode) ? null : mode,
            IsPublished = string.IsNullOrWhiteSpace(isPublishedInput) ? false : (isPublishedInput == "y"),
            IsPublic = string.IsNullOrWhiteSpace(isPublicInput) ? false : (isPublicInput == "y"),
            IsStateArchive = string.IsNullOrWhiteSpace(isStateArchiveInput) ? false : (isStateArchiveInput == "y"),
            Tasks = Array.Empty<TestTaskUpdateDto>()
        };

        Console.WriteLine("Updating test...");
        var result = await _apiClient.UpdateTestAsync(testId, request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("Test updated successfully!");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private Task ManageTestTasksAsync()
    {
        Console.WriteLine("=== Manage Test Tasks ===");
        Console.WriteLine("This feature requires complex task management.");
        Console.WriteLine("Please use Edit Test option or API directly to manage test tasks.");
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
        return Task.CompletedTask;
    }

    private async Task DeleteTestAsync()
    {
        Console.WriteLine("=== Delete Test ===");
        Console.Write("Enter test ID: ");
        var testIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(testIdInput, out var testId))
        {
            Console.WriteLine("Error: Invalid test ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.Write("Are you sure you want to delete this test? (yes/no): ");
        var confirm = Console.ReadLine()?.Trim().ToLower();
        if (confirm != "yes")
        {
            Console.WriteLine("Deletion cancelled.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Deleting test...");
        var result = await _apiClient.DeleteTestAsync(testId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("Test deleted successfully.");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }


    private async Task LogoutAsync()
    {
        var result = await _apiClient.LogoutAsync(_cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("Logged out successfully.");
        }
    }

    private static string ReadPassword()
    {
        var password = "";
        ConsoleKeyInfo key;
        do
        {
            key = Console.ReadKey(true);
            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
            {
                password += key.KeyChar;
                Console.Write("*");
            }
            else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password.Substring(0, password.Length - 1);
                Console.Write("\b \b");
            }
        } while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();
        return password;
    }

    private static string FormatError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return "Unknown error occurred.";
        }

        try
        {
            if (error.Contains("\"message\"") || error.Contains("\"Message\""))
            {
                var start = error.IndexOf('"', error.IndexOf("message", StringComparison.OrdinalIgnoreCase)) + 1;
                var end = error.IndexOf('"', start);
                if (start > 0 && end > start)
                {
                    return error.Substring(start, end - start);
                }
            }
        }
        catch
        {
        }

        return error;
    }

    private static string GetRoleName(int roleId)
    {
        return roleId switch
        {
            (int)RoleTypeEnum.Student => "Student",
            (int)RoleTypeEnum.Teacher => "Teacher",
            (int)RoleTypeEnum.Admin => "Admin",
            _ => $"Unknown ({roleId})"
        };
    }

    private async Task ManageInvitationCodesAsync()
    {
        Console.WriteLine("=== Manage Invitation Codes ===");
        Console.WriteLine("1) List invitation codes");
        Console.WriteLine("2) Create invitation code");
        Console.WriteLine("3) Edit invitation code");
        Console.WriteLine("4) Delete invitation code");
        Console.WriteLine("0) Back");
        Console.Write("Select option: ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await ListInvitationCodesAsync();
                break;
            case "2":
                await CreateInvitationCodeAsync();
                break;
            case "3":
                await EditInvitationCodeAsync();
                break;
            case "4":
                await DeleteInvitationCodeAsync();
                break;
            case "0":
                return;
            default:
                Console.WriteLine("Unknown option.");
                break;
        }
    }

    private async Task ListInvitationCodesAsync()
    {
        Console.WriteLine("=== List Invitation Codes ===");
        Console.WriteLine("Enter filter options (press Enter to skip and show all codes):");
        Console.Write("Teacher ID: ");
        var teacherIdInput = Console.ReadLine()?.Trim();
        Console.Write("Status (active/revoked/expired, or Enter for all): ");
        var status = Console.ReadLine()?.Trim();
        Console.WriteLine();

        long? teacherId = null;
        if (!string.IsNullOrWhiteSpace(teacherIdInput) && long.TryParse(teacherIdInput, out var tid))
        {
            teacherId = tid;
        }

        Console.WriteLine("Loading invitation codes...");
        var result = await _apiClient.GetAllInvitationCodesAsync(teacherId, status, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        var codes = result.Value!;
        if (codes.Count == 0)
        {
            Console.WriteLine("No invitation codes found.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Found {codes.Count} invitation code(s):");
        Console.WriteLine(new string('-', 100));
        foreach (var code in codes)
        {
            var maxUsesInfo = code.MaxUses.HasValue ? $" / {code.MaxUses.Value}" : " / Unlimited";
            var expiresInfo = code.ExpiresAt.HasValue ? $" | Expires: {code.ExpiresAt.Value:yyyy-MM-dd HH:mm}" : " | No expiration";
            Console.WriteLine($"ID: {code.Id} | Code: {code.Code}");
            Console.WriteLine($"  Teacher: {code.TeacherName} ({code.TeacherEmail})");
            Console.WriteLine($"  Uses: {code.UsedCount}{maxUsesInfo} | Status: {code.Status}{expiresInfo}");
            Console.WriteLine($"  Created: {code.CreatedAt:yyyy-MM-dd HH:mm}");
            Console.WriteLine();
        }
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task CreateInvitationCodeAsync()
    {
        Console.WriteLine("=== Create Invitation Code ===");
        Console.Write("Teacher ID: ");
        var teacherIdInput = Console.ReadLine()?.Trim();
        if (!long.TryParse(teacherIdInput, out var teacherId))
        {
            Console.WriteLine("Error: Invalid teacher ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.Write("Max uses (optional, press Enter for unlimited): ");
        var maxUsesInput = Console.ReadLine()?.Trim();
        int? maxUses = null;
        if (!string.IsNullOrWhiteSpace(maxUsesInput) && int.TryParse(maxUsesInput, out var mu))
        {
            maxUses = mu;
        }

        Console.Write("Expires at (optional, format: yyyy-MM-dd HH:mm, or press Enter for no expiration): ");
        var expiresInput = Console.ReadLine()?.Trim();
        DateTimeOffset? expiresAt = null;
        if (!string.IsNullOrWhiteSpace(expiresInput) && DateTimeOffset.TryParse(expiresInput, out var exp))
        {
            expiresAt = exp;
        }
        Console.WriteLine();

        Console.WriteLine("Creating invitation code...");
        var request = new ApiClient.CreateInvitationCodeRequest
        {
            TeacherId = teacherId,
            MaxUses = maxUses,
            ExpiresAt = expiresAt
        };

        var result = await _apiClient.CreateInvitationCodeAsync(request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine($"Invitation code created successfully!");
            Console.WriteLine($"Code: {result.Value!.Code}");
            Console.WriteLine($"ID: {result.Value!.Id}");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task EditInvitationCodeAsync()
    {
        Console.WriteLine("=== Edit Invitation Code ===");
        Console.Write("Enter invitation code ID: ");
        var codeIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(codeIdInput, out var codeId))
        {
            Console.WriteLine("Error: Invalid invitation code ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Leave field empty to keep current value.");
        Console.Write("Max uses: ");
        var maxUsesInput = Console.ReadLine()?.Trim();
        Console.Write("Expires at (format: yyyy-MM-dd HH:mm): ");
        var expiresInput = Console.ReadLine()?.Trim();
        Console.Write("Status (active/revoked/expired): ");
        var status = Console.ReadLine()?.Trim();
        Console.WriteLine();

        int? maxUses = null;
        if (!string.IsNullOrWhiteSpace(maxUsesInput) && int.TryParse(maxUsesInput, out var mu))
        {
            maxUses = mu;
        }

        DateTimeOffset? expiresAt = null;
        if (!string.IsNullOrWhiteSpace(expiresInput) && DateTimeOffset.TryParse(expiresInput, out var exp))
        {
            expiresAt = exp;
        }

        var request = new ApiClient.UpdateInvitationCodeRequest
        {
            MaxUses = maxUses,
            ExpiresAt = expiresAt,
            Status = string.IsNullOrWhiteSpace(status) ? null : status
        };

        Console.WriteLine("Updating invitation code...");
        var result = await _apiClient.UpdateInvitationCodeAsync(codeId, request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("Invitation code updated successfully!");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task DeleteInvitationCodeAsync()
    {
        Console.WriteLine("=== Delete Invitation Code ===");
        Console.Write("Enter invitation code ID: ");
        var codeIdInput = Console.ReadLine();
        Console.WriteLine();

        if (!long.TryParse(codeIdInput, out var codeId))
        {
            Console.WriteLine("Error: Invalid invitation code ID.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.Write("Are you sure you want to delete this invitation code? (yes/no): ");
        var confirm = Console.ReadLine()?.Trim().ToLower();
        if (confirm != "yes")
        {
            Console.WriteLine("Deletion cancelled.");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Deleting invitation code...");
        var result = await _apiClient.DeleteInvitationCodeAsync(codeId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
        }
        else
        {
            Console.WriteLine("Invitation code deleted successfully.");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _apiClient?.Dispose();
    }
}

