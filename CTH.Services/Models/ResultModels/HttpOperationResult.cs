using System.Net;

namespace PropTechPeople.Services.Models.ResultApiModels;

public class HttpOperationResult
{
    public HttpStatusCode Status { get; set; }
    public string? Error { get; set; }

    public HttpOperationResult() { }

    public HttpOperationResult(HttpStatusCode status)
    {
        Status = status;
    }

    public bool IsSuccessful => (int)Status is >= 200 and <= 300;

    public HttpOperationResult<K> ConvertErrorResultToAnotherType<K>()
    {
        return new HttpOperationResult<K>
        {
            Error = Error,
            Status = Status
        };
    }
}

public class HttpOperationResult<T> : HttpOperationResult
{
    public HttpOperationResult()
    {
    }

    public HttpOperationResult(T? result, HttpStatusCode status)
    {
        Result = result;
        Status = status;
    }
    public T? Result { get; set; }
}
