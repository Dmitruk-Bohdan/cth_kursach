using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTH.Common.Enums;
using static CTH.TeacherWebClient.ApiClient;

namespace CTH.TeacherWebClient;

public sealed class TeacherWebClientApp : IDisposable
{
    private readonly ApiClient _apiClient;
    private readonly CancellationTokenSource _cts = new();

    public TeacherWebClientApp(string baseUrl)
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
        Console.WriteLine("=== Teacher Web Client ===");
        Console.WriteLine("1) Register");
        Console.WriteLine("2) Login");
        Console.WriteLine("0) Exit");
        Console.Write("Select option: ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await RegisterAsync();
                break;
            case "2":
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

    private async Task RegisterAsync()
    {
        Console.Write("Email: ");
        var email = Console.ReadLine()?.Trim();
        Console.Write("User name: ");
        var userName = Console.ReadLine()?.Trim();
        Console.Write("Password: ");
        var password = ReadPassword();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Email, user name and password are required.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Registering as teacher...");

        var request = new RegisterRequest
        {
            Email = email,
            UserName = userName,
            Password = password,
            RoleTypeId = (int)RoleTypeEnum.Teacher // Только роль преподавателя
        };

        var result = await _apiClient.RegisterAsync(request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        // Проверяем роль пользователя
        if (_apiClient.UserRoleId != (int)RoleTypeEnum.Teacher && _apiClient.UserRoleId != (int)RoleTypeEnum.Admin)
        {
            Console.WriteLine("Registration failed: Invalid role assigned.");
            await _apiClient.LogoutAsync(_cts.Token);
            return;
        }

        Console.WriteLine($"Registered and logged in as {result.Value!.UserName}!");
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

        // Проверяем роль пользователя
        if (_apiClient.UserRoleId == null)
        {
            Console.WriteLine("Error: Could not determine user role.");
            await _apiClient.LogoutAsync(_cts.Token);
            Console.WriteLine();
            return;
        }

        if (_apiClient.UserRoleId != (int)RoleTypeEnum.Teacher && _apiClient.UserRoleId != (int)RoleTypeEnum.Admin)
        {
            var roleName = GetRoleName(_apiClient.UserRoleId.Value);
            Console.WriteLine($"Access denied. Only teachers and admins can use this client. Your role: {roleName}");
            await _apiClient.LogoutAsync(_cts.Token);
            Console.WriteLine();
            return;
        }

        Console.WriteLine($"Welcome, {result.Value!.UserName}!");
        Console.WriteLine();
    }

    private async Task ShowMainMenuAsync()
    {
        // Сначала выбираем предмет
        var subject = await SelectSubjectAsync();
        if (subject == null)
        {
            return;
        }

        // Затем показываем меню для выбранного предмета
        while (!_cts.IsCancellationRequested)
        {
            Console.WriteLine($"=== Subject: {subject.SubjectName} ===");
            Console.WriteLine("1) Create test");
            Console.WriteLine("2) Manage current tests");
            Console.WriteLine("3) Manage student bindings");
            Console.WriteLine("4) Select another subject");
            Console.WriteLine("5) Logout");
            Console.WriteLine("0) Exit");
            Console.Write("Select option: ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await CreateTestAsync(subject.Id, subject.SubjectName);
                    break;
                case "2":
                    await ManageTestsAsync(subject.Id, subject.SubjectName);
                    break;
                case "3":
                    await ManageStudentBindingsAsync();
                    break;
                case "4":
                    return; // Вернуться к выбору предмета
                case "5":
                    await LogoutAsync();
                    return;
                case "0":
                    _cts.Cancel();
                    return;
                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }
        }
    }

    private async Task<SubjectListItem?> SelectSubjectAsync()
    {
        var result = await _apiClient.GetAllSubjectsAsync(_cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return null;
        }

        var subjects = result.Value!;
        if (subjects.Count == 0)
        {
            Console.WriteLine("No subjects available.");
            return null;
        }

        Console.WriteLine("=== Select Subject ===");
        var subjectsList = subjects.ToList();
        for (int i = 0; i < subjectsList.Count; i++)
        {
            Console.WriteLine($"{i + 1}) {subjectsList[i].SubjectName}");
        }
        Console.Write("Enter subject number: ");
        var subjectInput = Console.ReadLine();
        Console.WriteLine();

        if (!int.TryParse(subjectInput, out var subjectIndex) || subjectIndex < 1 || subjectIndex > subjectsList.Count)
        {
            Console.WriteLine("Invalid subject number.");
            return null;
        }

        return subjectsList[subjectIndex - 1];
    }

    private async Task CreateTestAsync(long subjectId, string subjectName)
    {
        Console.WriteLine($"=== Create Test for {subjectName} ===");
        
        var testData = new TestCreationData
        {
            SubjectId = subjectId,
            TestKind = "CUSTOM", // Преподаватели всегда создают CUSTOM тесты
            IsStateArchive = false // Кастомные тесты преподавателей никогда не являются государственными
        };

        while (!_cts.IsCancellationRequested)
        {
            Console.WriteLine();
            Console.WriteLine("Current test configuration:");
            PrintTestConfiguration(testData);
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("1) Set title");
            Console.WriteLine("2) Set time limit");
            Console.WriteLine("3) Set attempts allowed");
            Console.WriteLine("4) Set mode");
            Console.WriteLine("5) Publish test");
            Console.WriteLine("6) Set privacy settings");
            Console.WriteLine("7) Add task");
            Console.WriteLine("8) Remove task");
            Console.WriteLine("9) View tasks");
            Console.WriteLine("10) Create test");
            Console.WriteLine("0) Cancel");
            Console.Write("Select option: ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    SetTitle(testData);
                    break;
                case "2":
                    SetTimeLimit(testData);
                    break;
                case "3":
                    SetAttemptsAllowed(testData);
                    break;
                case "4":
                    SetMode(testData);
                    break;
                case "5":
                    PublishTest(testData);
                    break;
                case "6":
                    SetPrivacySettings(testData);
                    break;
                case "7":
                    await AddTaskAsync(testData);
                    break;
                case "8":
                    RemoveTask(testData);
                    break;
                case "9":
                    ViewTasks(testData);
                    break;
                case "10":
                    if (await TryCreateTestAsync(testData))
                    {
                        return; // Вернуться в главное меню после успешного создания
                    }
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }
        }
    }

    private void PrintTestConfiguration(TestCreationData data)
    {
        Console.WriteLine($"  Title: {(string.IsNullOrWhiteSpace(data.Title) ? "(not set)" : data.Title)}");
        Console.WriteLine($"  Test Kind: {data.TestKind} (CUSTOM - teacher's test)");
        Console.WriteLine($"  Time Limit: {(data.TimeLimitSec.HasValue ? $"{data.TimeLimitSec / 60} minute(s)" : "Unlimited")}");
        Console.WriteLine($"  Attempts Allowed: {(data.AttemptsAllowed.HasValue ? data.AttemptsAllowed.ToString() : "Unlimited")}");
        Console.WriteLine($"  Mode: {(string.IsNullOrWhiteSpace(data.Mode) ? "none" : data.Mode)}");
        Console.WriteLine($"  Published: {data.IsPublished}");
        Console.WriteLine($"  Public: {data.IsPublic}");
        Console.WriteLine($"  State Archive: {data.IsStateArchive}");
        Console.WriteLine($"  Tasks: {data.Tasks.Count}");
    }

    private void SetTitle(TestCreationData data)
    {
        Console.Write("Enter test title: ");
        var title = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine("Title cannot be empty.");
            return;
        }
        if (title.Length > 200)
        {
            Console.WriteLine("Title cannot exceed 200 characters.");
            return;
        }
        data.Title = title;
        Console.WriteLine("Title set successfully.");
    }

    private void SetTestKind(TestCreationData data)
    {
        Console.WriteLine("Select test kind:");
        Console.WriteLine("1) CUSTOM - Custom test created by teacher");
        Console.WriteLine("2) PAST_EXAM - Past exam (state archive)");
        Console.WriteLine("3) MIXED - Mixed test");
        Console.Write("Enter option (1-3): ");
        
        var choice = Console.ReadLine()?.Trim();
        string? kind = choice switch
        {
            "1" => "CUSTOM",
            "2" => "PAST_EXAM",
            "3" => "MIXED",
            _ => null
        };

        if (kind == null)
        {
            Console.WriteLine("Invalid option. Test kind not changed.");
            return;
        }

        data.TestKind = kind;
        Console.WriteLine($"Test kind set to: {kind}");
        
        // Если выбран PAST_EXAM, автоматически устанавливаем is_state_archive = true
        if (kind == "PAST_EXAM")
        {
            data.IsStateArchive = true;
            Console.WriteLine("Note: PAST_EXAM tests are automatically marked as state archive.");
        }
    }

    private void SetTimeLimit(TestCreationData data)
    {
        Console.Write("Enter time limit in minutes (or 0 for unlimited): ");
        if (int.TryParse(Console.ReadLine(), out var minutes))
        {
            if (minutes == 0)
            {
                data.TimeLimitSec = null;
                Console.WriteLine("Time limit set to unlimited.");
            }
            else if (minutes > 0)
            {
                data.TimeLimitSec = minutes * 60; // Конвертируем минуты в секунды
                Console.WriteLine($"Time limit set to {minutes} minute(s) ({data.TimeLimitSec} seconds).");
            }
            else
            {
                Console.WriteLine("Time limit must be a positive number or 0 for unlimited.");
            }
        }
        else
        {
            Console.WriteLine("Invalid input. Please enter a number.");
        }
    }

    private void SetAttemptsAllowed(TestCreationData data)
    {
        Console.Write("Enter number of attempts allowed (or 0 for unlimited): ");
        if (short.TryParse(Console.ReadLine(), out var attempts))
        {
            data.AttemptsAllowed = attempts > 0 ? attempts : null;
            Console.WriteLine($"Attempts allowed set to {(data.AttemptsAllowed.HasValue ? data.AttemptsAllowed.ToString() : "unlimited")}.");
        }
        else
        {
            Console.WriteLine("Invalid input.");
        }
    }

    private void SetMode(TestCreationData data)
    {
        Console.WriteLine("Select test mode:");
        Console.WriteLine("1) training - Training mode (with hints, unlimited attempts)");
        Console.WriteLine("2) exam - Exam mode (strict, limited attempts)");
        Console.WriteLine("3) control - Control work mode (moderate restrictions)");
        Console.WriteLine("0) Clear mode (no specific mode)");
        Console.Write("Enter option (1-3, or 0 to clear): ");
        
        var choice = Console.ReadLine()?.Trim();
        string? mode = choice switch
        {
            "1" => "training",
            "2" => "exam",
            "3" => "control",
            "0" => null,
            _ => data.Mode // Сохраняем текущее значение при неверном вводе
        };

        if (mode == null && choice == "0")
        {
            data.Mode = null;
            Console.WriteLine("Mode cleared.");
        }
        else if (mode != null && (choice == "1" || choice == "2" || choice == "3"))
        {
            data.Mode = mode;
            Console.WriteLine($"Mode set to: {mode}");
        }
        else
        {
            Console.WriteLine("Invalid option. Mode not changed.");
        }
    }

    private void PublishTest(TestCreationData data)
    {
        Console.Write("Publish this test? (y/n): ");
        var response = Console.ReadLine()?.Trim().ToLower();
        if (response == "y" || response == "yes")
        {
            data.IsPublished = true;
            Console.WriteLine("Test will be published.");
        }
        else if (response == "n" || response == "no")
        {
            data.IsPublished = false;
            Console.WriteLine("Test will not be published.");
        }
        else
        {
            Console.WriteLine("Invalid input. Please enter 'y' or 'n'.");
        }
    }

    private void SetPrivacySettings(TestCreationData data)
    {
        Console.WriteLine("=== Privacy Settings ===");
        Console.Write("Make test public (visible to all students)? (y/n): ");
        var publicResponse = Console.ReadLine()?.Trim().ToLower();
        if (publicResponse == "y" || publicResponse == "yes")
        {
            data.IsPublic = true;
            Console.WriteLine("Test will be public.");
        }
        else if (publicResponse == "n" || publicResponse == "no")
        {
            data.IsPublic = false;
            Console.WriteLine("Test will be private (only for your students).");
        }
        else
        {
            Console.WriteLine("Invalid input. Please enter 'y' or 'n'.");
        }
        
        // Кастомные тесты преподавателей всегда не являются государственными
        data.IsStateArchive = false;
    }

    private async Task ChangePrivacySettingsAsync(long testId, TestCreationData data)
    {
        while (!_cts.IsCancellationRequested)
        {
            Console.WriteLine("=== Privacy Settings ===");
            Console.WriteLine($"Current status: {(data.IsPublic ? "Public" : "Private")}");
            Console.WriteLine();
            
            if (data.IsPublic)
            {
                Console.WriteLine("1) Make test private");
            }
            else
            {
                Console.WriteLine("1) Make test public");
            }
            
            if (!data.IsPublic)
            {
                Console.WriteLine("2) Set student IDs with access to this test");
            }
            
            Console.WriteLine("0) Back");
            Console.Write("Select option: ");

            var choice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(choice))
            {
                Console.WriteLine("Please select an option.");
                Console.WriteLine();
                continue;
            }

            switch (choice)
            {
                case "1":
                    data.IsPublic = !data.IsPublic;
                    if (data.IsPublic)
                    {
                        Console.WriteLine("Test is now public (visible to all students).");
                        // При переходе в публичный режим удаляем все индивидуальные доступы
                        await _apiClient.SetTestStudentAccessListAsync(testId, Array.Empty<long>(), _cts.Token);
                    }
                    else
                    {
                        Console.WriteLine("Test is now private.");
                    }
                    Console.WriteLine();
                    break;
                case "2":
                    if (!data.IsPublic)
                    {
                        await SetStudentAccessListAsync(testId);
                    }
                    else
                    {
                        Console.WriteLine("Invalid option.");
                    }
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }
        }
    }

