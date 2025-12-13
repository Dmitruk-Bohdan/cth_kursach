using CTH.Common.Enums;
using CTH.Services.Extensions;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Tests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CTH.Api.Controllers;

[Route("teacher/tests")]
[Authorize]
[ApiController]
public class TeacherTestsController : ControllerBase
{
    private readonly ITeacherTestService _teacherTestService;
    private readonly IStudentTestService _studentTestService;

    public TeacherTestsController(
        ITeacherTestService teacherTestService,
        IStudentTestService studentTestService)
    {
        _teacherTestService = teacherTestService;
        _studentTestService = studentTestService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTest([FromBody] CreateTestRequestDto request, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _teacherTestService.CreateTestAsync(userId, isAdmin, request, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(nameof(GetTestDetails), new { testId = result.Result!.Id }, result.Result);
    }

    [HttpPut("{testId:long}")]
    public async Task<IActionResult> UpdateTest(long testId, [FromBody] UpdateTestRequestDto request, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _teacherTestService.UpdateTestAsync(userId, isAdmin, testId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{testId:long}")]
    public async Task<IActionResult> DeleteTest(long testId, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _teacherTestService.DeleteTestAsync(userId, isAdmin, testId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTests([FromQuery] long subjectId, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _teacherTestService.GetMyTestsAsync(userId, subjectId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{testId:long}")]
    public async Task<IActionResult> GetTestDetails(long testId, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        var result = await _teacherTestService.GetTestAsync(userId, isAdmin, testId, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return Ok(result.Result);
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasksBySubject([FromQuery] long subjectId, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        if (!IsTeacher())
        {
            return Forbid();
        }

        var result = await _teacherTestService.GetTasksBySubjectAsync(subjectId, search, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask([FromBody] CTH.Services.Models.Dto.Tasks.CreateTaskRequestDto request, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _teacherTestService.CreateTaskAsync(userId, isAdmin, request, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(nameof(GetTasksBySubject), new { subjectId = request.SubjectId }, result.Result);
    }

    [HttpPut("tasks/{taskId:long}")]
    public async Task<IActionResult> UpdateTask(long taskId, [FromBody] CTH.Services.Models.Dto.Tasks.UpdateTaskRequestDto request, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _teacherTestService.UpdateTaskAsync(userId, isAdmin, taskId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopicsBySubject([FromQuery] long subjectId, CancellationToken cancellationToken)
    {
        if (!IsTeacher())
        {
            return Forbid();
        }

        var result = await _teacherTestService.GetTopicsBySubjectAsync(subjectId, cancellationToken);
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
}
