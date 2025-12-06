using CTH.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CTH.Api.Controllers;

[ApiController]
[Route("student/statistics")]
[Authorize]
public class StudentStatisticsController : ControllerBase
{
    private readonly IStudentStatisticsService _statisticsService;

    public StudentStatisticsController(IStudentStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("user_id");
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    [HttpGet("by-subject")]
    public async Task<IActionResult> GetStatisticsBySubject([FromQuery] long? subjectId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _statisticsService.GetStatisticsBySubjectAsync(userId, subjectId, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return Ok(result.Result);
    }

    [HttpGet("by-topic")]
    public async Task<IActionResult> GetStatisticsByTopic([FromQuery] long? subjectId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _statisticsService.GetStatisticsByTopicAsync(userId, subjectId, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return Ok(result.Result);
    }
}

