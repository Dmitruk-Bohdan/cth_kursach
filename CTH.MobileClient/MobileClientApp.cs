using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CTH.Common.Enums;
using CTH.Common.Extensions;
using CTH.Common.Helpers;
using static CTH.MobileClient.ApiClient;

namespace CTH.MobileClient;

public sealed class MobileClientApp : IDisposable
{
    private readonly ApiClient _apiClient;
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<long, string> _answerCache = new();

    public MobileClientApp(string baseUrl)
    {
        _apiClient = new ApiClient(baseUrl);
    }

    public async Task RunAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            if (_apiClient.IsAuthenticated)
            {
                await ShowAuthorizedMenuAsync();
            }
            else
            {
                await ShowAuthMenuAsync();
            }
        }
    }

    private async Task ShowAuthMenuAsync()
    {
        Console.WriteLine("=== Mobile Client ===");
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

    private async Task ShowAuthorizedMenuAsync()
    {
        Console.WriteLine("=== Mobile Client (authorized) ===");
        Console.WriteLine("1) List available tests");
        Console.WriteLine("2) View test details");
        Console.WriteLine("3) Start new attempt");
        Console.WriteLine("4) Continue interrupted attempt");
        Console.WriteLine("5) View attempt history");
        Console.WriteLine("6) View statistics");
        Console.WriteLine("7) Logout");
        Console.WriteLine("0) Exit");
        Console.Write("Select option: ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await ListTestsAsync();
                break;
            case "2":
                await ShowTestDetailsAsync();
                break;
            case "3":
                await StartAttemptAsync();
                break;
            case "4":
                await ContinueAttemptAsync();
                break;
            case "5":
                await ViewAttemptHistoryAsync();
                break;
            case "6":
                await ViewStatisticsAsync();
                break;
            case "7":
                await LogoutAsync();
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
        var request = new RegisterRequest
        {
            Email = Prompt("Email"),
            UserName = Prompt("User name"),
            Password = PromptSecure("Password"),
            RoleTypeId = PromptRole("Choose role (1 - student, 2 - teacher)", 1)
        };

        if (request.RoleTypeId is not (1 or 2))
        {
            Console.WriteLine("Registration cancelled: invalid role.");
            return;
        }

        var result = await _apiClient.RegisterAsync(request, _cts.Token);
        if (result.Success)
        {
            Console.WriteLine($"Registered and logged in as {result.Value!.UserName}");
        }
        else
        {
            Console.WriteLine($"Registration failed: {result.Error}");
        }
    }

    private async Task LoginAsync()
    {
        var email = Prompt("Email");
        var password = PromptSecure("Password");

        var result = await _apiClient.LoginAsync(email, password, _cts.Token);
        if (result.Success)
        {
            Console.WriteLine($"Welcome, {result.Value!.UserName}");
        }
        else
        {
            Console.WriteLine($"Login failed: {result.Error}");
        }
    }

    private async Task LogoutAsync()
    {
        var result = await _apiClient.LogoutAsync(_cts.Token);
        if (result.Success)
        {
            Console.WriteLine("Logged out.");
            _answerCache.Clear();
        }
        else
        {
            Console.WriteLine($"Logout failed: {FormatError(result.Error)}");
        }
    }

    private async Task ListTestsAsync()
    {
        var result = await _apiClient.GetPublishedTestsAsync(null, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        Console.WriteLine("Available tests:");
        foreach (var test in result.Value!)
        {
            Console.WriteLine($"- #{test.Id}: {test.Title} ({test.TestKind}) | Subject: {FormatSubject(test.SubjectId)} | Public: {test.IsPublic}, State: {test.IsStateArchive}");
        }
    }

    private async Task ShowTestDetailsAsync()
    {
        if (!long.TryParse(Prompt("Test ID"), out var testId))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var result = await _apiClient.GetTestDetailsAsync(testId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        var details = result.Value!;
        Console.WriteLine($"Test #{details.Id}: {details.Title}");
        Console.WriteLine($"Type: {details.TestKind}, Subject: {FormatSubject(details.SubjectId)}");
        Console.WriteLine($"Time limit: {(details.TimeLimitSec.HasValue ? $"{details.TimeLimitSec.Value} sec" : "Unlimited")}");
        Console.WriteLine($"Attempts allowed: {(details.AttemptsAllowed.HasValue ? details.AttemptsAllowed : "unlimited")}");
        Console.WriteLine($"Public: {details.IsPublic}, State archive: {details.IsStateArchive}");
        Console.WriteLine("Tasks:");
        foreach (var task in details.Tasks)
        {
            Console.WriteLine($"  [{task.Position}] Task #{task.TaskId} ({task.TaskType}) diff {task.Difficulty}");
            Console.WriteLine($"      {task.Statement}");
            if (!string.IsNullOrWhiteSpace(task.Explanation))
            {
                Console.WriteLine($"      Hint: {task.Explanation}");
            }
        }
    }

    private async Task StartAttemptAsync()
    {
        if (!long.TryParse(Prompt("Test ID to start"), out var testId))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        _answerCache.Clear();
        var testDetailsResult = await _apiClient.GetTestDetailsAsync(testId, _cts.Token);
        if (!testDetailsResult.Success)
        {
            Console.WriteLine($"Error: {FormatError(testDetailsResult.Error)}");
            return;
        }

        var testDetails = testDetailsResult.Value!;
        if (testDetails.Tasks.Count == 0)
        {
            Console.WriteLine("Selected test has no tasks.");
            return;
        }

        var result = await _apiClient.StartAttemptAsync(testId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        var attempt = result.Value!;
        Console.WriteLine($"Attempt #{attempt.AttemptId} started at {attempt.StartedAt}. Status: {attempt.Status}");
        
        // Загружаем существующие ответы, если попытка уже была начата ранее
        await SyncAnswerStateAsync(attempt.AttemptId, testDetails.Tasks.Count);
        
        await WalkThroughAttemptAsync(attempt.AttemptId, testDetails);
    }

    private async Task ContinueAttemptAsync()
    {
        // Получаем как in_progress, так и aborted попытки
        var inProgressResult = await _apiClient.GetAttemptsAsync("in_progress", 100, 0, _cts.Token);
        var abortedResult = await _apiClient.GetAttemptsAsync("aborted", 100, 0, _cts.Token);
        
        if (!inProgressResult.Success || !abortedResult.Success)
        {
            Console.WriteLine($"Error: {FormatError(inProgressResult.Error ?? abortedResult.Error)}");
            return;
        }

        var allAttempts = new List<AttemptListItem>();
        allAttempts.AddRange(inProgressResult.Value ?? Array.Empty<AttemptListItem>());
        allAttempts.AddRange(abortedResult.Value ?? Array.Empty<AttemptListItem>());
        
        if (allAttempts.Count == 0)
        {
            Console.WriteLine("No in-progress or aborted attempts found.");
            return;
        }

        Console.WriteLine("Available attempts to continue:");
        var attemptList = allAttempts.OrderByDescending(a => a.StartedAt).ToList();
        for (int i = 0; i < attemptList.Count; i++)
        {
            var attempt = attemptList[i];
            var elapsed = DateTimeOffset.UtcNow - attempt.StartedAt;
            var statusDisplay = attempt.Status == "aborted" ? " (Aborted - will be resumed)" : "";
            Console.WriteLine($"{i + 1}) Attempt #{attempt.Id}: {attempt.TestTitle} ({attempt.SubjectName}){statusDisplay}");
            Console.WriteLine($"   Started: {attempt.StartedAt:yyyy-MM-dd HH:mm:ss} ({elapsed.TotalMinutes:F1} minutes ago)");
        }

        Console.Write("Select attempt number to continue (0 to cancel): ");
        if (!int.TryParse(Console.ReadLine(), out var choice) || choice < 1 || choice > attemptList.Count)
        {
            if (choice != 0)
            {
                Console.WriteLine("Invalid selection.");
            }
            return;
        }

        var selectedAttempt = attemptList[choice - 1];
        _answerCache.Clear();
        
        // Если попытка aborted, сначала возобновляем её
        if (selectedAttempt.Status == "aborted")
        {
            Console.WriteLine($"Resuming aborted attempt #{selectedAttempt.Id}...");
            var resumeResult = await _apiClient.ResumeAttemptAsync(selectedAttempt.Id, _cts.Token);
            if (!resumeResult.Success)
            {
                Console.WriteLine($"Error resuming attempt: {FormatError(resumeResult.Error)}");
                return;
            }
            Console.WriteLine("Attempt resumed successfully.");
        }
        
        // Загружаем детали теста
        var testDetailsResult = await _apiClient.GetTestDetailsAsync(selectedAttempt.TestId, _cts.Token);
        if (!testDetailsResult.Success)
        {
            Console.WriteLine($"Error loading test details: {FormatError(testDetailsResult.Error)}");
            return;
        }

        var testDetails = testDetailsResult.Value!;
        Console.WriteLine($"Continuing attempt #{selectedAttempt.Id}...");
        
        // Проверяем статус попытки - если она уже завершена, не можем продолжить
        var attemptStatusResult = await _apiClient.GetAttemptAsync(selectedAttempt.Id, _cts.Token);
        if (attemptStatusResult.Success && attemptStatusResult.Value != null)
        {
            if (!string.Equals(attemptStatusResult.Value.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Attempt #{selectedAttempt.Id} is already {attemptStatusResult.Value.Status}. Cannot continue.");
                return;
            }
        }
        
        // Проверяем таймер, если он установлен
        if (testDetails.TimeLimitSec.HasValue)
        {
            var elapsed = (DateTimeOffset.UtcNow - selectedAttempt.StartedAt).TotalSeconds;
            var remaining = testDetails.TimeLimitSec.Value - (int)elapsed;
            
            if (remaining <= 0)
            {
                Console.WriteLine($"Time limit expired. Attempt will be completed automatically.");
                // Автоматически завершаем попытку
                var (_, finalCorrect) = await LoadAttemptSummaryAsync(selectedAttempt.Id, testDetails.Tasks.Count);
                var completeRequest = new CompleteAttemptRequest((decimal?)finalCorrect, null, (int)elapsed);
                var completeResult = await _apiClient.CompleteAttemptAsync(selectedAttempt.Id, completeRequest, _cts.Token);
                Console.WriteLine(completeResult.Success
                    ? $"Attempt completed. Score: {finalCorrect}/{testDetails.Tasks.Count}"
                    : $"Failed to complete attempt: {FormatError(completeResult.Error)}");
                return;
            }
            
            Console.WriteLine($"Time remaining: {remaining / 60:F0} minutes {remaining % 60:F0} seconds");
        }
        
        // Загружаем существующие ответы
        await SyncAnswerStateAsync(selectedAttempt.Id, testDetails.Tasks.Count);
        
        await WalkThroughAttemptAsync(selectedAttempt.Id, testDetails);
    }

    private async Task ViewAttemptHistoryAsync()
    {
        Console.WriteLine("=== Attempt History ===");
        Console.WriteLine("Filter by status:");
        Console.WriteLine("1) All attempts");
        Console.WriteLine("2) In progress");
        Console.WriteLine("3) Completed");
        Console.WriteLine("4) Aborted");
        Console.Write("Choose filter (1-4): ");
        var filterChoice = Console.ReadLine();
        Console.WriteLine();

        string? statusFilter = null;
        switch (filterChoice)
        {
            case "1":
                statusFilter = null; // Все
                break;
            case "2":
                statusFilter = "in_progress";
                break;
            case "3":
                statusFilter = "completed";
                break;
            case "4":
                statusFilter = "aborted";
                break;
            default:
                Console.WriteLine("Invalid filter. Showing all attempts.");
                break;
        }

        var result = await _apiClient.GetAttemptsAsync(statusFilter, 100, 0, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        var attempts = result.Value!;
        if (attempts.Count == 0)
        {
            Console.WriteLine("No attempts found.");
            return;
        }

        Console.WriteLine($"Found {attempts.Count} attempt(s):");
        var attemptList = attempts.ToList();
        for (int i = 0; i < attemptList.Count; i++)
        {
            var attempt = attemptList[i];
            var elapsed = attempt.FinishedAt.HasValue 
                ? (attempt.FinishedAt.Value - attempt.StartedAt).TotalMinutes 
                : (DateTimeOffset.UtcNow - attempt.StartedAt).TotalMinutes;
            var statusDisplay = attempt.Status switch
            {
                "in_progress" => "In Progress",
                "completed" => "Completed",
                "aborted" => "Aborted",
                _ => attempt.Status
            };
            
            Console.WriteLine($"{i + 1}) Attempt #{attempt.Id}: {attempt.TestTitle} ({attempt.SubjectName})");
            Console.WriteLine($"   Status: {statusDisplay}");
            Console.WriteLine($"   Started: {attempt.StartedAt:yyyy-MM-dd HH:mm:ss}");
            if (attempt.FinishedAt.HasValue)
            {
                Console.WriteLine($"   Finished: {attempt.FinishedAt.Value:yyyy-MM-dd HH:mm:ss}");
            }
            Console.WriteLine($"   Duration: {elapsed:F1} minutes");
            if (attempt.RawScore.HasValue && (attempt.Status == "completed" || attempt.Status == "aborted"))
            {
                Console.WriteLine($"   Score: {attempt.RawScore.Value:F1}");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("Press Enter to return to menu...");
        Console.ReadLine();
    }

    private async Task ViewStatisticsAsync()
    {
        Console.WriteLine("=== Statistics ===");
        Console.WriteLine("1) Statistics by subject");
        Console.WriteLine("2) Statistics by topic");
        Console.Write("Choose option: ");
        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await ViewStatisticsBySubjectAsync();
                break;
            case "2":
                await ViewStatisticsByTopicAsync();
                break;
            default:
                Console.WriteLine("Unknown option.");
                break;
        }
    }

    private async Task ViewStatisticsBySubjectAsync()
    {
        Console.Write("Enter subject ID (or press Enter for all subjects): ");
        var subjectIdInput = Console.ReadLine();
        long? subjectId = null;
        if (!string.IsNullOrWhiteSpace(subjectIdInput) && long.TryParse(subjectIdInput, out var parsedId))
        {
            subjectId = parsedId;
        }

        var result = await _apiClient.GetStatisticsBySubjectAsync(subjectId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        var statistics = result.Value!;
        if (statistics.Count == 0)
        {
            Console.WriteLine("No statistics found.");
            return;
        }

        Console.WriteLine($"=== Statistics by Subject ===");
        foreach (var stat in statistics)
        {
            Console.WriteLine($"Subject: {stat.SubjectName ?? "Unknown"}");
            Console.WriteLine($"  Attempts: {stat.AttemptsTotal}");
            Console.WriteLine($"  Correct answers: {stat.CorrectTotal}");
            if (stat.AccuracyPercentage.HasValue)
            {
                Console.WriteLine($"  Accuracy: {stat.AccuracyPercentage.Value:F1}%");
            }
            if (stat.AverageScore.HasValue)
            {
                Console.WriteLine($"  Average score: {stat.AverageScore.Value:F2}");
            }
            if (stat.AverageTimeSec.HasValue)
            {
                Console.WriteLine($"  Average time: {stat.AverageTimeSec.Value / 60.0:F1} minutes");
            }
            if (stat.LastAttemptAt.HasValue)
            {
                Console.WriteLine($"  Last attempt: {stat.LastAttemptAt.Value:yyyy-MM-dd HH:mm:ss}");
            }
            Console.WriteLine();
        }
    }

    private async Task ViewStatisticsByTopicAsync()
    {
        Console.Write("Enter subject ID (or press Enter for all subjects): ");
        var subjectIdInput = Console.ReadLine();
        long? subjectId = null;
        if (!string.IsNullOrWhiteSpace(subjectIdInput) && long.TryParse(subjectIdInput, out var parsedId))
        {
            subjectId = parsedId;
        }

        var result = await _apiClient.GetStatisticsByTopicAsync(subjectId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        var statistics = result.Value!;
        if (statistics.Count == 0)
        {
            Console.WriteLine("No statistics found.");
            return;
        }

        Console.WriteLine($"=== Statistics by Topic ===");
        var groupedBySubject = statistics.GroupBy(s => s.SubjectName ?? "Unknown");
        foreach (var subjectGroup in groupedBySubject)
        {
            Console.WriteLine($"Subject: {subjectGroup.Key}");
            foreach (var stat in subjectGroup)
            {
                Console.WriteLine($"  Topic: {stat.TopicName ?? "Unknown"}");
                Console.WriteLine($"    Attempts: {stat.AttemptsTotal}");
                Console.WriteLine($"    Correct answers: {stat.CorrectTotal}");
                if (stat.AccuracyPercentage.HasValue)
                {
                    Console.WriteLine($"    Accuracy: {stat.AccuracyPercentage.Value:F1}%");
                }
                if (stat.LastAttemptAt.HasValue)
                {
                    Console.WriteLine($"    Last attempt: {stat.LastAttemptAt.Value:yyyy-MM-dd HH:mm:ss}");
                }
            }
            Console.WriteLine();
        }
    }

    private static string Prompt(string message, string? defaultValue = null)
    {
        Console.Write(string.IsNullOrEmpty(defaultValue)
            ? $"{message}: "
            : $"{message} [{defaultValue}]: ");
        var input = Console.ReadLine();
        return string.IsNullOrEmpty(input) ? defaultValue ?? string.Empty : input;
    }

    private static string PromptSecure(string message)
    {
        Console.Write($"{message}: ");
        var pwd = string.Empty;
        ConsoleKey key;
        while ((key = Console.ReadKey(intercept: true).Key) != ConsoleKey.Enter)
        {
            if (key == ConsoleKey.Backspace && pwd.Length > 0)
            {
                pwd = pwd[..^1];
                Console.Write("\b \b");
            }
            else if (!char.IsControl((char)key))
            {
                pwd += (char)key;
                Console.Write('*');
            }
        }
        Console.WriteLine();
        return pwd;
    }

    private static int PromptRole(string message, int defaultRole)
    {
        var input = Prompt(message, defaultRole.ToString());
        return int.TryParse(input, out var role) ? role : defaultRole;
    }

    private static string FormatError(string? error)
        => string.IsNullOrWhiteSpace(error) ? "Unknown error." : error;

    private static string FormatSubject(long subjectId)
    {
        if (EnumHelper.TryParseFromInt<SubjectTypeEnum>((int)subjectId, out var subject))
        {
            return subject.Description();
        }

        return $"Предмет #{subjectId}";
    }

    private async Task WalkThroughAttemptAsync(long attemptId, TestDetails testDetails)
    {
        var orderedTasks = testDetails.Tasks.OrderBy(t => t.Position).ToArray();
        if (orderedTasks.Length == 0)
        {
            Console.WriteLine("Test has no tasks.");
            return;
        }

        var currentIndex = 0;
        var answered = await SyncAnswerStateAsync(attemptId, orderedTasks.Length);

        while (true)
        {
            var task = orderedTasks[currentIndex];
            Console.WriteLine($"--- Task {currentIndex + 1}/{orderedTasks.Length} ---");
            Console.WriteLine($"Type: {task.TaskType}, Difficulty: {task.Difficulty}");
            Console.WriteLine(task.Statement);
            if (!string.IsNullOrWhiteSpace(task.Explanation))
            {
                Console.WriteLine($"Hint: {task.Explanation}");
            }

            Console.WriteLine($"Progress: answered {answered}/{orderedTasks.Length}");
            Console.WriteLine("1) Submit or update answer");
            if (currentIndex < orderedTasks.Length - 1)
            {
                Console.WriteLine("2) Next question");
            }
            if (currentIndex > 0)
            {
                Console.WriteLine("3) Previous question");
            }
            Console.WriteLine("4) Finish attempt");
            if (_answerCache.TryGetValue(task.TaskId, out var currentAnswer) && !string.IsNullOrWhiteSpace(currentAnswer))
            {
                Console.WriteLine($"Current answer: {currentAnswer}");
            }
            Console.Write("Choose option: ");
            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    if (await SubmitAnswerForTaskAsync(attemptId, task))
                    {
                        answered = await SyncAnswerStateAsync(attemptId, orderedTasks.Length);
                        Console.WriteLine("Answer saved.");
                    }
                    break;
                case "2":
                    if (currentIndex < orderedTasks.Length - 1)
                    {
                        currentIndex++;
                    }
                    else
                    {
                        Console.WriteLine("This is the last question.");
                    }
                    break;
                case "3":
                    if (currentIndex > 0)
                    {
                        currentIndex--;
                    }
                    else
                    {
                        Console.WriteLine("This is the first question.");
                    }
                    break;
                case "4":
                    // Показываем summary и предлагаем выбор действия
                    var finalAnswered = await SyncAnswerStateAsync(attemptId, orderedTasks.Length);
                    Console.WriteLine($"Attempt {attemptId} summary: answered {finalAnswered}/{orderedTasks.Length}.");
                    Console.WriteLine();
                    Console.WriteLine("1) Complete attempt");
                    Console.WriteLine("2) Abort attempt");
                    Console.WriteLine("3) Cancel");
                    Console.Write("Choose option: ");
                    var actionChoice = Console.ReadLine();
                    Console.WriteLine();
                    
                    switch (actionChoice)
                    {
                        case "1":
                            // Завершить попытку - подсчитываем результаты и завершаем
                            Console.WriteLine("Completing attempt...");
                            var (_, finalCorrect) = await LoadAttemptSummaryAsync(attemptId, orderedTasks.Length);
                            var completeRequest = new CompleteAttemptRequest((decimal?)finalCorrect, null, null);
                            var completeResult = await _apiClient.CompleteAttemptAsync(attemptId, completeRequest, _cts.Token);
                            Console.WriteLine(completeResult.Success
                                ? $"Attempt completed. Score: {finalCorrect}/{orderedTasks.Length}"
                                : $"Failed to complete attempt: {FormatError(completeResult.Error)}");
                            return; // Выходим из метода после завершения попытки
                            
                        case "2":
                            // Прервать попытку - помечаем как aborted
                            Console.WriteLine("Aborting attempt...");
                            var abortResult = await _apiClient.AbortAttemptAsync(attemptId, _cts.Token);
                            Console.WriteLine(abortResult.Success
                                ? "Attempt aborted. You can start a new attempt later."
                                : $"Failed to abort attempt: {FormatError(abortResult.Error)}");
                            return; // Выходим из метода после прерывания попытки
                            
                        case "3":
                            // Отмена - возвращаемся к тесту
                            Console.WriteLine("Returning to test.");
                            Console.WriteLine();
                            continue; // Продолжаем цикл - возвращаемся к текущему вопросу
                            
                        default:
                            Console.WriteLine("Unknown option. Returning to test.");
                            Console.WriteLine();
                            continue; // Продолжаем цикл при неизвестной опции
                    }
                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }

            Console.WriteLine();
        }
    }

    private async Task<bool> SubmitAnswerForTaskAsync(long attemptId, TestTask task)
    {
        var answerText = Prompt("Enter your answer (leave empty to skip)");
        // Храним просто JSON-строку вместо объекта {"value": "..."}
        var givenAnswer = string.IsNullOrWhiteSpace(answerText) 
            ? "\"\"" 
            : JsonSerializer.Serialize(answerText);
        var submitRequest = new SubmitAnswerRequest(task.TaskId, givenAnswer, null);
        var submitResult = await _apiClient.SubmitAnswerAsync(attemptId, submitRequest, _cts.Token);
        if (!submitResult.Success)
        {
            Console.WriteLine($"Failed to submit answer: {FormatError(submitResult.Error)}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(answerText))
        {
            _answerCache.Remove(task.TaskId);
        }
        else
        {
            _answerCache[task.TaskId] = answerText;
        }

        return true;
    }

    private async Task<int> SyncAnswerStateAsync(long attemptId, int totalTasks)
    {
        var attemptState = await _apiClient.GetAttemptAsync(attemptId, _cts.Token);
        if (!attemptState.Success || attemptState.Value == null)
        {
            Console.WriteLine($"Warning: unable to load attempt progress: {FormatError(attemptState.Error)}");
            return 0;
        }

        var answers = attemptState.Value.Answers ?? Array.Empty<AttemptAnswer>();
        
        // Загружаем ответы в кэш, извлекая значение из JSON
        foreach (var answer in answers)
        {
            // Проверяем, что GivenAnswer не null и не пустой
            if (string.IsNullOrWhiteSpace(answer.GivenAnswer))
            {
                _answerCache.Remove(answer.TaskId);
                continue;
            }

            var answerValue = ExtractAnswerValueFromJson(answer.GivenAnswer);
            if (!string.IsNullOrWhiteSpace(answerValue))
            {
                _answerCache[answer.TaskId] = answerValue;
            }
            else
            {
                // Если не удалось извлечь, очищаем кэш для этого задания
                _answerCache.Remove(answer.TaskId);
            }
        }

        // Возвращаем только количество отвеченных (без правильных)
        var answered = answers.Count;
        return Math.Min(answered, totalTasks);
    }

    private async Task<(int answered, int correct)> LoadAttemptSummaryAsync(long attemptId, int totalTasks)
    {
        var attemptState = await _apiClient.GetAttemptAsync(attemptId, _cts.Token);
        if (!attemptState.Success || attemptState.Value == null)
        {
            Console.WriteLine($"Warning: unable to load attempt summary: {FormatError(attemptState.Error)}");
            return (0, 0);
        }

        var answers = attemptState.Value.Answers ?? Array.Empty<AttemptAnswer>();
        var answered = answers.Count;
        // Теперь можно показать правильные ответы, так как тест завершается
        var correct = answers.Count(a => a.IsCorrect);
        return (Math.Min(answered, totalTasks), Math.Min(correct, totalTasks));
    }

    private static string? ExtractAnswerValueFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var trimmed = json.Trim();
        
        // Если это не JSON (не начинается с " или { или [), возвращаем как есть
        if (!trimmed.StartsWith("\"", StringComparison.Ordinal) && 
            !trimmed.StartsWith("{", StringComparison.Ordinal) && 
            !trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            return trimmed;
        }

        try
        {
            // Парсим как JSON - ожидаем строку или объект с полем "value" (для обратной совместимости)
            using var document = JsonDocument.Parse(trimmed);
            
            // Если это JSON-строка (самый простой случай)
            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                return document.RootElement.GetString();
            }
            
            // Если это объект с полем "value" (для обратной совместимости со старыми данными)
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("value", out var valueProperty))
            {
                if (valueProperty.ValueKind == JsonValueKind.String)
                {
                    return valueProperty.GetString();
                }
                return valueProperty.GetRawText().Trim('"');
            }
            
            // Для других типов возвращаем как строку
            return document.RootElement.GetRawText();
        }
        catch (JsonException)
        {
            // Если не JSON, убираем кавычки если есть
            if (trimmed.StartsWith("\"", StringComparison.Ordinal) && 
                trimmed.EndsWith("\"", StringComparison.Ordinal) && 
                trimmed.Length > 1)
            {
                return trimmed.Substring(1, trimmed.Length - 2);
            }
            return trimmed;
        }
        catch (Exception)
        {
            return trimmed;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _apiClient.Dispose();
        _cts.Dispose();
    }
}
