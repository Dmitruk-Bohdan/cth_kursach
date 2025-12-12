using CTH.Services.Extensions;
using CTH.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CTH.Api.Controllers;

[Route("student/recommendations")]
[Authorize]
[ApiController]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationsService _recommendationsService;

    public RecommendationsController(IRecommendationsService recommendationsService)
    {
        _recommendationsService = recommendationsService;
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

    [HttpGet("subject/{subjectId:long}")]
    public async Task<IActionResult> GetRecommendations(
        long subjectId,
        [FromQuery] int? criticalThreshold,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var threshold = criticalThreshold ?? 80;

        var result = await _recommendationsService.GetRecommendationsAsync(
            userId, 
            subjectId, 
            threshold, 
            cancellationToken);

        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return Ok(result.Result);
    }

    [HttpPut("critical-threshold")]
    public async Task<IActionResult> UpdateCriticalThreshold(
        [FromBody] UpdateCriticalThresholdRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result = await _recommendationsService.UpdateCriticalThresholdAsync(
            userId, 
            request.Threshold, 
            cancellationToken);

        return result.ToActionResult();
    }

    public sealed record UpdateCriticalThresholdRequest(int Threshold);
}

