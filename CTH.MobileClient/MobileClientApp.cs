using System;
using System.Collections.Generic;
using System.Linq;
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
        Console.WriteLine("3) Start attempt");
        Console.WriteLine("4) Logout");
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
        await WalkThroughAttemptAsync(attempt.AttemptId, testDetails);
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
                    goto FinishAttempt;
                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }

            Console.WriteLine();
        }

    FinishAttempt:
        var (finalAnswered, finalCorrect) = await LoadAttemptSummaryAsync(attemptId, orderedTasks.Length);
        Console.WriteLine($"Attempt {attemptId} summary: answered {finalAnswered}/{orderedTasks.Length}, correct {finalCorrect}.");
        var complete = Prompt("Complete attempt now? (y/n)", "y").Equals("y", StringComparison.OrdinalIgnoreCase);
        if (!complete)
        {
            Console.WriteLine("Attempt left in progress. You can complete it later from the menu.");
            return;
        }

        var completeRequest = new CompleteAttemptRequest((decimal?)finalCorrect, null, null);
        var completeResult = await _apiClient.CompleteAttemptAsync(attemptId, completeRequest, _cts.Token);
        Console.WriteLine(completeResult.Success
            ? $"Attempt completed. Score: {finalCorrect}/{orderedTasks.Length}"
            : $"Failed to complete attempt: {FormatError(completeResult.Error)}");
    }

    private async Task<bool> SubmitAnswerForTaskAsync(long attemptId, TestTask task)
    {
        var answerText = Prompt("Enter your answer (leave empty to skip)");
        var givenAnswer = string.IsNullOrWhiteSpace(answerText) ? string.Empty : $"{{\"value\":\"{answerText}\"}}";
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

    private async Task<(int answered, int correct)> GetAttemptProgressAsync(long attemptId, int totalTasks)
    {
        var attemptState = await _apiClient.GetAttemptAsync(attemptId, _cts.Token);
        if (!attemptState.Success || attemptState.Value == null)
        {
            Console.WriteLine($"Warning: unable to load attempt progress: {FormatError(attemptState.Error)}");
            return (0, 0);
        }

        var answers = attemptState.Value.Answers ?? Array.Empty<AttemptAnswer>();
        var answered = answers.Count;
        var correct = answers.Count(a => a.IsCorrect);
        return (Math.Min(answered, totalTasks), Math.Min(correct, totalTasks));
    }

    public void Dispose()
    {
        _cts.Cancel();
        _apiClient.Dispose();
        _cts.Dispose();
    }
}
