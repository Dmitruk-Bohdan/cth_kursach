using Microsoft.AspNetCore.Mvc;
using PropTechPeople.Services.Models.ResultApiModels;
using System.Net;

namespace CTH.Services.Extensions;

public static class HttpOperationResultExtension
{
    public static IActionResult ToActionResult(this HttpOperationResult httpOperationResult)
    {
        return httpOperationResult.Status switch
        {
            HttpStatusCode.OK => new NoContentResult(),
            HttpStatusCode.NoContent => new NoContentResult(),
            HttpStatusCode.NotFound => new NotFoundObjectResult(httpOperationResult.Error),
            HttpStatusCode.Conflict => new ConflictObjectResult(httpOperationResult.Error),
            HttpStatusCode.Unauthorized => new UnauthorizedObjectResult(httpOperationResult.Error),
            HttpStatusCode.Forbidden => new ForbidResult(),
            _ => new BadRequestObjectResult(httpOperationResult.Error)
        };
    }

    public static IActionResult ToActionResult<T>(this HttpOperationResult<T> httpOperationResult) where T : class
    {
        return httpOperationResult.Status switch
        {
            HttpStatusCode.OK => new OkObjectResult(httpOperationResult.Result),
            HttpStatusCode.NotFound => new NotFoundObjectResult(httpOperationResult.Error),
            HttpStatusCode.Conflict => new ConflictObjectResult(httpOperationResult.Error),
            HttpStatusCode.Unauthorized => new UnauthorizedObjectResult(httpOperationResult.Error),
            HttpStatusCode.Forbidden => new ForbidResult(),
            _ => new BadRequestObjectResult(httpOperationResult.Error)
        };
    }
}
