using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CTH.Api.Extensions;

public class ExceptionHandlerAttribute : ExceptionFilterAttribute
{
    private readonly ILogger<ExceptionHandlerAttribute> _logger;

    public ExceptionHandlerAttribute(ILogger<ExceptionHandlerAttribute> logger)
    {
        _logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Request has failed.");

        var problem = new ProblemDetails()
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "An error occurred while processing your request.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = context.Exception.Message
        };
        var traceId = Activity.Current?.Id;
        if (traceId != null)
        {
            problem.Extensions["traceId"] = traceId;
        }

        context.Result = new JsonResult(problem)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };

        base.OnException(context);
    }
}
