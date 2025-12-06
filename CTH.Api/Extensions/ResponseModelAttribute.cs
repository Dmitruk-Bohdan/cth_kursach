using CTH.Services.Models;
using CTH.Services.Models.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;
using System.Security.Principal;

namespace CTH.Api.Extensions;

public class ResponseModelAttribute : ActionFilterAttribute
{
    private static readonly List<int> SuccessCodes = new() { StatusCodes.Status200OK, StatusCodes.Status201Created, StatusCodes.Status204NoContent };

    public override void OnResultExecuting(ResultExecutingContext context)
    {
        if (!context.ModelState.IsValid) return;

        var result = context.Result as ObjectResult;

        if (result != null && SuccessCodes.Contains(result.StatusCode ?? 500))
        {
            var method = context.HttpContext.Request.Method;
            var model = result.Value;

            if ((HttpMethods.IsGet(method) || HttpMethods.IsPut(method)) && model == null)
            {
                context.Result = new NotFoundObjectResult(new ResponseModel
                {
                    Success = false,
                    Message = "Item was not found.",
                    Result = null
                });
            }
            else if (HttpMethods.IsPost(method) && model is IIdentity identity)
            {
                var location = $"{context.HttpContext.Request.Host}/{context.Controller.GetType().Name.Replace("Controller", string.Empty)}";
                context.Result = new CreatedResult(location, new ResponseModel
                {
                    Success = true,
                    Result = model
                });
            }
            else if (HttpMethods.IsPost(method) && result is IActionResult)
            {
                context.Result = result;
            }
            else if (HttpMethods.IsDelete(method) && model == null)
            {
                context.Result = new OkObjectResult(new ResponseModel
                {
                    Success = true
                });
            }
            else
            {
                context.Result = new OkObjectResult(new ResponseModel
                {
                    Success = true,
                    Result = result.Value
                });
            }
        }

        base.OnResultExecuting(context);
    }
}
