using CTH.Services.Extensions;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Attempts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CTH.Api.Controllers;

[Route("student/attempts")]
[Authorize]
[ApiController]
public class StudentAttemptsController : ControllerBase
{
    private readonly IStudentAttemptService _studentAttemptService;

    public StudentAttemptsController(IStudentAttemptService studentAttemptService)
    {
        _studentAttemptService = studentAttemptService;
    }

    [HttpGet("{attemptId:long}")]
    public async Task<IActionResult> GetAttempt(long attemptId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _studentAttemptService.GetAttemptAsync(userId, attemptId, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return Ok(result.Result);
    }

    [HttpPost("{attemptId:long}/answers")]
    public async Task<IActionResult> SubmitAnswer(long attemptId, [FromBody] SubmitAnswerRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _studentAttemptService.SubmitAnswerAsync(userId, attemptId, request, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return NoContent();
    }

    [HttpPost("{attemptId:long}/complete")]
    public async Task<IActionResult> CompleteAttempt(long attemptId, [FromBody] CompleteAttemptRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _studentAttemptService.CompleteAttemptAsync(userId, attemptId, request, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return NoContent();
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
