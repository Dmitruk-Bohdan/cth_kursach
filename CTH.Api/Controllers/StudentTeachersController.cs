using CTH.Services.Extensions;
using CTH.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CTH.Api.Controllers;

[Route("student/teachers")]
[Authorize]
[ApiController]
public class StudentTeachersController : ControllerBase
{
    private readonly IStudentTeacherService _studentTeacherService;

    public StudentTeachersController(IStudentTeacherService studentTeacherService)
    {
        _studentTeacherService = studentTeacherService;
    }

    [HttpPost("join")]
    public async Task<IActionResult> JoinTeacherByCode([FromBody] JoinTeacherRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.InvitationCode))
        {
            return BadRequest("Invitation code is required");
        }

        var userId = GetCurrentUserId();
        var result = await _studentTeacherService.JoinTeacherByCodeAsync(userId, request.InvitationCode, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(nameof(GetMyTeachers), result.Result);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTeachers(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _studentTeacherService.GetMyTeachersAsync(userId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{teacherId:long}")]
    public async Task<IActionResult> RemoveTeacher(long teacherId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _studentTeacherService.RemoveTeacherAsync(userId, teacherId, cancellationToken);
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

    public sealed class JoinTeacherRequest
    {
        public string InvitationCode { get; set; } = string.Empty;
    }
}

