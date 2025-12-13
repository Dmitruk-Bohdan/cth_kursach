using CTH.Database.Models;
using CTH.Services.Extensions;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Attempts;
using CTH.Services.Models.Dto.Tests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CTH.Api.Controllers;

[Route("student/tests")]
[Authorize]
[ApiController]
public class StudentTestsController : ControllerBase
{
    private readonly IStudentTestService _studentTestService;
    private readonly IStudentAttemptService _studentAttemptService;

    public StudentTestsController(
        IStudentTestService studentTestService,
        IStudentAttemptService studentAttemptService)
    {
        _studentTestService = studentTestService;
        _studentAttemptService = studentAttemptService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPublishedTests(
        [FromQuery] long? subjectId,
        [FromQuery] bool onlyTeachers = false,
        [FromQuery] bool onlyStateArchive = false,
        [FromQuery] bool onlyLimitedAttempts = false,
        [FromQuery] string? title = null,
        [FromQuery] string? mode = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var filter = new TestListFilter
        {
            SubjectId = subjectId,
            OnlyTeachers = onlyTeachers,
            OnlyStateArchive = onlyStateArchive,
            OnlyLimitedAttempts = onlyLimitedAttempts,
            TitlePattern = title,
            Mode = mode
        };

        var tests = await _studentTestService.GetPublishedTestsAsync(userId, filter, cancellationToken);
        return Ok(tests);
    }

    [HttpGet("{testId:long}")]
    public async Task<IActionResult> GetTestDetails(long testId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _studentTestService.GetTestDetailsAsync(testId, userId, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return Ok(result.Result);
    }

    [HttpPost("{testId:long}/attempts")]
    public async Task<IActionResult> StartAttempt(long testId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _studentAttemptService.StartAttemptAsync(userId, testId, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return Ok(result.Result);
    }

    [HttpPost("mixed/generate")]
    public async Task<IActionResult> GenerateMixedTest(
        [FromBody] GenerateMixedTestRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _studentTestService.GenerateMixedTestAsync(userId, request, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return Ok(result.Result);
    }

    [HttpGet("mixed")]
    public async Task<IActionResult> GetMyMixedTests(
        [FromQuery] long subjectId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _studentTestService.GetMyMixedTestsAsync(userId, subjectId, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return Ok(result.Result);
    }

    [HttpDelete("mixed/{testId:long}")]
    public async Task<IActionResult> DeleteMixedTest(
        long testId,
        CancellationToken cancellationToken)
    {
        var result = await _studentTestService.DeleteMixedTestAsync(GetCurrentUserId(), testId, cancellationToken);
        return result.ToActionResult();
    }

    private long GetCurrentUserId()
    {
        var claim = User.FindFirst("Id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !long.TryParse(claim.Value, out var userId))
        {
            throw new InvalidOperationException("Cannot resolve current user id.");
        }

        return userId;
    }
}
