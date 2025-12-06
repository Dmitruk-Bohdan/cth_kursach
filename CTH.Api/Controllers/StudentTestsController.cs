using CTH.Services.Extensions;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Attempts;
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
    public async Task<IActionResult> GetPublishedTests([FromQuery] long? subjectId, CancellationToken cancellationToken)
    {
        var tests = await _studentTestService.GetPublishedTestsAsync(subjectId, cancellationToken);
        return Ok(tests);
    }

    [HttpGet("{testId:long}")]
    public async Task<IActionResult> GetTestDetails(long testId, CancellationToken cancellationToken)
    {
        var result = await _studentTestService.GetTestDetailsAsync(testId, cancellationToken);
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
