using CTH.Database.Entities.Public;
using CTH.Database.Repositories.Interfaces;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.Teachers;
using PropTechPeople.Services.Models.ResultApiModels;
using System.Net;

namespace CTH.Services.Implementations;

public class StudentTeacherService : IStudentTeacherService
{
    private readonly IInvitationCodeRepository _invitationCodeRepository;
    private readonly ITeacherStudentRepository _teacherStudentRepository;

    public StudentTeacherService(
        IInvitationCodeRepository invitationCodeRepository,
        ITeacherStudentRepository teacherStudentRepository)
    {
        _invitationCodeRepository = invitationCodeRepository;
        _teacherStudentRepository = teacherStudentRepository;
    }

    public async Task<HttpOperationResult<TeacherDto>> JoinTeacherByCodeAsync(long studentId, string invitationCode, CancellationToken cancellationToken)
    {
        // Нормализуем код: убираем пробелы, приводим к верхнему регистру
        var normalizedCode = invitationCode?.Trim().ToUpperInvariant() ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new HttpOperationResult<TeacherDto>
            {
                Status = HttpStatusCode.BadRequest,
                Error = "Invitation code is required"
            };
        }

        // Ищем код приглашения (код хранится с дефисами)
        var code = await _invitationCodeRepository.GetByCodeAsync(normalizedCode, cancellationToken);
        if (code == null)
        {
            return new HttpOperationResult<TeacherDto>
            {
                Status = HttpStatusCode.NotFound,
                Error = "Invitation code not found"
            };
        }

        // Проверяем статус кода
        if (code.Status != "active")
        {
            return new HttpOperationResult<TeacherDto>
            {
                Status = HttpStatusCode.BadRequest,
                Error = $"Invitation code is {code.Status}"
            };
        }

        // Проверяем срок действия
        if (code.ExpiresAt.HasValue && code.ExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            return new HttpOperationResult<TeacherDto>
            {
                Status = HttpStatusCode.BadRequest,
                Error = "Invitation code has expired"
            };
        }

        // Проверяем лимит использований
        if (code.MaxUses.HasValue && code.UsedCount >= code.MaxUses.Value)
        {
            return new HttpOperationResult<TeacherDto>
            {
                Status = HttpStatusCode.BadRequest,
                Error = "Invitation code has reached maximum uses"
            };
        }

        // Проверяем, не привязан ли уже ученик к этому учителю
        var existing = await _teacherStudentRepository.GetByTeacherAndStudentAsync(code.TeacherId, studentId, cancellationToken);
        if (existing != null)
        {
            if (existing.Status == "active" || existing.Status == "approved")
            {
                return new HttpOperationResult<TeacherDto>
                {
                    Status = HttpStatusCode.Conflict,
                    Error = "You are already connected to this teacher"
                };
            }
            // Если связь была отозвана, можно создать новую
        }

        // Создаем связь
        var teacherStudent = new TeacherStudent
        {
            TeacherId = code.TeacherId,
            StudentId = studentId,
            Status = "active",
            EstablishedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await _teacherStudentRepository.CreateAsync(teacherStudent, cancellationToken);
        }
        catch (Exception ex)
        {
            // Если связь уже существует (UNIQUE constraint), возвращаем ошибку
            if (ex.Message.Contains("UNIQUE") || ex.Message.Contains("duplicate"))
            {
                return new HttpOperationResult<TeacherDto>
                {
                    Status = HttpStatusCode.Conflict,
                    Error = "You are already connected to this teacher"
                };
            }
            throw;
        }

        // Увеличиваем счетчик использований
        code.UsedCount++;
        await _invitationCodeRepository.UpdateAsync(code, cancellationToken);

        // Получаем информацию об учителе
        var teachers = await _teacherStudentRepository.GetTeachersByStudentIdAsync(studentId, cancellationToken);
        var teacher = teachers.FirstOrDefault(t => t.TeacherId == code.TeacherId);

        if (teacher == null)
        {
            return new HttpOperationResult<TeacherDto>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "Failed to retrieve teacher information"
            };
        }

        var dto = new TeacherDto
        {
            Id = teacher.Teacher.Id,
            UserName = teacher.Teacher.UserName,
            Email = teacher.Teacher.Email,
            EstablishedAt = teacher.EstablishedAt
        };

        return new HttpOperationResult<TeacherDto>(dto, HttpStatusCode.Created);
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<TeacherDto>>> GetMyTeachersAsync(long studentId, CancellationToken cancellationToken)
    {
        var teachers = await _teacherStudentRepository.GetTeachersByStudentIdAsync(studentId, cancellationToken);

        var dtos = teachers.Select(t => new TeacherDto
        {
            Id = t.Teacher.Id,
            UserName = t.Teacher.UserName,
            Email = t.Teacher.Email,
            EstablishedAt = t.EstablishedAt
        }).ToArray();

        return new HttpOperationResult<IReadOnlyCollection<TeacherDto>>(dtos, HttpStatusCode.OK);
    }

    public async Task<HttpOperationResult> RemoveTeacherAsync(long studentId, long teacherId, CancellationToken cancellationToken)
    {
        // Проверяем, существует ли связь
        var existing = await _teacherStudentRepository.GetByTeacherAndStudentAsync(teacherId, studentId, cancellationToken);
        if (existing == null)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.NotFound,
                Error = "You are not connected to this teacher"
            };
        }

        // Проверяем, что это действительно связь этого ученика
        if (existing.StudentId != studentId)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.Forbidden,
                Error = "Not authorized to remove this relationship"
            };
        }

        // Удаляем связь
        await _teacherStudentRepository.DeleteAsync(teacherId, studentId, cancellationToken);

        return new HttpOperationResult(HttpStatusCode.NoContent);
    }
}

