using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Attempts;
using CTH.Services.Models.Dto.Invitations;
using CTH.Services.Models.Dto.Statistics;
using CTH.Services.Models.Dto.Students;
using PropTechPeople.Services.Models.ResultApiModels;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CTH.Services.Implementations;

public class InvitationCodeService : IInvitationCodeService
{
    private readonly IInvitationCodeRepository _invitationCodeRepository;
    private readonly ITeacherStudentRepository _teacherStudentRepository;
    private readonly IStudentAttemptService _studentAttemptService;
    private readonly IStudentStatisticsService _studentStatisticsService;
    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; 
    private const int CodeLength = 32; // XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX = 32 символа

    public InvitationCodeService(
        IInvitationCodeRepository invitationCodeRepository,
        ITeacherStudentRepository teacherStudentRepository,
        IStudentAttemptService studentAttemptService,
        IStudentStatisticsService studentStatisticsService)
    {
        _invitationCodeRepository = invitationCodeRepository;
        _teacherStudentRepository = teacherStudentRepository;
        _studentAttemptService = studentAttemptService;
        _studentStatisticsService = studentStatisticsService;
    }

    public async Task<HttpOperationResult<InvitationCodeDto>> CreateInvitationCodeAsync(long teacherId, CreateInvitationCodeRequestDto request, CancellationToken cancellationToken)
    {
        
        string code;
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            code = GenerateInvitationCode();
            var existing = await _invitationCodeRepository.GetByCodeAsync(code, cancellationToken);
            if (existing == null)
            {
                break;
            }
            attempts++;
        } while (attempts < maxAttempts);

