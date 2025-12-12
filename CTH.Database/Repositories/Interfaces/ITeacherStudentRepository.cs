using CTH.Database.Entities.Public;

namespace CTH.Database.Repositories.Interfaces;

public interface ITeacherStudentRepository
{
    Task<long> CreateAsync(TeacherStudent teacherStudent, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TeacherStudent>> GetTeachersByStudentIdAsync(long studentId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TeacherStudent>> GetStudentsByTeacherIdAsync(long teacherId, CancellationToken cancellationToken);
    Task<TeacherStudent?> GetByTeacherAndStudentAsync(long teacherId, long studentId, CancellationToken cancellationToken);
    Task DeleteAsync(long teacherId, long studentId, CancellationToken cancellationToken);
}

