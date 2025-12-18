using CTH.Database.Repositories.Interfaces;
using CTH.Services.Interfaces;
using CTH.Services.Models.Dto.TestAccess;
using PropTechPeople.Services.Models.ResultApiModels;
using System.Net;

namespace CTH.Services.Implementations;

public class TestAccessService : ITestAccessService
{
    private readonly ITestStudentAccessRepository _testStudentAccessRepository;
    private readonly ITestRepository _testRepository;

    public TestAccessService(
        ITestStudentAccessRepository testStudentAccessRepository,
        ITestRepository testRepository)
    {
        _testStudentAccessRepository = testStudentAccessRepository;
        _testRepository = testRepository;
    }

    public async Task<HttpOperationResult<IReadOnlyCollection<StudentAccessDto>>> GetStudentsByTestAsync(long testId, long teacherId, CancellationToken cancellationToken)
    {
        
        var test = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
        if (test == null)
        {
            return new HttpOperationResult<IReadOnlyCollection<StudentAccessDto>>
            {
                Status = HttpStatusCode.NotFound,
                Error = "Test not found"
            };
        }

        if (test.AuthorId != teacherId)
        {
            return new HttpOperationResult<IReadOnlyCollection<StudentAccessDto>>
            {
                Status = HttpStatusCode.Forbidden,
                Error = "You don't have permission to access this test"
            };
        }

        var accesses = await _testStudentAccessRepository.GetStudentsByTestIdAsync(testId, cancellationToken);
        var dtos = accesses.Select(a => new StudentAccessDto
        {
            Id = a.Student.Id,
            UserName = a.Student.UserName,
            Email = a.Student.Email,
            CreatedAt = a.CreatedAt
        }).ToArray();

        return new HttpOperationResult<IReadOnlyCollection<StudentAccessDto>>(dtos, HttpStatusCode.OK);
    }

    public async Task<HttpOperationResult> AddStudentAccessAsync(long testId, long studentId, long teacherId, CancellationToken cancellationToken)
    {
        
        var test = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
        if (test == null)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.NotFound,
                Error = "Test not found"
            };
        }

        if (test.AuthorId != teacherId)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.Forbidden,
                Error = "You don't have permission to modify this test"
            };
        }

        await _testStudentAccessRepository.AddStudentAccessAsync(testId, studentId, cancellationToken);
        return new HttpOperationResult(HttpStatusCode.Created);
    }

    public async Task<HttpOperationResult> RemoveStudentAccessAsync(long testId, long studentId, long teacherId, CancellationToken cancellationToken)
    {
        
        var test = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
        if (test == null)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.NotFound,
                Error = "Test not found"
            };
        }

        if (test.AuthorId != teacherId)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.Forbidden,
                Error = "You don't have permission to modify this test"
            };
        }

        await _testStudentAccessRepository.RemoveStudentAccessAsync(testId, studentId, cancellationToken);
        return new HttpOperationResult(HttpStatusCode.NoContent);
    }

    public async Task<HttpOperationResult> SetStudentAccessListAsync(long testId, IReadOnlyCollection<long> studentIds, long teacherId, CancellationToken cancellationToken)
    {
        
        var test = await _testRepository.GetTestByIdAsync(testId, cancellationToken);
        if (test == null)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.NotFound,
                Error = "Test not found"
            };
        }

        if (test.AuthorId != teacherId)
        {
            return new HttpOperationResult
            {
                Status = HttpStatusCode.Forbidden,
                Error = "You don't have permission to modify this test"
            };
        }

        
        await _testStudentAccessRepository.RemoveAllStudentAccessAsync(testId, cancellationToken);

        
        foreach (var studentId in studentIds)
        {
            await _testStudentAccessRepository.AddStudentAccessAsync(testId, studentId, cancellationToken);
        }

        return new HttpOperationResult(HttpStatusCode.OK);
    }
}

