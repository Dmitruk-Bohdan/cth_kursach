using CTH.Common.Enums;
using CTH.Services.Extensions;
using CTH.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CTH.Api.Controllers;

[Route("teacher/tests/{testId:long}/access")]
[Authorize]
[ApiController]
public class TestAccessController : ControllerBase
{
    private readonly ITestAccessService _testAccessService;

    public TestAccessController(ITestAccessService testAccessService)
    {
        _testAccessService = testAccessService;
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetStudentsByTest(long testId, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _testAccessService.GetStudentsByTestAsync(testId, userId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("students/{studentId:long}")]
    public async Task<IActionResult> AddStudentAccess(long testId, long studentId, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _testAccessService.AddStudentAccessAsync(testId, studentId, userId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("students/{studentId:long}")]
    public async Task<IActionResult> RemoveStudentAccess(long testId, long studentId, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _testAccessService.RemoveStudentAccessAsync(testId, studentId, userId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("students")]
    public async Task<IActionResult> SetStudentAccessList(long testId, [FromBody] SetStudentAccessRequest request, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _testAccessService.SetStudentAccessListAsync(testId, request.StudentIds, userId, cancellationToken);
        return result.ToActionResult();
    }

    private (long userId, bool isAdmin) GetCurrentUser()
    {
        var claim = User.FindFirst("Id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !long.TryParse(claim.Value, out var userId))
        {
            throw new InvalidOperationException("Cannot resolve current user id.");
        }

        var roleClaim = User.FindFirst(ClaimTypes.Role);
        var isAdmin = roleClaim != null && int.TryParse(roleClaim.Value, out var roleInt) &&
                      roleInt == (int)RoleTypeEnum.Admin;

        return (userId, isAdmin);
    }

    private bool IsTeacher()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role);
        return roleClaim != null &&
               int.TryParse(roleClaim.Value, out var roleInt) &&
               roleInt == (int)RoleTypeEnum.Teacher;
    }

    public sealed class SetStudentAccessRequest
    {
        public IReadOnlyCollection<long> StudentIds { get; set; } = Array.Empty<long>();
    }
}

