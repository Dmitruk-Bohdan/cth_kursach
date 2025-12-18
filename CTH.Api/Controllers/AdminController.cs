using CTH.Common.Enums;
using CTH.Services.Extensions;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Admin;
using CTH.Services.Models.Dto.Tasks;
using CTH.Services.Models.Dto.Tests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CTH.Api.Controllers;

[Route("admin")]
[Authorize]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    private bool IsAdmin()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role);
        return roleClaim != null &&
               int.TryParse(roleClaim.Value, out var roleInt) &&
               roleInt == (int)RoleTypeEnum.Admin;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.GetAllUsersAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.CreateUserAsync(request, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(nameof(GetUserById), new { userId = result.Result!.Id }, result.Result);
    }

    [HttpGet("users/{userId:long}")]
    public async Task<IActionResult> GetUserById(long userId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        return NotFound();
    }

    [HttpPut("users/{userId:long}")]
    public async Task<IActionResult> UpdateUser(long userId, [FromBody] UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.UpdateUserAsync(userId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("users/{userId:long}/block")]
    public async Task<IActionResult> BlockUser(long userId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.BlockUserAsync(userId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("users/{userId:long}/unblock")]
    public async Task<IActionResult> UnblockUser(long userId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.UnblockUserAsync(userId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("users/{userId:long}")]
    public async Task<IActionResult> DeleteUser(long userId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.DeleteUserAsync(userId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> GetAllSubjects(CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.GetAllSubjectsAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.CreateSubjectAsync(request, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(nameof(GetSubjectById), new { subjectId = result.Result!.Id }, result.Result);
    }

    [HttpGet("subjects/{subjectId:long}")]
    public async Task<IActionResult> GetSubjectById(long subjectId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        return NotFound();
    }

    [HttpPut("subjects/{subjectId:long}")]
    public async Task<IActionResult> UpdateSubject(long subjectId, [FromBody] UpdateSubjectRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.UpdateSubjectAsync(subjectId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("subjects/{subjectId:long}")]
    public async Task<IActionResult> DeleteSubject(long subjectId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.DeleteSubjectAsync(subjectId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("topics")]
    public async Task<IActionResult> GetAllTopics([FromQuery] long? subjectId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.GetAllTopicsAsync(subjectId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("topics")]
    public async Task<IActionResult> CreateTopic([FromBody] CreateTopicRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.CreateTopicAsync(request, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(nameof(GetTopicById), new { topicId = result.Result!.Id }, result.Result);
    }

    [HttpGet("topics/{topicId:long}")]
    public async Task<IActionResult> GetTopicById(long topicId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        return NotFound();
    }

    [HttpPut("topics/{topicId:long}")]
    public async Task<IActionResult> UpdateTopic(long topicId, [FromBody] UpdateTopicRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.UpdateTopicAsync(topicId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("topics/{topicId:long}")]
    public async Task<IActionResult> DeleteTopic(long topicId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.DeleteTopicAsync(topicId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> GetAllTasks([FromQuery] TaskFilterDto? filter, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.GetAllTasksAsync(filter, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.CreateTaskAsync(request, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(nameof(GetTaskById), new { taskId = result.Result!.Id }, result.Result);
    }

    [HttpGet("tasks/{taskId:long}")]
    public async Task<IActionResult> GetTaskById(long taskId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        return NotFound();
    }

    [HttpPut("tasks/{taskId:long}")]
    public async Task<IActionResult> UpdateTask(long taskId, [FromBody] UpdateTaskRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.UpdateTaskAsync(taskId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("tasks/{taskId:long}/activate")]
    public async Task<IActionResult> ActivateTask(long taskId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.ActivateTaskAsync(taskId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("tasks/{taskId:long}/deactivate")]
    public async Task<IActionResult> DeactivateTask(long taskId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.DeactivateTaskAsync(taskId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("tasks/{taskId:long}")]
    public async Task<IActionResult> DeleteTask(long taskId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.DeleteTaskAsync(taskId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("tests")]
    public async Task<IActionResult> GetAllTests([FromQuery] TestFilterDto? filter, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.GetAllTestsAsync(filter, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("tests")]
    public async Task<IActionResult> CreateTest([FromBody] CreateTestRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.CreateTestAsync(request, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(nameof(GetTestById), new { testId = result.Result!.Id }, result.Result);
    }

    [HttpGet("tests/{testId:long}")]
    public async Task<IActionResult> GetTestById(long testId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        return NotFound();
    }

    [HttpPut("tests/{testId:long}")]
    public async Task<IActionResult> UpdateTest(long testId, [FromBody] UpdateTestRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.UpdateTestAsync(testId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("tests/{testId:long}")]
    public async Task<IActionResult> DeleteTest(long testId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.DeleteTestAsync(testId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("invitation-codes")]
    public async Task<IActionResult> GetAllInvitationCodes([FromQuery] long? teacherId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.GetAllInvitationCodesAsync(teacherId, status, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("invitation-codes")]
    public async Task<IActionResult> CreateInvitationCode([FromBody] CreateInvitationCodeRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.CreateInvitationCodeAsync(request, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(nameof(GetInvitationCodeById), new { invitationCodeId = result.Result!.Id }, result.Result);
    }

    [HttpGet("invitation-codes/{invitationCodeId:long}")]
    public async Task<IActionResult> GetInvitationCodeById(long invitationCodeId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        return NotFound();
    }

    [HttpPut("invitation-codes/{invitationCodeId:long}")]
    public async Task<IActionResult> UpdateInvitationCode(long invitationCodeId, [FromBody] UpdateInvitationCodeRequestDto request, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.UpdateInvitationCodeAsync(invitationCodeId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("invitation-codes/{invitationCodeId:long}")]
    public async Task<IActionResult> DeleteInvitationCode(long invitationCodeId, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var result = await _adminService.DeleteInvitationCodeAsync(invitationCodeId, cancellationToken);
        return result.ToActionResult();
    }
}

