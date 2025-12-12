using CTH.Services.Extensions;
using CTH.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropTechPeople.Services.Models.ResultApiModels;
using System.Security.Claims;

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
        var claim = User.FindFirst("Id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !long.TryParse(claim.Value, out var userId))
        {
            throw new InvalidOperationException("Cannot resolve current user id.");
        }
        return userId;
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> GetAllSubjects(CancellationToken cancellationToken)
    {
        var result = await _statisticsService.GetAllSubjectsAsync(cancellationToken);
        if (!result.IsSuccessful)
        {
            return ((HttpOperationResult)result).ToActionResult();
        }

        return Ok(result.Result);
    }

    [HttpGet("subject/{subjectId:long}")]
    public async Task<IActionResult> GetSubjectStatistics(long subjectId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _statisticsService.GetSubjectStatisticsAsync(userId, subjectId, cancellationToken);
        if (!result.IsSuccessful)
        {
            return ((HttpOperationResult)result).ToActionResult();
        }

        return Ok(result.Result);
    }
}

