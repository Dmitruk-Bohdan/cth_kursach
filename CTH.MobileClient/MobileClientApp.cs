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
        Console.WriteLine("7) View recommendations");
        Console.WriteLine("8) My teachers");
        Console.WriteLine("9) My mixed tests");
        Console.WriteLine("10) Logout");
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
                await ViewRecommendationsAsync();
                break;
            case "8":
                await ShowMyTeachersAsync();
                break;
            case "9":
                await ShowMyMixedTestsAsync();
                break;
            case "10":
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
        Console.WriteLine("=== Filter Tests ===");
        Console.WriteLine("Select filtering options:");
        Console.WriteLine();

        
        long? subjectId = null;
        Console.WriteLine("Filter by subject? (y/n): ");
        if (Console.ReadLine()?.Trim().ToLower() == "y")
        {
            var subjectsResult = await _apiClient.GetAllSubjectsAsync(_cts.Token);
            if (subjectsResult.Success && subjectsResult.Value != null && subjectsResult.Value.Any())
            {
                Console.WriteLine("Available subjects:");
                var subjects = subjectsResult.Value.ToList();
                for (int i = 0; i < subjects.Count; i++)
                {
                    Console.WriteLine($"{i + 1}) {subjects[i].SubjectName}");
                }
                Console.Write("Select subject number (or 0 to skip): ");
                if (int.TryParse(Console.ReadLine(), out var subjectIndex) && subjectIndex > 0 && subjectIndex <= subjects.Count)
                {
                    subjectId = subjects[subjectIndex - 1].Id;
                }
            }
        }

        
        Console.Write("Show only tests from my teachers? (y/n): ");
        var onlyTeachers = Console.ReadLine()?.Trim().ToLower() == "y";

        
        Console.Write("Show only state archive tests? (y/n): ");
        var onlyStateArchive = Console.ReadLine()?.Trim().ToLower() == "y";

        
        Console.Write("Show only tests with limited attempts? (y/n): ");
        var onlyLimitedAttempts = Console.ReadLine()?.Trim().ToLower() == "y";

        
        Console.Write("Search by title (leave empty to skip): ");
        var title = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            title = null;
        }

        
        string? mode = null;
        Console.WriteLine("Filter by test mode? (y/n): ");
        if (Console.ReadLine()?.Trim().ToLower() == "y")
        {
            Console.WriteLine("Select test mode:");
            Console.WriteLine("1) training - Training mode");
            Console.WriteLine("2) exam - Exam mode");
            Console.WriteLine("3) control - Control work mode");
            Console.Write("Enter option (1-3, or leave empty to skip): ");
            var modeChoice = Console.ReadLine()?.Trim();
            mode = modeChoice switch
            {
                "1" => "training",
                "2" => "exam",
                "3" => "control",
                _ => null
            };
        }

        Console.WriteLine();
        Console.WriteLine("Loading tests...");
        Console.WriteLine();

        var result = await _apiClient.GetPublishedTestsAsync(
            subjectId: subjectId,
            onlyTeachers: onlyTeachers,
            onlyStateArchive: onlyStateArchive,
            onlyLimitedAttempts: onlyLimitedAttempts,
            title: title,
            mode: mode,
            cancellationToken: _cts.Token);

        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        
        var activeFilters = new List<string>();
        if (subjectId.HasValue)
        {
            activeFilters.Add($"Subject: {FormatSubject(subjectId.Value)}");
        }
        if (onlyTeachers)
        {
            activeFilters.Add("From my teachers");
        }
        if (onlyStateArchive)
        {
            activeFilters.Add("State archive only");
        }
        if (onlyLimitedAttempts)
        {
            activeFilters.Add("Limited attempts only");
        }
        if (!string.IsNullOrWhiteSpace(title))
        {
            activeFilters.Add($"Title contains: {title}");
        }
        if (!string.IsNullOrWhiteSpace(mode))
        {
            activeFilters.Add($"Mode: {mode}");
        }

        if (activeFilters.Any())
        {
            Console.WriteLine($"Active filters: {string.Join(", ", activeFilters)}");
            Console.WriteLine();
        }

        var tests = result.Value!;
        if (!tests.Any())
        {
            Console.WriteLine("No tests found matching the criteria.");
            return;
        }

        Console.WriteLine($"Found {tests.Count} test(s):");
        foreach (var test in tests)
        {
            var modeInfo = !string.IsNullOrWhiteSpace(test.Mode) ? $" | Mode: {test.Mode}" : "";
            Console.WriteLine($"- #{test.Id}: {test.Title} ({test.TestKind}){modeInfo} | Subject: {FormatSubject(test.SubjectId)} | Public: {test.IsPublic}, State: {test.IsStateArchive}");
            if (test.AttemptsAllowed.HasValue)
            {
                Console.WriteLine($"  Attempts allowed: {test.AttemptsAllowed}");
            }
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
        Console.WriteLine($"Time limit: {(details.TimeLimitSec.HasValue ? $"{details.TimeLimitSec.Value / 60} minute(s)" : "Unlimited")}");
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
        
        
        await SyncAnswerStateAsync(attempt.AttemptId, testDetails.Tasks.Count);
        
        await WalkThroughAttemptAsync(attempt.AttemptId, testDetails);
    }

    private async Task ContinueAttemptAsync()
    {
        
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
        
        
        var testDetailsResult = await _apiClient.GetTestDetailsAsync(selectedAttempt.TestId, _cts.Token);
        if (!testDetailsResult.Success)
        {
            Console.WriteLine($"Error loading test details: {FormatError(testDetailsResult.Error)}");
            return;
        }

        var testDetails = testDetailsResult.Value!;
        Console.WriteLine($"Continuing attempt #{selectedAttempt.Id}...");
        
        
        var attemptStatusResult = await _apiClient.GetAttemptAsync(selectedAttempt.Id, _cts.Token);
        if (attemptStatusResult.Success && attemptStatusResult.Value != null)
        {
            if (!string.Equals(attemptStatusResult.Value.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Attempt #{selectedAttempt.Id} is already {attemptStatusResult.Value.Status}. Cannot continue.");
                return;
            }
        }
        
        
        if (testDetails.TimeLimitSec.HasValue)
        {
            var elapsed = (DateTimeOffset.UtcNow - selectedAttempt.StartedAt).TotalSeconds;
            var remaining = testDetails.TimeLimitSec.Value - (int)elapsed;
            
            if (remaining <= 0)
            {
                Console.WriteLine($"Time limit expired. Attempt will be completed automatically.");
                
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
                statusFilter = null; 
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
        Console.WriteLine("Options:");
        Console.WriteLine("1) View attempt details");
        Console.WriteLine("0) Back to menu");
        Console.Write("Choose option: ");
        var choice = Console.ReadLine();
        Console.WriteLine();

        if (choice == "1" && attemptList.Count > 0)
        {
            Console.Write("Enter attempt number to view details: ");
            if (int.TryParse(Console.ReadLine(), out var attemptNumber) && attemptNumber >= 1 && attemptNumber <= attemptList.Count)
            {
                var selectedAttempt = attemptList[attemptNumber - 1];
                await ShowAttemptDetailsAsync(selectedAttempt.Id);
            }
            else
            {
                Console.WriteLine("Invalid attempt number.");
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }
        }
    }

    private async Task ViewStatisticsAsync()
    {
        
        var subjectsResult = await _apiClient.GetAllSubjectsAsync(_cts.Token);
        if (!subjectsResult.Success)
        {
            Console.WriteLine($"Error: {FormatError(subjectsResult.Error)}");
            return;
        }

        var subjects = subjectsResult.Value!;
        if (subjects.Count == 0)
        {
            Console.WriteLine("No subjects found.");
            return;
        }

        
        Console.WriteLine("=== Select Subject ===");
        for (int i = 0; i < subjects.Count; i++)
        {
            Console.WriteLine($"{i + 1}) {subjects.ElementAt(i).SubjectName} (ID: {subjects.ElementAt(i).Id})");
        }
        Console.Write("Enter subject number: ");
        var choice = Console.ReadLine();
        Console.WriteLine();

        if (!int.TryParse(choice, out var subjectIndex) || subjectIndex < 1 || subjectIndex > subjects.Count)
        {
            Console.WriteLine("Invalid selection.");
            return;
        }

        var selectedSubject = subjects.ElementAt(subjectIndex - 1);
        await ShowSubjectStatisticsAsync(selectedSubject.Id, selectedSubject.SubjectName);
    }

    private async Task ShowSubjectStatisticsAsync(long subjectId, string subjectName)
    {
        var result = await _apiClient.GetSubjectStatisticsAsync(subjectId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        var stats = result.Value!;

        Console.WriteLine($"=== Statistics for {subjectName} ===");
        Console.WriteLine();

        
        if (stats.OverallAccuracyPercentage.HasValue)
        {
            Console.WriteLine($"Overall accuracy: {stats.OverallAccuracyPercentage.Value:F1}%");
            Console.WriteLine($"Total attempts: {stats.OverallAttemptsTotal}");
            Console.WriteLine($"Correct answers: {stats.OverallCorrectTotal}");
        }
        else
        {
            Console.WriteLine("No attempts yet for this subject.");
        }
        Console.WriteLine();

        
        if (stats.Top3ErrorTopics.Count > 0)
        {
            Console.WriteLine("Top 3 topics with most errors:");
            for (int i = 0; i < stats.Top3ErrorTopics.Count; i++)
            {
                var topic = stats.Top3ErrorTopics.ElementAt(i);
                Console.WriteLine($"  - {topic.TopicName}");
                Console.WriteLine($"    Errors: {topic.ErrorsCount}");
                Console.WriteLine($"    Accuracy: {(topic.AccuracyPercentage.HasValue ? $"{topic.AccuracyPercentage.Value:F1}%" : "N/A")}");
                Console.WriteLine($"    Completed tasks: {topic.AttemptsTotal}");
                if (i < stats.Top3ErrorTopics.Count - 1)
                {
                    Console.WriteLine();
                }
            }
            Console.WriteLine();
        }

        
        if (stats.OtherTopics.Count > 0)
        {
            Console.WriteLine("Other topics (sorted by accuracy, lowest first):");
            for (int i = 0; i < stats.OtherTopics.Count; i++)
            {
                var topic = stats.OtherTopics.ElementAt(i);
                Console.WriteLine($"  - {topic.TopicName}");
                Console.WriteLine($"    Accuracy: {(topic.AccuracyPercentage.HasValue ? $"{topic.AccuracyPercentage.Value:F1}%" : "N/A")}");
                Console.WriteLine($"    Errors: {topic.ErrorsCount}");
                Console.WriteLine($"    Completed tasks: {topic.AttemptsTotal}");
                if (i < stats.OtherTopics.Count - 1)
                {
                    Console.WriteLine();
                }
            }
            Console.WriteLine();
        }

        
        if (stats.UnattemptedTopics.Count > 0)
        {
            Console.WriteLine("Topics not yet attempted:");
            foreach (var topic in stats.UnattemptedTopics)
            {
                Console.WriteLine($"  - {topic.TopicName}");
            }
            Console.WriteLine();
        }

        
        Console.WriteLine("Options:");
        Console.WriteLine("1) Create custom test for problematic topics");
        Console.WriteLine("0) Back to menu");
        Console.Write("Choose option: ");
        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                
                var problematicTopics = new List<ApiClient.TopicStatistics>();
                problematicTopics.AddRange(stats.Top3ErrorTopics);
                problematicTopics.AddRange(stats.OtherTopics);
                
                
                var validTopics = problematicTopics
                    .Where(t => t.TopicId.HasValue)
                    .Take(5)
                    .Select(t => new ApiClient.TopicRecommendation(
                        t.TopicId!.Value,
                        t.TopicName,
                        null,
                        t.AttemptsTotal,
                        t.CorrectTotal,
                        t.AccuracyPercentage,
                        t.LastAttemptAt,
                        null,
                        null
                    )).ToList();
                
                if (validTopics.Count > 0)
                {
                    
                    await CreateMixedTestFromTopicsAsync(subjectId, validTopics, "problematic topics", tasksPerTopic: 5, difficulty: 3);
                }
                else
                {
                    Console.WriteLine("No problematic topics found.");
                    Console.WriteLine("Press Enter to continue...");
                    Console.ReadLine();
                }
                break;
            case "0":
                break;
            default:
                Console.WriteLine("Unknown option.");
                break;
        }
    }

    private async Task ViewRecommendationsAsync()
    {
        
        var subjectsResult = await _apiClient.GetAllSubjectsAsync(_cts.Token);
        if (!subjectsResult.Success)
        {
            Console.WriteLine($"Error: {FormatError(subjectsResult.Error)}");
            return;
        }

        var subjects = subjectsResult.Value!;
        if (subjects.Count == 0)
        {
            Console.WriteLine("No subjects available.");
            return;
        }

        Console.WriteLine("=== Select Subject ===");
        for (int i = 0; i < subjects.Count; i++)
        {
            Console.WriteLine($"{i + 1}) {subjects.ElementAt(i).SubjectName} (ID: {subjects.ElementAt(i).Id})");
        }
        Console.Write("Enter subject number: ");
        var subjectInput = Console.ReadLine();
        Console.WriteLine();

        if (!int.TryParse(subjectInput, out var subjectIndex) || subjectIndex < 1 || subjectIndex > subjects.Count)
        {
            Console.WriteLine("Invalid subject number.");
            return;
        }

        var selectedSubject = subjects.ElementAt(subjectIndex - 1);
        await ShowRecommendationsForSubjectAsync(selectedSubject.Id, selectedSubject.SubjectName);
    }

    private async Task ShowRecommendationsForSubjectAsync(long subjectId, string subjectName)
    {
        int criticalThreshold = 80; 

        while (true)
        {
            var result = await _apiClient.GetRecommendationsAsync(subjectId, criticalThreshold, _cts.Token);
            if (!result.Success)
            {
                Console.WriteLine($"Error: {FormatError(result.Error)}");
                return;
            }

            var recommendations = result.Value!;
            criticalThreshold = recommendations.CriticalThreshold;

            Console.WriteLine($"=== Recommendations for {subjectName} ===");
            Console.WriteLine($"Critical threshold: {criticalThreshold}%");
            Console.WriteLine();

            
            if (recommendations.CriticalTopics.Count > 0)
            {
                Console.WriteLine($"1. Topics with accuracy below {criticalThreshold}% ({recommendations.CriticalTopics.Count}):");
                for (int i = 0; i < recommendations.CriticalTopics.Count; i++)
                {
                    var topic = recommendations.CriticalTopics.ElementAt(i);
                    Console.WriteLine($"   {i + 1}) {topic.TopicName}");
                    if (topic.AccuracyPercentage.HasValue)
                    {
                        Console.WriteLine($"      Accuracy: {topic.AccuracyPercentage.Value:F1}%");
                        Console.WriteLine($"      Completed tasks: {topic.AttemptsTotal ?? 0}");
                    }
                    else
                    {
                        Console.WriteLine($"      Not yet attempted");
                    }
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine($"1. Topics with accuracy below {criticalThreshold}%: None");
                Console.WriteLine();
            }

            
            if (recommendations.LeitnerTopics.Count > 0)
            {
                Console.WriteLine($"2. Topics to review (Leitner system) ({recommendations.LeitnerTopics.Count}):");
                for (int i = 0; i < recommendations.LeitnerTopics.Count; i++)
                {
                    var topic = recommendations.LeitnerTopics.ElementAt(i);
                    Console.WriteLine($"   {i + 1}) {topic.TopicName}");
                    if (topic.SuccessfulRepetitions.HasValue)
                    {
                        Console.WriteLine($"      Successful repetitions: {topic.SuccessfulRepetitions}");
                    }
                    if (topic.RepetitionIntervalDays.HasValue)
                    {
                        Console.WriteLine($"      Review interval: {topic.RepetitionIntervalDays} days");
                    }
                    if (topic.LastAttemptAt.HasValue)
                    {
                        var daysAgo = (DateTimeOffset.UtcNow - topic.LastAttemptAt.Value).Days;
                        Console.WriteLine($"      Last attempt: {daysAgo} days ago");
                    }
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("2. Topics to review (Leitner system): None");
                Console.WriteLine();
            }

            
            if (recommendations.UnstudiedTopics.Count > 0)
            {
                Console.WriteLine($"3. Unstudied topics ({recommendations.UnstudiedTopics.Count}):");
                for (int i = 0; i < recommendations.UnstudiedTopics.Count; i++)
                {
                    var topic = recommendations.UnstudiedTopics.ElementAt(i);
                    Console.WriteLine($"   {i + 1}) {topic.TopicName}");
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("3. Unstudied topics: None");
                Console.WriteLine();
            }

            
            Console.WriteLine("Options:");
            Console.WriteLine("1) Create test for critical topics");
            Console.WriteLine("2) Create test for topics to review");
            Console.WriteLine("3) Create test for unstudied topic");
            Console.WriteLine("4) Change critical threshold");
            Console.WriteLine("0) Back to menu");
            Console.Write("Choose option: ");
            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    if (recommendations.CriticalTopics.Count > 0)
                    {
                        
                        var selectedTopics = recommendations.CriticalTopics.Take(5).ToList();
                        await CreateMixedTestFromTopicsAsync(subjectId, selectedTopics, "critical topics", tasksPerTopic: 5, difficulty: 3);
                    }
                    else
                    {
                        Console.WriteLine("No critical topics to create test for.");
                        Console.WriteLine("Press Enter to continue...");
                        Console.ReadLine();
                    }
                    break;
                case "2":
                    if (recommendations.LeitnerTopics.Count > 0)
                    {
                        
                        var selectedTopics = recommendations.LeitnerTopics.Take(5).ToList();
                        await CreateMixedTestFromTopicsAsync(subjectId, selectedTopics, "topics to review", tasksPerTopic: 5, difficulty: 3);
                    }
                    else
                    {
                        Console.WriteLine("No topics to review.");
                        Console.WriteLine("Press Enter to continue...");
                        Console.ReadLine();
                    }
                    break;
                case "3":
                    if (recommendations.UnstudiedTopics.Count > 0)
                    {
                        Console.WriteLine("Select unstudied topic:");
                        for (int i = 0; i < recommendations.UnstudiedTopics.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}) {recommendations.UnstudiedTopics.ElementAt(i).TopicName}");
                        }
                        Console.WriteLine("0) Back");
                        Console.Write("Enter topic number: ");
                        var topicInput = Console.ReadLine();
                        Console.WriteLine();

                        if (topicInput == "0")
                        {
                            
                            continue;
                        }

                        if (int.TryParse(topicInput, out var topicIndex) && topicIndex >= 1 && topicIndex <= recommendations.UnstudiedTopics.Count)
                        {
                            var selectedTopic = recommendations.UnstudiedTopics.ElementAt(topicIndex - 1);
                            var topics = new List<ApiClient.TopicRecommendation> { selectedTopic };
                            
                            await CreateMixedTestFromTopicsAsync(subjectId, topics, $"topic '{selectedTopic.TopicName}'", tasksPerTopic: 25, difficulty: 2);
                        }
                        else
                        {
                            Console.WriteLine("Invalid topic number.");
                            Console.WriteLine("Press Enter to continue...");
                            Console.ReadLine();
                        }
                    }
                    else
                    {
                        Console.WriteLine("No unstudied topics.");
                        Console.WriteLine("Press Enter to continue...");
                        Console.ReadLine();
                    }
                    break;
                case "4":
                    Console.Write($"Enter new critical threshold (current: {criticalThreshold}%): ");
                    var thresholdInput = Console.ReadLine();
                    Console.WriteLine();

                    if (int.TryParse(thresholdInput, out var newThreshold) && newThreshold >= 0 && newThreshold <= 100)
                    {
                        var updateResult = await _apiClient.UpdateCriticalThresholdAsync(newThreshold, _cts.Token);
                        if (updateResult.Success)
                        {
                            criticalThreshold = newThreshold;
                            Console.WriteLine($"Critical threshold updated to {newThreshold}%");
                        }
                        else
                        {
                            Console.WriteLine($"Error: {FormatError(updateResult.Error)}");
                        }
                        Console.WriteLine("Press Enter to continue...");
                        Console.ReadLine();
                    }
                    else
                    {
                        Console.WriteLine("Invalid threshold. Must be between 0 and 100.");
                        Console.WriteLine("Press Enter to continue...");
                        Console.ReadLine();
                    }
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Unknown option.");
                    Console.WriteLine("Press Enter to continue...");
                    Console.ReadLine();
                    break;
            }
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
                            
                            Console.WriteLine("Completing attempt...");
                            var (_, finalCorrect) = await LoadAttemptSummaryAsync(attemptId, orderedTasks.Length);
                            var completeRequest = new CompleteAttemptRequest((decimal?)finalCorrect, null, null);
                            var completeResult = await _apiClient.CompleteAttemptAsync(attemptId, completeRequest, _cts.Token);
                            Console.WriteLine(completeResult.Success
                                ? $"Attempt completed. Score: {finalCorrect}/{orderedTasks.Length}"
                                : $"Failed to complete attempt: {FormatError(completeResult.Error)}");
                            return; 
                            
                        case "2":
                            
                            Console.WriteLine("Aborting attempt...");
                            var abortResult = await _apiClient.AbortAttemptAsync(attemptId, _cts.Token);
                            Console.WriteLine(abortResult.Success
                                ? "Attempt aborted. You can start a new attempt later."
                                : $"Failed to abort attempt: {FormatError(abortResult.Error)}");
                            return; 
                            
                        case "3":
                            
                            Console.WriteLine("Returning to test.");
                            Console.WriteLine();
                            continue; 
                            
                        default:
                            Console.WriteLine("Unknown option. Returning to test.");
                            Console.WriteLine();
                            continue; 
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
        
        
        foreach (var answer in answers)
        {
            
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
                
                _answerCache.Remove(answer.TaskId);
            }
        }

        
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
        
        
        if (!trimmed.StartsWith("\"", StringComparison.Ordinal) && 
            !trimmed.StartsWith("{", StringComparison.Ordinal) && 
            !trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            return trimmed;
        }

        try
        {
            
            using var document = JsonDocument.Parse(trimmed);
            
            
            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                return document.RootElement.GetString();
            }
            
            
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("value", out var valueProperty))
            {
                if (valueProperty.ValueKind == JsonValueKind.String)
                {
                    return valueProperty.GetString();
                }
                return valueProperty.GetRawText().Trim('"');
            }
            
            
            return document.RootElement.GetRawText();
        }
        catch (JsonException)
        {
            
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

    private async Task JoinTeacherByCodeAsync()
    {
        Console.WriteLine("=== Join Teacher by Code ===");
        Console.Write("Enter invitation code: ");
        var code = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(code))
        {
            Console.WriteLine("Invitation code cannot be empty.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("Joining teacher...");
        var result = await _apiClient.JoinTeacherByCodeAsync(code, _cts.Token);

        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine();
            return;
        }

        var teacher = result.Value!;
        Console.WriteLine();
        Console.WriteLine("Successfully joined teacher!");
        Console.WriteLine($"Teacher: {teacher.UserName}");
        Console.WriteLine($"Email: {teacher.Email}");
        if (teacher.EstablishedAt.HasValue)
        {
            Console.WriteLine($"Connected: {teacher.EstablishedAt.Value:yyyy-MM-dd HH:mm}");
        }
        Console.WriteLine();
    }

    private async Task ShowMyTeachersAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            Console.WriteLine("=== My Teachers ===");
            Console.WriteLine("1) Join teacher by code");
            Console.WriteLine("2) View my teachers");
            Console.WriteLine("3) Remove teacher");
            Console.WriteLine("0) Back");
            Console.Write("Select option: ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await JoinTeacherByCodeAsync();
                    break;
                case "2":
                    await ViewMyTeachersListAsync();
                    break;
                case "3":
                    await RemoveTeacherAsync();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }
        }
    }

    private async Task ViewMyTeachersListAsync()
    {
        Console.WriteLine("=== My Teachers List ===");
        
        var result = await _apiClient.GetMyTeachersAsync(_cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine();
            return;
        }

        var teachers = result.Value!;
        if (teachers.Count == 0)
        {
            Console.WriteLine("You are not connected to any teachers yet.");
            Console.WriteLine("Use option 1 to join a teacher by invitation code.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine();
        for (int i = 0; i < teachers.Count; i++)
        {
            var teacher = teachers.ElementAt(i);
            Console.WriteLine($"{i + 1}) {teacher.UserName} (ID: {teacher.Id})");
            Console.WriteLine($"   Email: {teacher.Email}");
            if (teacher.EstablishedAt.HasValue)
            {
                Console.WriteLine($"   Connected: {teacher.EstablishedAt.Value:yyyy-MM-dd HH:mm}");
            }
            Console.WriteLine();
        }
    }

    private async Task RemoveTeacherAsync()
    {
        Console.WriteLine("=== Remove Teacher ===");
        
        var result = await _apiClient.GetMyTeachersAsync(_cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine();
            return;
        }

        var teachers = result.Value!;
        if (teachers.Count == 0)
        {
            Console.WriteLine("You are not connected to any teachers yet.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("Your teachers:");
        for (int i = 0; i < teachers.Count; i++)
        {
            var teacher = teachers.ElementAt(i);
            Console.WriteLine($"{i + 1}) {teacher.UserName} (ID: {teacher.Id})");
        }
        Console.WriteLine("0) Cancel");
        Console.Write("Enter teacher ID to remove: ");

        var input = Console.ReadLine()?.Trim();
        Console.WriteLine();

        if (input == "0" || !long.TryParse(input, out var teacherId))
        {
            return;
        }

        
        var teacherToRemove = teachers.FirstOrDefault(t => t.Id == teacherId);
        if (teacherToRemove == null)
        {
            Console.WriteLine($"Teacher with ID {teacherId} not found in your teachers list.");
            Console.WriteLine();
            return;
        }

        Console.Write($"Are you sure you want to remove {teacherToRemove.UserName}? (y/n): ");
        var confirm = Console.ReadLine()?.Trim().ToLower();
        
        if (confirm != "y")
        {
            Console.WriteLine("Removal cancelled.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine($"Removing connection to {teacherToRemove.UserName}...");
        
        var removeResult = await _apiClient.RemoveTeacherAsync(teacherId, _cts.Token);
        if (!removeResult.Success)
        {
            Console.WriteLine($"Error: {FormatError(removeResult.Error)}");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("Teacher removed successfully.");
        Console.WriteLine();
    }

    private async Task ShowMyMixedTestsAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            Console.WriteLine("=== My Mixed Tests ===");
            Console.WriteLine("1) Create mixed test");
            Console.WriteLine("2) View my mixed tests");
            Console.WriteLine("3) Delete mixed test");
            Console.WriteLine("0) Back");
            Console.Write("Select option: ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await CreateMixedTestAsync();
                    break;
                case "2":
                    await ViewMyMixedTestsListAsync();
                    break;
                case "3":
                    await DeleteMixedTestAsync();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }
        }
    }

    private async Task CreateMixedTestAsync()
    {
        Console.WriteLine("=== Create Mixed Test ===");
        
        
        var subjectsResult = await _apiClient.GetAllSubjectsAsync(_cts.Token);
        if (!subjectsResult.Success || subjectsResult.Value == null || !subjectsResult.Value.Any())
        {
            Console.WriteLine("Error: Could not load subjects.");
            return;
        }

        var subjects = subjectsResult.Value.ToList();
        Console.WriteLine("Select subject:");
        for (int i = 0; i < subjects.Count; i++)
        {
            Console.WriteLine($"{i + 1}) {subjects[i].SubjectName}");
        }
        Console.Write("Enter subject number: ");
        if (!int.TryParse(Console.ReadLine(), out var subjectIndex) || subjectIndex < 1 || subjectIndex > subjects.Count)
        {
            Console.WriteLine("Invalid subject number.");
            return;
        }

        var subjectId = subjects[subjectIndex - 1].Id;
        Console.WriteLine();

        
        Console.Write("Enter test title (or press Enter for auto-generated): ");
        var titleInput = Console.ReadLine()?.Trim();
        string? title = string.IsNullOrWhiteSpace(titleInput) ? null : titleInput;
        Console.WriteLine();

        
        Console.Write("Enter time limit in minutes (or press Enter for unlimited): ");
        var timeLimitInput = Console.ReadLine()?.Trim();
        int? timeLimitSec = null;
        if (!string.IsNullOrWhiteSpace(timeLimitInput) && int.TryParse(timeLimitInput, out var minutes) && minutes > 0)
        {
            timeLimitSec = minutes * 60; 
        }
        Console.WriteLine();

        
        Console.WriteLine("Select topics for the test:");
        Console.WriteLine("1) Use default (top 3 error topics, 10 tasks each, medium difficulty)");
        Console.WriteLine("2) Custom selection");
        Console.Write("Enter option: ");
        var selectionChoice = Console.ReadLine();
        Console.WriteLine();

        var topics = new List<ApiClient.TopicSelection>();

        if (selectionChoice == "1")
        {
            
            topics = new List<ApiClient.TopicSelection>();
        }
        else if (selectionChoice == "2")
        {
            
            var statsResult = await _apiClient.GetSubjectStatisticsAsync(subjectId, _cts.Token);
            if (!statsResult.Success || statsResult.Value == null)
            {
                Console.WriteLine("Error: Could not load statistics.");
                return;
            }

            var stats = statsResult.Value;
            var allTopics = new List<(long? TopicId, string TopicName)>();
            
            
            foreach (var topic in stats.Top3ErrorTopics)
            {
                if (topic.TopicId.HasValue)
                {
                    allTopics.Add((topic.TopicId.Value, topic.TopicName));
                }
            }
            foreach (var topic in stats.OtherTopics)
            {
                if (topic.TopicId.HasValue && !allTopics.Any(t => t.TopicId == topic.TopicId))
                {
                    allTopics.Add((topic.TopicId.Value, topic.TopicName));
                }
            }
            foreach (var topic in stats.UnattemptedTopics)
            {
                if (topic.TopicId.HasValue && !allTopics.Any(t => t.TopicId == topic.TopicId))
                {
                    allTopics.Add((topic.TopicId.Value, topic.TopicName));
                }
            }

            if (allTopics.Count == 0)
            {
                Console.WriteLine("No topics available.");
                return;
            }

            Console.WriteLine("Available topics:");
            for (int i = 0; i < allTopics.Count; i++)
            {
                Console.WriteLine($"{i + 1}) {allTopics[i].TopicName} (ID: {allTopics[i].TopicId})");
            }
            Console.WriteLine("0) Finish selection");
            Console.WriteLine();

            while (true)
            {
                Console.Write("Enter topic number (or 0 to finish): ");
                var topicInput = Console.ReadLine();
                if (topicInput == "0")
                {
                    break;
                }

                if (!int.TryParse(topicInput, out var topicIndex) || topicIndex < 1 || topicIndex > allTopics.Count)
                {
                    Console.WriteLine("Invalid topic number.");
                    continue;
                }

                var selectedTopic = allTopics[topicIndex - 1];
                Console.Write($"Enter number of tasks for '{selectedTopic.TopicName}' (default 10): ");
                var taskCountInput = Console.ReadLine()?.Trim();
                var taskCount = 10;
                if (!string.IsNullOrWhiteSpace(taskCountInput) && int.TryParse(taskCountInput, out var count))
                {
                    taskCount = count;
                }

                Console.WriteLine("Select difficulty:");
                Console.WriteLine("1) Very easy");
                Console.WriteLine("2) Easy");
                Console.WriteLine("3) Medium");
                Console.WriteLine("4) Hard");
                Console.WriteLine("5) Very hard");
                Console.Write("Enter difficulty (1-5, default 3): ");
                var difficultyInput = Console.ReadLine()?.Trim();
                var difficulty = (short)3;
                if (!string.IsNullOrWhiteSpace(difficultyInput) && short.TryParse(difficultyInput, out var diff) && diff >= 1 && diff <= 5)
                {
                    difficulty = diff;
                }

                topics.Add(new ApiClient.TopicSelection(selectedTopic.TopicId!.Value, taskCount, difficulty));
                Console.WriteLine($"Added: {selectedTopic.TopicName} - {taskCount} tasks, difficulty {difficulty}");
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine("Invalid option.");
            return;
        }

        
        Console.WriteLine("Creating mixed test...");
        var request = new ApiClient.GenerateMixedTestRequest(
            subjectId,
            timeLimitSec,
            title,
            topics);

        var result = await _apiClient.GenerateMixedTestAsync(request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        var test = result.Value!;
        Console.WriteLine($"Mixed test created successfully!");
        Console.WriteLine($"Test ID: {test.Id}");
        Console.WriteLine($"Title: {test.Title}");
        Console.WriteLine($"Tasks: {test.Tasks.Count}");
        Console.WriteLine();
    }

    private async Task ViewMyMixedTestsListAsync()
    {
        Console.WriteLine("=== My Mixed Tests List ===");
        
        
        var subjectsResult = await _apiClient.GetAllSubjectsAsync(_cts.Token);
        if (!subjectsResult.Success || subjectsResult.Value == null || !subjectsResult.Value.Any())
        {
            Console.WriteLine("Error: Could not load subjects.");
            return;
        }

        var subjects = subjectsResult.Value.ToList();
        Console.WriteLine("Select subject:");
        for (int i = 0; i < subjects.Count; i++)
        {
            Console.WriteLine($"{i + 1}) {subjects[i].SubjectName}");
        }
        Console.Write("Enter subject number: ");
        if (!int.TryParse(Console.ReadLine(), out var subjectIndex) || subjectIndex < 1 || subjectIndex > subjects.Count)
        {
            Console.WriteLine("Invalid subject number.");
            return;
        }

        var subjectId = subjects[subjectIndex - 1].Id;
        Console.WriteLine();

        var result = await _apiClient.GetMyMixedTestsAsync(subjectId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine();
            return;
        }

        var tests = result.Value!;
        if (tests.Count == 0)
        {
            Console.WriteLine("You have no mixed tests yet.");
            Console.WriteLine("Use option 1 to create a mixed test.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine($"Found {tests.Count} mixed test(s):");
        foreach (var test in tests)
        {
            var timeLimitInfo = test.TimeLimitSec.HasValue ? $"{test.TimeLimitSec.Value / 60} minute(s)" : "Unlimited";
            Console.WriteLine($"- #{test.Id}: {test.Title}");
            Console.WriteLine($"  Time limit: {timeLimitInfo} | Mode: {test.Mode ?? "N/A"}");
        }
        Console.WriteLine();
    }

    private async Task DeleteMixedTestAsync()
    {
        Console.WriteLine("=== Delete Mixed Test ===");
        Console.Write("Enter test ID to delete: ");
        if (!long.TryParse(Console.ReadLine(), out var testId))
        {
            Console.WriteLine("Invalid test ID.");
            return;
        }

        Console.Write($"Are you sure you want to delete test #{testId}? (y/n): ");
        var confirm = Console.ReadLine()?.Trim().ToLower();
        if (confirm != "y")
        {
            Console.WriteLine("Deletion cancelled.");
            return;
        }

        Console.WriteLine("Deleting test...");
        var result = await _apiClient.DeleteMixedTestAsync(testId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            return;
        }

        Console.WriteLine("Test deleted successfully.");
        Console.WriteLine();
    }

    private async Task CreateMixedTestFromTopicsAsync(
        long subjectId, 
        IReadOnlyCollection<ApiClient.TopicRecommendation> topics, 
        string description,
        int tasksPerTopic = 10,
        short difficulty = 3)
    {
        Console.WriteLine($"=== Create Mixed Test for {description} ===");
        
        
        Console.Write("Enter test title (or press Enter for auto-generated): ");
        var titleInput = Console.ReadLine()?.Trim();
        string? title = string.IsNullOrWhiteSpace(titleInput) ? null : titleInput;
        Console.WriteLine();

        
        Console.Write("Enter time limit in minutes (or press Enter for unlimited): ");
        var timeLimitInput = Console.ReadLine()?.Trim();
        int? timeLimitSec = null;
        if (!string.IsNullOrWhiteSpace(timeLimitInput) && int.TryParse(timeLimitInput, out var minutes) && minutes > 0)
        {
            timeLimitSec = minutes * 60; 
        }
        Console.WriteLine();

        
        var topicSelections = topics.Select(t => new ApiClient.TopicSelection(
            t.TopicId,
            tasksPerTopic,
            difficulty
        )).ToList();

        
        Console.WriteLine("Creating mixed test...");
        var request = new ApiClient.GenerateMixedTestRequest(
            subjectId,
            timeLimitSec,
            title,
            topicSelections);

        var result = await _apiClient.GenerateMixedTestAsync(request, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        var test = result.Value!;
        Console.WriteLine($"Mixed test created successfully!");
        Console.WriteLine($"Test ID: {test.Id}");
        Console.WriteLine($"Title: {test.Title}");
        Console.WriteLine($"Tasks: {test.Tasks.Count}");
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task ShowAttemptDetailsAsync(long attemptId)
    {
        Console.WriteLine("=== Attempt Details ===");
        Console.WriteLine("Loading attempt details...");
        Console.WriteLine();

        var result = await _apiClient.GetAttemptDetailsWithTasksAsync(attemptId, _cts.Token);
        if (!result.Success)
        {
            Console.WriteLine($"Error: {FormatError(result.Error)}");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            return;
        }

        var details = result.Value!;
        
        Console.WriteLine($"Attempt #{details.Id}: {details.TestTitle}");
        Console.WriteLine($"Status: {details.Status}");
        Console.WriteLine($"Started: {details.StartedAt:yyyy-MM-dd HH:mm:ss}");
        if (details.FinishedAt.HasValue)
        {
            Console.WriteLine($"Finished: {details.FinishedAt.Value:yyyy-MM-dd HH:mm:ss}");
        }
        if (details.DurationSec.HasValue)
        {
            var minutes = details.DurationSec.Value / 60;
            var seconds = details.DurationSec.Value % 60;
            Console.WriteLine($"Duration: {minutes} minute(s) {seconds} second(s)");
        }
        if (details.RawScore.HasValue)
        {
            Console.WriteLine($"Score: {details.RawScore.Value:F1}");
        }
        Console.WriteLine();

        var tasks = details.Tasks.OrderBy(t => t.Position).ToList();
        Console.WriteLine($"Tasks ({tasks.Count}):");
        Console.WriteLine();

        for (int i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            Console.WriteLine($"--- Task {i + 1}/{tasks.Count} ---");
            Console.WriteLine($"Type: {task.TaskType}, Difficulty: {task.Difficulty}");
            Console.WriteLine($"Statement: {task.Statement}");
            Console.WriteLine();

            
            if (task.GivenAnswer != null)
            {
                var answerStatus = task.IsCorrect == true ? "Correct" : task.IsCorrect == false ? "Incorrect" : "Unknown";
                Console.WriteLine($"Your answer: {task.GivenAnswer} ({answerStatus})");
            }
            else
            {
                Console.WriteLine("Your answer: Not answered");
            }

            
            if (task.CorrectAnswer != null)
            {
                Console.WriteLine($"Correct answer: {task.CorrectAnswer}");
            }

            
            if (!string.IsNullOrWhiteSpace(task.Explanation))
            {
                Console.WriteLine($"Explanation: {task.Explanation}");
            }

            
            if (task.TimeSpentSec.HasValue)
            {
                var taskMinutes = task.TimeSpentSec.Value / 60;
                var taskSeconds = task.TimeSpentSec.Value % 60;
                Console.WriteLine($"Time spent: {taskMinutes} minute(s) {taskSeconds} second(s)");
            }

            if (i < tasks.Count - 1)
            {
                Console.WriteLine();
            }
        }

        Console.WriteLine();
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _apiClient.Dispose();
        _cts.Dispose();
    }
}
