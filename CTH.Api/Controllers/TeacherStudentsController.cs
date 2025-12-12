using CTH.Common.Enums;
using CTH.Services.Extensions;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Invitations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CTH.Api.Controllers;

[Route("teacher/students")]
[Authorize]
[ApiController]
public class TeacherStudentsController : ControllerBase
{
    private readonly IInvitationCodeService _invitationCodeService;

    public TeacherStudentsController(IInvitationCodeService invitationCodeService)
    {
        _invitationCodeService = invitationCodeService;
    }

    [HttpPost("invitations")]
    public async Task<IActionResult> CreateInvitationCode([FromBody] CreateInvitationCodeRequestDto request, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _invitationCodeService.CreateInvitationCodeAsync(userId, request, cancellationToken);
        if (!result.IsSuccessful)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(nameof(GetInvitationCodes), result.Result);
    }

    [HttpGet("invitations")]
    public async Task<IActionResult> GetInvitationCodes(CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _invitationCodeService.GetInvitationCodesByTeacherAsync(userId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("invitations/{invitationCodeId:long}/revoke")]
    public async Task<IActionResult> RevokeInvitationCode(long invitationCodeId, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _invitationCodeService.RevokeInvitationCodeAsync(userId, invitationCodeId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("invitations/{invitationCodeId:long}")]
    public async Task<IActionResult> DeleteInvitationCode(long invitationCodeId, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _invitationCodeService.DeleteInvitationCodeAsync(userId, invitationCodeId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet]
    public async Task<IActionResult> GetMyStudents(CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _invitationCodeService.GetMyStudentsAsync(userId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{studentId:long}")]
    public async Task<IActionResult> RemoveStudent(long studentId, CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetCurrentUser();
        if (!isAdmin && !IsTeacher())
        {
            return Forbid();
        }

        var result = await _invitationCodeService.RemoveStudentAsync(userId, studentId, cancellationToken);
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