    private async Task SetStudentAccessListAsync(long testId)
    {
        Console.WriteLine("=== Set Student Access ===");
        Console.WriteLine("Enter student IDs separated by spaces (or press Enter to clear all): ");
        var input = Console.ReadLine()?.Trim();
        Console.WriteLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            // Очищаем все доступы
            var result = await _apiClient.SetTestStudentAccessListAsync(testId, Array.Empty<long>(), _cts.Token);
            if (result.Success)
            {
                Console.WriteLine("All student access cleared.");
            }
            else
            {
                Console.WriteLine($"Error: {FormatError(result.Error)}");
            }
            Console.WriteLine();
            return;
        }

        var studentIdStrings = input.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var studentIds = new List<long>();

        foreach (var idString in studentIdStrings)
        {
            if (long.TryParse(idString, out var studentId))
            {
                studentIds.Add(studentId);
            }
            else
            {
                Console.WriteLine($"Invalid student ID: {idString}. Skipping.");
            }
        }

        if (studentIds.Count == 0)
        {
            Console.WriteLine("No valid student IDs provided.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine($"Setting access for {studentIds.Count} student(s)...");
        var setResult = await _apiClient.SetTestStudentAccessListAsync(testId, studentIds, _cts.Token);
        
        if (setResult.Success)
        {
            Console.WriteLine("Student access updated successfully.");
            
            // Показываем текущий список
            var getResult = await _apiClient.GetTestStudentAccessAsync(testId, _cts.Token);
            if (getResult.Success && getResult.Value!.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Students with access:");
                foreach (var student in getResult.Value!)
                {
                    Console.WriteLine($"  - {student.UserName} (ID: {student.Id}, Email: {student.Email})");
                }
            }
        }
        else
        {
            Console.WriteLine($"Error: {FormatError(setResult.Error)}");
        }
        Console.WriteLine();
    }

    private async Task AddTaskAsync(TestCreationData data)
    {
        Console.WriteLine();
        Console.WriteLine("=== Add Task ===");
        Console.WriteLine("1) Create new task");
        Console.WriteLine("2) Select from existing tasks");
        Console.Write("Select option: ");
        
        var choice = Console.ReadLine()?.Trim();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await CreateNewTaskAsync(data);
                break;
            case "2":
                await SelectExistingTaskAsync(data);
                break;
            default:
                Console.WriteLine("Invalid option.");
                break;
        }
    }

