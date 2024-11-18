// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdentitySidecar.Filters;

internal class RequestResponseLoggingFilter : IActionFilter
{
    private readonly ILogger logger;

    public RequestResponseLoggingFilter(ILogger logger)
    {
        this.logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext actionContext)
    {
        // Request body logging. Body.ToString() gets logged.
        foreach (ControllerParameterDescriptor param in
            actionContext.ActionDescriptor.Parameters.Cast<ControllerParameterDescriptor>())
        {
            if (param.ParameterInfo.IsDefined(typeof(FromBodyAttribute), true))
            {
                var entity = actionContext.ActionArguments[param.Name];
                var action = (string?)actionContext.RouteData.Values["action"];
                this.logger.LogInformation($"[{action}].Body: {entity}");
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext actionContext)
    {
        // Response logging. ObjectResult.Value.ToString() gets logged.
        if (actionContext.HttpContext.Request.Method != "GET" &&
            actionContext.Result is ObjectResult result &&
            result.Value != null)
        {
            var action = (string?)actionContext.RouteData.Values["action"];
            this.logger.LogInformation(
                $"[{action}].Response: {result.Value}");
        }
    }
}