        if (attempts >= maxAttempts)
        {
            return new HttpOperationResult<InvitationCodeDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to generate unique invitation code"
            };
        }

        
        code = FormatCode(code);

        var invitationCode = new InvitationCode
        {
            TeacherId = teacherId,
            Code = code,
            MaxUses = request.MaxUses,
            UsedCount = 0,
            ExpiresAt = request.ExpiresAt,
            Status = "active",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var id = await _invitationCodeRepository.CreateAsync(invitationCode, cancellationToken);

        var dto = new InvitationCodeDto
        {
            Id = id,
            TeacherId = invitationCode.TeacherId,
            Code = invitationCode.Code,
            MaxUses = invitationCode.MaxUses,
            UsedCount = invitationCode.UsedCount,
            ExpiresAt = invitationCode.ExpiresAt,
            Status = invitationCode.Status,
            CreatedAt = invitationCode.CreatedAt
        };

        return new HttpOperationResult<InvitationCodeDto>(dto, HttpStatusCode.Created);
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<InvitationCodeDto>>> GetInvitationCodesByTeacherAsync(long teacherId, CancellationToken cancellationToken)
    {
        var codes = await _invitationCodeRepository.GetByTeacherIdAsync(teacherId, cancellationToken);
        
        var dtos = codes.Select(c => new InvitationCodeDto
        {
            Id = c.Id,
            TeacherId = c.TeacherId,
            Code = c.Code,
            MaxUses = c.MaxUses,
            UsedCount = c.UsedCount,
            ExpiresAt = c.ExpiresAt,
            Status = c.Status,
            CreatedAt = c.CreatedAt
        }).ToArray();

        return new HttpOperationResult<IReadOnlyCollection<InvitationCodeDto>>(dtos, HttpStatusCode.OK);
    }

    public async Task<HttpOperationResult> RevokeInvitationCodeAsync(long teacherId, long invitationCodeId, CancellationToken cancellationToken)
    {
        var codes = await _invitationCodeRepository.GetByTeacherIdAsync(teacherId, cancellationToken);
        var code = codes.FirstOrDefault(c => c.Id == invitationCodeId);

        if (code == null)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.NotFound,
                Error = "Invitation code not found"
            };
        }

        if (code.TeacherId != teacherId)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.Forbidden,
                Error = "Not authorized to revoke this invitation code"
            };
        }

        code.Status = "revoked";
        await _invitationCodeRepository.UpdateAsync(code, cancellationToken);

        return new HttpOperationResult(HttpStatusCode.NoContent);
    }

    public async Task<HttpOperationResult> DeleteInvitationCodeAsync(long teacherId, long invitationCodeId, CancellationToken cancellationToken)
    {
        var codes = await _invitationCodeRepository.GetByTeacherIdAsync(teacherId, cancellationToken);
        var code = codes.FirstOrDefault(c => c.Id == invitationCodeId);

        if (code == null)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.NotFound,
                Error = "Invitation code not found"
            };
        }

        if (code.TeacherId != teacherId)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.Forbidden,
                Error = "Not authorized to delete this invitation code"
            };
        }

        await _invitationCodeRepository.DeleteAsync(invitationCodeId, cancellationToken);

        return new HttpOperationResult(HttpStatusCode.NoContent);
    }

    private static string GenerateInvitationCode()
    {
        var random = RandomNumberGenerator.GetBytes(CodeLength);
        var code = new StringBuilder(CodeLength);

        for (int i = 0; i < CodeLength; i++)
        {
            code.Append(CodeChars[random[i] % CodeChars.Length]);
        }

        return code.ToString();
    }

    private static string FormatCode(string code)
    {
        
        if (code.Length == CodeLength)
        {
            return $"{code[0..8]}-{code[8..12]}-{code[12..16]}-{code[16..20]}-{code[20..32]}";
        }
        return code;
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<StudentDto>>> GetMyStudentsAsync(long teacherId, CancellationToken cancellationToken)
    {
        var students = await _teacherStudentRepository.GetStudentsByTeacherIdAsync(teacherId, cancellationToken);

        var dtos = students.Select(s => new StudentDto
        {
            Id = s.Student.Id,
            UserName = s.Student.UserName,
            Email = s.Student.Email,
            EstablishedAt = s.EstablishedAt
        }).ToArray();

        return new HttpOperationResult<IReadOnlyCollection<StudentDto>>(dtos, HttpStatusCode.OK);
    }

    public async Task<HttpOperationResult> RemoveStudentAsync(long teacherId, long studentId, CancellationToken cancellationToken)
    {
        
        var existing = await _teacherStudentRepository.GetByTeacherAndStudentAsync(teacherId, studentId, cancellationToken);
        if (existing == null)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.NotFound,
                Error = "Student is not connected to this teacher"
            };
        }

        
        if (existing.TeacherId != teacherId)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.Forbidden,
                Error = "Not authorized to remove this relationship"
            };
        }

        
        await _teacherStudentRepository.DeleteAsync(teacherId, studentId, cancellationToken);

        return new HttpOperationResult(HttpStatusCode.NoContent);
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<AttemptListItemDto>>> GetStudentAttemptsAsync(
        long teacherId,
        long studentId,
        string? status,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        
        var relationship = await _teacherStudentRepository.GetByTeacherAndStudentAsync(teacherId, studentId, cancellationToken);
        if (relationship == null)
        {
            return new HttpOperationResult<IReadOnlyCollection<AttemptListItemDto>>
            {
                Status = HttpStatusCode.NotFound,
                Error = "Student is not connected to this teacher"
            };
        }

        
        return await _studentAttemptService.GetAttemptsAsync(studentId, status, limit, offset, cancellationToken);
    }

    public async Task<HttpOperationResult<AttemptDetailsWithTasksDto>> GetStudentAttemptDetailsWithTasksAsync(
        long teacherId,
        long studentId,
        long attemptId,
        CancellationToken cancellationToken)
    {
        
        var relationship = await _teacherStudentRepository.GetByTeacherAndStudentAsync(teacherId, studentId, cancellationToken);
        if (relationship == null)
        {
            return new HttpOperationResult<AttemptDetailsWithTasksDto>
            {
                Status = HttpStatusCode.NotFound,
                Error = "Student is not connected to this teacher"
            };
        }

        
        return await _studentAttemptService.GetAttemptDetailsWithTasksAsync(studentId, attemptId, cancellationToken);
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<SubjectDto>>> GetStudentStatisticsSubjectsAsync(
        long teacherId,
        long studentId,
        CancellationToken cancellationToken)
    {
        
        var relationship = await _teacherStudentRepository.GetByTeacherAndStudentAsync(teacherId, studentId, cancellationToken);
        if (relationship == null)
        {
            return new HttpOperationResult<IReadOnlyCollection<SubjectDto>>
            {
                Status = HttpStatusCode.NotFound,
                Error = "Student is not connected to this teacher"
            };
        }

        
        return await _studentStatisticsService.GetAllSubjectsAsync(cancellationToken);
    }

    public async Task<HttpOperationResult<SubjectStatisticsDto>> GetStudentSubjectStatisticsAsync(
        long teacherId,
        long studentId,
        long subjectId,
        CancellationToken cancellationToken)
    {
        
        var relationship = await _teacherStudentRepository.GetByTeacherAndStudentAsync(teacherId, studentId, cancellationToken);
        if (relationship == null)
        {
            return new HttpOperationResult<SubjectStatisticsDto>
            {
                Status = HttpStatusCode.NotFound,
                Error = "Student is not connected to this teacher"
            };
        }

        
        return await _studentStatisticsService.GetSubjectStatisticsAsync(studentId, subjectId, cancellationToken);
    }
}