    private async Task CreateNewTaskAsync(TestCreationData data)
    {
        var taskData = new TaskCreationData
        {
            SubjectId = data.SubjectId
        };

        while (!_cts.IsCancellationRequested)
        {
            Console.WriteLine();
            Console.WriteLine("Current task configuration:");
            PrintTaskConfiguration(taskData);
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("1) Set topic");
            Console.WriteLine("2) Set task type");
            Console.WriteLine("3) Set difficulty");
            Console.WriteLine("4) Set statement");
            Console.WriteLine("5) Set correct answer");
            Console.WriteLine("6) Set explanation");
            Console.WriteLine("7) Create task");
            Console.WriteLine("0) Cancel");
            Console.Write("Select option: ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await SetTopicAsync(taskData);
                    break;
                case "2":
                    SetTaskType(taskData);
                    break;
                case "3":
                    SetDifficulty(taskData);
                    break;
                case "4":
                    SetStatement(taskData);
                    break;
                case "5":
                    SetCorrectAnswer(taskData);
                    break;
                case "6":
                    SetExplanation(taskData);
                    break;
                case "7":
                    if (await TryCreateTaskAsync(taskData, data))
                    {
                        return; // Вернуться в меню создания теста после успешного создания задания
                    }
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private async Task SelectExistingTaskAsync(TestCreationData data)
    {
        // Запрашиваем поисковый запрос
        Console.Write("Enter search query (ID or text, or leave empty for all tasks): ");
        var searchQuery = Console.ReadLine()?.Trim();
        Console.WriteLine();

        Console.WriteLine("Loading tasks...");
        var tasksResult = await _apiClient.GetTasksBySubjectAsync(data.SubjectId, searchQuery, _cts.Token);
        
        if (!tasksResult.Success || tasksResult.Value == null)
        {
            Console.WriteLine($"Failed to load tasks: {tasksResult.Error ?? "Unknown error"}");
            return;
        }

        var tasks = tasksResult.Value.ToList();
        if (tasks.Count == 0)
        {
            Console.WriteLine("No tasks found.");
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                Console.WriteLine("Trying to load all tasks...");
                // Если поиск не дал результатов, загружаем все задания
                var allTasksResult = await _apiClient.GetTasksBySubjectAsync(data.SubjectId, null, _cts.Token);
                if (allTasksResult.Success && allTasksResult.Value != null)
                {
                    tasks = allTasksResult.Value.ToList();
                    if (tasks.Count == 0)
                    {
                        Console.WriteLine("No tasks available for this subject.");
                        return;
                    }
                    Console.WriteLine($"Found {tasks.Count} task(s).");
                }
            }
            else
            {
                return;
            }
        }

        // Показываем список заданий
        Console.WriteLine();
        Console.WriteLine($"Found {tasks.Count} task(s):");
        Console.WriteLine(new string('-', 100));
        for (int i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            var topicInfo = !string.IsNullOrWhiteSpace(task.TopicName) 
                ? $" [{task.TopicName}]" 
                : "";
            var statementPreview = task.Statement.Length > 60 
                ? task.Statement.Substring(0, 60) + "..." 
                : task.Statement;
            Console.WriteLine($"{i + 1,3}. ID: {task.Id,5} | Type: {task.TaskType,-15} | Difficulty: {task.Difficulty,2} |{topicInfo}");
            Console.WriteLine($"     {statementPreview}");
            
            // Добавляем отступ между заданиями (кроме последнего)
            if (i < tasks.Count - 1)
            {
                Console.WriteLine();
            }
        }
        Console.WriteLine(new string('-', 100));
        Console.WriteLine();

        // Выбор задания
        Console.Write($"Select task (1-{tasks.Count}): ");
        if (!int.TryParse(Console.ReadLine(), out var taskIndex) || taskIndex < 1 || taskIndex > tasks.Count)
        {
            Console.WriteLine("Invalid task number.");
            return;
        }

        var selectedTask = tasks[taskIndex - 1];
        var taskId = selectedTask.Id;

        // Проверяем, что задание не добавлено дважды
        if (data.Tasks.Any(t => t.TaskId == taskId))
        {
            Console.WriteLine($"Task {taskId} is already added.");
            return;
        }

        // Автоматически определяем следующую позицию (добавляем в конец)
        var position = data.Tasks.Count > 0 
            ? data.Tasks.Max(t => t.Position) + 1 
            : 1;

        data.Tasks.Add(new TestTaskData
        {
            TaskId = taskId,
            Position = position,
            Weight = null // Weight не используется в текущей логике
        });

        Console.WriteLine($"Task {taskId} ({selectedTask.TaskType}) added at position {position}.");
    }

    private void RemoveTask(TestCreationData data)
    {
        if (data.Tasks.Count == 0)
        {
            Console.WriteLine("No tasks to remove.");
            return;
        }

        ViewTasks(data);
        Console.Write("Enter position of task to remove: ");
        if (!int.TryParse(Console.ReadLine(), out var position))
        {
            Console.WriteLine("Invalid position.");
            return;
        }

        var task = data.Tasks.FirstOrDefault(t => t.Position == position);
        if (task == null)
        {
            Console.WriteLine($"No task found at position {position}.");
            return;
        }

        data.Tasks.Remove(task);
        Console.WriteLine($"Task at position {position} removed.");
    }

    private void ViewTasks(TestCreationData data)
    {
        if (data.Tasks.Count == 0)
        {
            Console.WriteLine("No tasks added yet.");
            return;
        }

        Console.WriteLine("Tasks:");
        foreach (var task in data.Tasks.OrderBy(t => t.Position))
        {
            Console.WriteLine($"  Position {task.Position}: Task ID {task.TaskId}");
        }
    }

    private async Task<bool> TryCreateTestAsync(TestCreationData data)
    {
        // Валидация
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(data.Title))
        {
            errors.Add("Title is required.");
        }
        else if (data.Title.Length > 200)
        {
            errors.Add("Title cannot exceed 200 characters.");
        }

        // TestKind всегда "CUSTOM" для тестов преподавателя, проверка не нужна

        if (data.Tasks.Count == 0)
        {
            errors.Add("At least one task is required.");
        }

        // Проверяем уникальность позиций
        var positions = data.Tasks.Select(t => t.Position).ToList();
        if (positions.Count != positions.Distinct().Count())
        {
            errors.Add("Task positions must be unique.");
        }

        if (errors.Count > 0)
        {
            Console.WriteLine("Cannot create test. Errors:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }
            return false;
        }

        Console.WriteLine("Creating test...");

        var request = new CreateTestRequest(
            data.SubjectId,
            data.TestKind,
            data.Title,
            data.TimeLimitSec,
            data.AttemptsAllowed,
            data.Mode,
            data.IsPublished,
            data.IsPublic,
            data.IsStateArchive,
            data.Tasks.Select(t => new TestTaskRequest(t.TaskId, t.Position, t.Weight)).ToList()
        );

        var result = await _apiClient.CreateTestAsync(request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return false;
        }

        Console.WriteLine($"Test created successfully! Test ID: {result.Value!.Id}");
        return true;
    }

    private async Task LogoutAsync()
    {
        await _apiClient.LogoutAsync(_cts.Token);
        Console.WriteLine("Logged out.");
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
                password = password[0..^1];
                Console.Write("\b \b");
            }
        } while (key.Key != ConsoleKey.Enter);
        Console.WriteLine();
        return password;
    }

    private static string GetRoleName(int roleId)
    {
        return roleId switch
        {
            (int)RoleTypeEnum.Student => "Student",
            (int)RoleTypeEnum.Teacher => "Teacher",
            (int)RoleTypeEnum.Admin => "Admin",
            _ => $"Unknown (ID: {roleId})"
        };
    }

    private static string FormatError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return "Unknown error occurred.";
        }
        return error;
    }

    public void Dispose()
    {
        _cts.Dispose();
        _apiClient.Dispose();
    }

    private void PrintTaskConfiguration(TaskCreationData data)
    {
        Console.WriteLine($"  Subject ID: {data.SubjectId}");
        Console.WriteLine($"  Topic: {(data.TopicName != null ? $"{data.TopicName} (ID: {data.TopicId})" : "Not set")}");
        Console.WriteLine($"  Task Type: {(data.TaskType ?? "Not set")}");
        Console.WriteLine($"  Difficulty: {(data.Difficulty.HasValue ? data.Difficulty.ToString() : "Not set")}");
        Console.WriteLine($"  Statement: {(string.IsNullOrWhiteSpace(data.Statement) ? "Not set" : (data.Statement.Length > 50 ? data.Statement.Substring(0, 50) + "..." : data.Statement))}");
        Console.WriteLine($"  Correct Answer: {(string.IsNullOrWhiteSpace(data.CorrectAnswer) ? "Not set" : (data.CorrectAnswer.Length > 50 ? data.CorrectAnswer.Substring(0, 50) + "..." : data.CorrectAnswer))}");
        Console.WriteLine($"  Explanation: {(string.IsNullOrWhiteSpace(data.Explanation) ? "Not set" : (data.Explanation.Length > 50 ? data.Explanation.Substring(0, 50) + "..." : data.Explanation))}");
        Console.WriteLine($"  Active: {data.IsActive}");
    }

    private async Task SetTopicAsync(TaskCreationData data)
    {
        Console.WriteLine("Loading topics...");
        var topicsResult = await _apiClient.GetTopicsBySubjectAsync(data.SubjectId, _cts.Token);
        
        if (!topicsResult.Success || topicsResult.Value == null)
        {
            Console.WriteLine($"Failed to load topics: {topicsResult.Error ?? "Unknown error"}");
            return;
        }

        var topics = topicsResult.Value.ToList();
        if (topics.Count == 0)
        {
            Console.WriteLine("No topics available for this subject.");
            Console.Write("Continue without topic? (y/n): ");
            var response = Console.ReadLine()?.Trim().ToLower();
            if (response == "y" || response == "yes")
            {
                data.TopicId = null;
                data.TopicName = null;
                Console.WriteLine("Topic cleared.");
            }
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Available topics:");
        for (int i = 0; i < topics.Count; i++)
        {
            var topic = topics[i];
            Console.WriteLine($"{i + 1}) {topic.TopicName}{(topic.TopicCode != null ? $" ({topic.TopicCode})" : "")}");
        }
        Console.WriteLine("0) Clear topic");
        Console.Write("Select topic: ");

        if (!int.TryParse(Console.ReadLine(), out var topicIndex))
        {
            Console.WriteLine("Invalid input.");
            return;
        }

        if (topicIndex == 0)
        {
            data.TopicId = null;
            data.TopicName = null;
            Console.WriteLine("Topic cleared.");
        }
        else if (topicIndex >= 1 && topicIndex <= topics.Count)
        {
            var selectedTopic = topics[topicIndex - 1];
            data.TopicId = selectedTopic.Id;
            data.TopicName = selectedTopic.TopicName;
            Console.WriteLine($"Topic set to: {selectedTopic.TopicName}");
        }
        else
        {
            Console.WriteLine("Invalid topic number.");
        }
    }

    private void SetTaskType(TaskCreationData data)
    {
        Console.WriteLine("Select task type:");
        Console.WriteLine("1) numeric - Numeric answer (numbers separated by space for multiple answers)");
        Console.WriteLine("2) text - Text answer (case-insensitive word)");
        Console.WriteLine("0) Clear task type");
        Console.Write("Enter option (1-2, or 0 to clear): ");
        
        var choice = Console.ReadLine()?.Trim();
        string? taskType = choice switch
        {
            "1" => "numeric",
            "2" => "text",
            "0" => null,
            _ => data.TaskType
        };

        if (taskType == null && choice == "0")
        {
            data.TaskType = null;
            Console.WriteLine("Task type cleared.");
        }
        else if (taskType != null)
        {
            data.TaskType = taskType;
            Console.WriteLine($"Task type set to: {taskType}");
        }
        else
        {
            Console.WriteLine("Invalid option. Task type not changed.");
        }
    }

    private void SetDifficulty(TaskCreationData data)
    {
        Console.WriteLine("Select difficulty (1-5):");
        Console.WriteLine("1) Very Easy");
        Console.WriteLine("2) Easy");
        Console.WriteLine("3) Medium");
        Console.WriteLine("4) Hard");
        Console.WriteLine("5) Very Hard");
        Console.WriteLine("0) Clear difficulty");
        Console.Write("Enter option (1-5, or 0 to clear): ");
        
        var choice = Console.ReadLine()?.Trim();
        if (choice == "0")
        {
            data.Difficulty = null;
            Console.WriteLine("Difficulty cleared.");
        }
        else if (short.TryParse(choice, out var difficulty) && difficulty >= 1 && difficulty <= 5)
        {
            data.Difficulty = difficulty;
            Console.WriteLine($"Difficulty set to: {difficulty}");
        }
        else
        {
            Console.WriteLine("Invalid option. Difficulty must be between 1 and 5.");
        }
    }

    private void SetStatement(TaskCreationData data)
    {
        Console.WriteLine("Enter task statement (press Enter on empty line to finish):");
        var lines = new List<string>();
        string? line;
        while ((line = Console.ReadLine()) != null && line.Trim() != "")
        {
            lines.Add(line);
        }
        
        if (lines.Count > 0)
        {
            data.Statement = string.Join("\n", lines);
            Console.WriteLine("Statement set.");
        }
        else
        {
            Console.WriteLine("Statement cannot be empty.");
        }
    }

    private void SetCorrectAnswer(TaskCreationData data)
    {
        if (data.TaskType == "numeric")
        {
            Console.WriteLine("Enter correct answer (numbers separated by space for multiple answers):");
            Console.WriteLine("Example: 5 6 7 or 42.5");
            Console.Write("Correct answer: ");
        }
        else if (data.TaskType == "text")
        {
            Console.WriteLine("Enter correct answer (case-insensitive word):");
            Console.WriteLine("Example: слово");
            Console.Write("Correct answer: ");
        }
        else
        {
            Console.WriteLine("Please set task type first.");
            return;
        }
        
        var answer = Console.ReadLine()?.Trim();
        if (!string.IsNullOrWhiteSpace(answer))
        {
            // Для numeric проверяем, что это числа через пробел
            if (data.TaskType == "numeric")
            {
                var parts = answer.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                bool isValid = true;
                foreach (var part in parts)
                {
                    if (!double.TryParse(part, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out _))
                    {
                        isValid = false;
                        break;
                    }
                }
                if (!isValid)
                {
                    Console.WriteLine("Invalid format. For numeric tasks, enter numbers separated by space (e.g., '5 6 7' or '42.5').");
                    return;
                }
            }
            
            // Сохраняем просто как строку - при сохранении в БД обернем в JSON
            data.CorrectAnswer = answer;
            Console.WriteLine("Correct answer set.");
        }
        else
        {
            Console.WriteLine("Answer cannot be empty.");
        }
    }

    private void SetExplanation(TaskCreationData data)
    {
        Console.WriteLine("Enter explanation (optional, press Enter on empty line to finish, or type 'clear' to remove):");
        var lines = new List<string>();
        string? line;
        while ((line = Console.ReadLine()) != null && line.Trim() != "")
        {
            if (line.Trim().ToLower() == "clear")
            {
                data.Explanation = null;
                Console.WriteLine("Explanation cleared.");
                return;
            }
            lines.Add(line);
        }
        
        if (lines.Count > 0)
        {
            data.Explanation = string.Join("\n", lines);
            Console.WriteLine("Explanation set.");
        }
        else
        {
            data.Explanation = null;
            Console.WriteLine("Explanation cleared.");
        }
    }

    private async Task<bool> TryCreateTaskAsync(TaskCreationData data, TestCreationData testData)
    {
        // Валидация
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(data.TaskType))
        {
            errors.Add("Task type is required.");
        }

        if (!data.Difficulty.HasValue)
        {
            errors.Add("Difficulty is required.");
        }
        else if (data.Difficulty < 1 || data.Difficulty > 5)
        {
            errors.Add("Difficulty must be between 1 and 5.");
        }

        if (string.IsNullOrWhiteSpace(data.Statement))
        {
            errors.Add("Statement is required.");
        }

        if (string.IsNullOrWhiteSpace(data.CorrectAnswer))
        {
            errors.Add("Correct answer is required.");
        }
        else if (data.TaskType == "numeric")
        {
            // Дополнительная валидация для numeric
            var parts = data.CorrectAnswer.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (!double.TryParse(part, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out _))
                {
                    errors.Add("For numeric tasks, correct answer must contain numbers separated by space.");
                    break;
                }
            }
        }

        if (errors.Count > 0)
        {
            Console.WriteLine("Validation errors:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }
            Console.WriteLine("Please fix the errors and try again.");
            return false;
        }

        // Создание задания
        var request = new ApiClient.CreateTaskRequest(
            data.SubjectId,
            data.TopicId,
            data.TaskType!,
            data.Difficulty!.Value,
            data.Statement!,
            data.CorrectAnswer!,
            data.Explanation,
            data.IsActive
        );

        Console.WriteLine("Creating task...");
        var result = await _apiClient.CreateTaskAsync(request, _cts.Token);

        if (!result.Success)
        {
            Console.WriteLine($"Failed to create task: {result.Error ?? "Unknown error"}");
            return false;
        }

        var createdTaskId = result.Value!.Id;
        Console.WriteLine($"Task created successfully! ID: {createdTaskId}");
        Console.WriteLine($"Task Type: {result.Value.TaskType}, Difficulty: {result.Value.Difficulty}");

        // Автоматически добавляем задание в тест
        var position = testData.Tasks.Count > 0 
            ? testData.Tasks.Max(t => t.Position) + 1 
            : 1;

        // Проверяем, что задание не добавлено дважды (на всякий случай)
        if (!testData.Tasks.Any(t => t.TaskId == createdTaskId))
        {
            testData.Tasks.Add(new TestTaskData
            {
                TaskId = createdTaskId,
                Position = position,
                Weight = null
            });
            Console.WriteLine($"Task {createdTaskId} automatically added to test at position {position}.");
        }
        else
        {
            Console.WriteLine($"Task {createdTaskId} is already in the test.");
        }

        return true;
    }

    private class TaskCreationData
    {
        public long SubjectId { get; set; }
        public long? TopicId { get; set; }
        public string? TopicName { get; set; }
        public string? TaskType { get; set; }
        public short? Difficulty { get; set; }
        public string? Statement { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
        public bool IsActive { get; set; } = true;
    }

    private class TestCreationData
    {
        public long SubjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TestKind { get; set; } = string.Empty;
        public int? TimeLimitSec { get; set; }
        public short? AttemptsAllowed { get; set; }
        public string? Mode { get; set; }
        public bool IsPublished { get; set; }
        public bool IsPublic { get; set; }
        public bool IsStateArchive { get; set; }
        public List<TestTaskData> Tasks { get; set; } = new();
    }

    private class TestTaskData
    {
        public long TaskId { get; set; }
        public int Position { get; set; }
        public decimal? Weight { get; set; }
    }

    private async Task ManageTestsAsync(long subjectId, string subjectName)
    {
        Console.WriteLine($"=== Manage Tests - {subjectName} ===");
        
        var result = await _apiClient.GetMyTestsAsync(subjectId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine();
            return;
        }

        var tests = result.Value!;
        if (tests.Count == 0)
        {
            Console.WriteLine("No tests found for this subject.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("Available tests:");
        for (int i = 0; i < tests.Count; i++)
        {
            var test = tests.ElementAt(i);
            var published = "Published";
            var mode = string.IsNullOrWhiteSpace(test.Mode) ? "none" : test.Mode;
            Console.WriteLine($"{i + 1}) {test.Title} (ID: {test.Id}, Mode: {mode}, {published})");
        }
        Console.WriteLine("0) Back");
        Console.Write("Select test to edit: ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        if (choice == "0" || !int.TryParse(choice, out var index) || index < 1 || index > tests.Count)
        {
            return;
        }

        var selectedTest = tests.ElementAt(index - 1);
        await EditTestAsync(selectedTest.Id, subjectId, subjectName);
    }

    private async Task EditTestAsync(long testId, long subjectId, string subjectName)
    {
        // Загружаем детали теста
        var result = await _apiClient.GetTestDetailsAsync(testId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error loading test: {FormatError(result.Error)}");
            Console.WriteLine();
            return;
        }

        var testDetails = result.Value!;
        
        // Создаем объект для редактирования на основе существующего теста
        var testData = new TestCreationData
        {
            SubjectId = testDetails.SubjectId,
            Title = testDetails.Title,
            TestKind = testDetails.TestKind,
            TimeLimitSec = testDetails.TimeLimitSec,
            AttemptsAllowed = testDetails.AttemptsAllowed,
            Mode = testDetails.Mode,
            IsPublished = testDetails.IsPublished,
            IsPublic = testDetails.IsPublic,
            IsStateArchive = testDetails.IsStateArchive,
            Tasks = testDetails.Tasks?.Select(t => new TestTaskData
            {
                TaskId = t.TaskId,
                Position = t.Position,
                Weight = null
            }).ToList() ?? new List<TestTaskData>()
        };

        while (!_cts.IsCancellationRequested)
        {
            Console.WriteLine();
            Console.WriteLine($"Current test configuration (ID: {testId}):");
            PrintTestConfiguration(testData);
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("1) Change title");
            Console.WriteLine("2) Change time limit");
            Console.WriteLine("3) Change attempts allowed");
            Console.WriteLine("4) Change mode");
            Console.WriteLine("5) Publish/Unpublish test");
            Console.WriteLine("6) Change privacy settings");
            Console.WriteLine("7) Add task");
            Console.WriteLine("8) Remove task");
            Console.WriteLine("9) View tasks");
            Console.WriteLine("10) Save changes");
            Console.WriteLine("0) Cancel");
            Console.Write("Select option: ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    ChangeTitle(testData);
                    break;
                case "2":
                    ChangeTimeLimit(testData);
                    break;
                case "3":
                    ChangeAttemptsAllowed(testData);
                    break;
                case "4":
                    ChangeMode(testData);
                    break;
                case "5":
                    PublishTest(testData);
                    break;
                case "6":
                    await ChangePrivacySettingsAsync(testId, testData);
                    break;
                case "7":
                    await AddTaskAsync(testData);
                    break;
                case "8":
                    RemoveTask(testData);
                    break;
                case "9":
                    ViewTasks(testData);
                    break;
                case "10":
                    if (await TryUpdateTestAsync(testId, testData))
                    {
                        return; // Вернуться в меню управления тестами после успешного обновления
                    }
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }
        }
    }

    private void ChangeTitle(TestCreationData data)
    {
        SetTitle(data);
    }

    private void ChangeTimeLimit(TestCreationData data)
    {
        SetTimeLimit(data);
    }

    private void ChangeAttemptsAllowed(TestCreationData data)
    {
        SetAttemptsAllowed(data);
    }

    private void ChangeMode(TestCreationData data)
    {
        SetMode(data);
    }

    private async Task<bool> TryUpdateTestAsync(long testId, TestCreationData data)
    {
        // Валидация
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(data.Title))
        {
            errors.Add("Title is required.");
        }
        else if (data.Title.Length > 200)
        {
            errors.Add("Title cannot exceed 200 characters.");
        }

        if (data.TimeLimitSec.HasValue && data.TimeLimitSec < 0)
        {
            errors.Add("Time limit cannot be negative.");
        }

        if (data.AttemptsAllowed.HasValue && data.AttemptsAllowed < 0)
        {
            errors.Add("Attempts allowed cannot be negative.");
        }

        if (errors.Count > 0)
        {
            Console.WriteLine("Validation errors:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }
            Console.WriteLine("Please fix the errors and try again.");
            return false;
        }

        // Создаем запрос на обновление
        var request = new ApiClient.UpdateTestRequest(
            data.SubjectId,
            data.TestKind,
            data.Title,
            data.TimeLimitSec,
            data.AttemptsAllowed,
            data.Mode,
            data.IsPublished,
            data.IsPublic,
            data.IsStateArchive,
            data.Tasks.Select(t => new ApiClient.TestTaskRequest(t.TaskId, t.Position, t.Weight)).ToArray()
        );

        Console.WriteLine("Updating test...");
        var result = await _apiClient.UpdateTestAsync(testId, request, _cts.Token);

        if (!result.Success)
        {
            Console.WriteLine($"Failed to update test: {result.Error ?? "Unknown error"}");
            return false;
        }

        Console.WriteLine("Test updated successfully!");
        return true;
    }

    private async Task ManageStudentBindingsAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            Console.WriteLine("=== Manage Student Bindings ===");
            Console.WriteLine("1) Create invitation code");
            Console.WriteLine("2) View invitation codes");
            Console.WriteLine("3) Revoke invitation code");
            Console.WriteLine("4) Delete invitation code");
            Console.WriteLine("5) View my students");
            Console.WriteLine("6) Remove student");
            Console.WriteLine("0) Back");
            Console.Write("Select option: ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await CreateInvitationCodeAsync();
                    break;
                case "2":
                    await ViewInvitationCodesAsync();
                    break;
                case "3":
                    await RevokeInvitationCodeAsync();
                    break;
                case "4":
                    await DeleteInvitationCodeAsync();
                    break;
                case "5":
                    await ViewMyStudentsAsync();
                    break;
                case "6":
                    await RemoveStudentAsync();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }
        }
    }

    private async Task CreateInvitationCodeAsync()
    {
        Console.WriteLine("=== Create Invitation Code ===");
        
        int? maxUses = null;
        Console.Write("Enter maximum uses (or press Enter for unlimited): ");
        var maxUsesInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrWhiteSpace(maxUsesInput))
        {
            if (int.TryParse(maxUsesInput, out var max) && max > 0)
            {
                maxUses = max;
            }
            else
            {
                Console.WriteLine("Invalid input. Maximum uses must be a positive number.");
                return;
            }
        }

        DateTimeOffset? expiresAt = null;
        Console.Write("Enter expiration date (YYYY-MM-DD or press Enter for no expiration): ");
        var expiresInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrWhiteSpace(expiresInput))
        {
            if (DateTimeOffset.TryParse(expiresInput, out var expires))
            {
                expiresAt = expires;
            }
            else
            {
                Console.WriteLine("Invalid date format. Use YYYY-MM-DD format.");
                return;
            }
        }

        var request = new ApiClient.CreateInvitationCodeRequest(maxUses, expiresAt);
        Console.WriteLine("Creating invitation code...");
        
        var result = await _apiClient.CreateInvitationCodeAsync(request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        var code = result.Value!;
        Console.WriteLine();
        Console.WriteLine("Invitation code created successfully!");
        Console.WriteLine($"Code: {code.Code}");
        Console.WriteLine($"Max Uses: {(code.MaxUses.HasValue ? code.MaxUses.ToString() : "Unlimited")}");
        Console.WriteLine($"Expires At: {(code.ExpiresAt.HasValue ? code.ExpiresAt.Value.ToString("yyyy-MM-dd HH:mm") : "Never")}");
        Console.WriteLine();
    }

    private async Task ViewInvitationCodesAsync()
    {
        Console.WriteLine("=== Invitation Codes ===");
        
        var result = await _apiClient.GetInvitationCodesAsync(_cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        var codes = result.Value!;
        if (codes.Count == 0)
        {
            Console.WriteLine("No invitation codes found.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine();
        foreach (var code in codes)
        {
            var status = code.Status;
            var isExpired = code.ExpiresAt.HasValue && code.ExpiresAt.Value < DateTimeOffset.UtcNow;
            var isExhausted = code.MaxUses.HasValue && code.UsedCount >= code.MaxUses.Value;
            
            if (isExpired && status == "active")
            {
                status = "expired";
            }
            if (isExhausted && status == "active")
            {
                status = "exhausted";
            }

            Console.WriteLine($"ID: {code.Id}");
            Console.WriteLine($"  Code: {code.Code}");
            Console.WriteLine($"  Status: {status}");
            Console.WriteLine($"  Used: {code.UsedCount} / {(code.MaxUses.HasValue ? code.MaxUses.ToString() : "∞")}");
            Console.WriteLine($"  Expires: {(code.ExpiresAt.HasValue ? code.ExpiresAt.Value.ToString("yyyy-MM-dd HH:mm") : "Never")}");
            Console.WriteLine($"  Created: {code.CreatedAt:yyyy-MM-dd HH:mm}");
            Console.WriteLine();
        }
    }

    private async Task RevokeInvitationCodeAsync()
    {
        Console.WriteLine("=== Revoke Invitation Code ===");
        
        var result = await _apiClient.GetInvitationCodesAsync(_cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        var codes = result.Value!.Where(c => c.Status == "active").ToList();
        if (codes.Count == 0)
        {
            Console.WriteLine("No active invitation codes found.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("Active invitation codes:");
        for (int i = 0; i < codes.Count; i++)
        {
            var code = codes[i];
            Console.WriteLine($"{i + 1}) {code.Code} (ID: {code.Id}, Used: {code.UsedCount}/{code.MaxUses?.ToString() ?? "∞"})");
        }
        Console.WriteLine("0) Cancel");
        Console.Write("Select code to revoke: ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        if (choice == "0" || !int.TryParse(choice, out var index) || index < 1 || index > codes.Count)
        {
            return;
        }

        var selectedCode = codes[index - 1];
        Console.WriteLine($"Revoking code {selectedCode.Code}...");
        
        var revokeResult = await _apiClient.RevokeInvitationCodeAsync(selectedCode.Id, _cts.Token);
        if (!revokeResult.Success)
        {
            Console.WriteLine($"Error: {FormatError(revokeResult.Error)}");
            return;
        }

        Console.WriteLine("Invitation code revoked successfully.");
        Console.WriteLine();
    }

    private async Task DeleteInvitationCodeAsync()
    {
        Console.WriteLine("=== Delete Invitation Code ===");
        
        var result = await _apiClient.GetInvitationCodesAsync(_cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        var codes = result.Value!.ToList();
        if (codes.Count == 0)
        {
            Console.WriteLine("No invitation codes found.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("Invitation codes:");
        for (int i = 0; i < codes.Count; i++)
        {
            var code = codes[i];
            Console.WriteLine($"{i + 1}) {code.Code} (ID: {code.Id}, Status: {code.Status})");
        }
        Console.WriteLine("0) Cancel");
        Console.Write("Select code to delete: ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        if (choice == "0" || !int.TryParse(choice, out var index) || index < 1 || index > codes.Count)
        {
            return;
        }

        var selectedCode = codes[index - 1];
        Console.Write($"Are you sure you want to delete code {selectedCode.Code}? (y/n): ");
        var confirm = Console.ReadLine()?.Trim().ToLower();
        
        if (confirm != "y")
        {
            Console.WriteLine("Deletion cancelled.");
            return;
        }

        Console.WriteLine($"Deleting code {selectedCode.Code}...");
        
        var deleteResult = await _apiClient.DeleteInvitationCodeAsync(selectedCode.Id, _cts.Token);
        if (!deleteResult.Success)
        {
            Console.WriteLine($"Error: {FormatError(deleteResult.Error)}");
            return;
        }

        Console.WriteLine("Invitation code deleted successfully.");
        Console.WriteLine();
    }

    private async Task ViewMyStudentsAsync()
    {
        Console.WriteLine("=== My Students ===");
        
        var result = await _apiClient.GetMyStudentsAsync(_cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine();
            return;
        }

        var students = result.Value!;
        if (students.Count == 0)
        {
            Console.WriteLine("No students connected yet.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine();
        for (int i = 0; i < students.Count; i++)
        {
            var student = students.ElementAt(i);
            Console.WriteLine($"{i + 1}) {student.UserName} (ID: {student.Id})");
            Console.WriteLine($"   Email: {student.Email}");
            if (student.EstablishedAt.HasValue)
            {
                Console.WriteLine($"   Connected: {student.EstablishedAt.Value:yyyy-MM-dd HH:mm}");
            }
            Console.WriteLine();
        }
    }

    private async Task RemoveStudentAsync()
    {
        Console.WriteLine("=== Remove Student ===");
        
        var result = await _apiClient.GetMyStudentsAsync(_cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine();
            return;
        }

        var students = result.Value!.ToList();
        if (students.Count == 0)
        {
            Console.WriteLine("No students connected yet.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("Your students:");
        for (int i = 0; i < students.Count; i++)
        {
            var student = students[i];
            Console.WriteLine($"{i + 1}) {student.UserName} (ID: {student.Id})");
        }
        Console.WriteLine("0) Cancel");
        Console.Write("Enter student ID to remove: ");

        var input = Console.ReadLine()?.Trim();
        Console.WriteLine();

        if (input == "0" || !long.TryParse(input, out var studentId))
        {
            return;
        }

        // Проверяем, что студент существует в списке
        var studentToRemove = students.FirstOrDefault(s => s.Id == studentId);
        if (studentToRemove == null)
        {
            Console.WriteLine($"Student with ID {studentId} not found in your students list.");
            Console.WriteLine();
            return;
        }

        Console.Write($"Are you sure you want to remove {studentToRemove.UserName}? ");
        Console.WriteLine("This will revoke their access to all your tests. (y/n): ");
        var confirm = Console.ReadLine()?.Trim().ToLower();
        
        if (confirm != "y")
        {
            Console.WriteLine("Removal cancelled.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine($"Removing connection to {studentToRemove.UserName}...");
        
        var removeResult = await _apiClient.RemoveStudentAsync(studentId, _cts.Token);
        if (!removeResult.Success)
        {
            Console.WriteLine($"Error: {FormatError(removeResult.Error)}");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("Student removed successfully. Access to all your tests has been revoked.");
        Console.WriteLine();
    }
}

