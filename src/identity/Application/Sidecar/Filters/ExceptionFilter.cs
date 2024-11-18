// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Azure.CleanRoomSidecar.Identity.Errors;

namespace IdentitySidecar.Filters;

internal class ExceptionFilter : IExceptionFilter
{
    private ILogger logger;

    public ExceptionFilter(ILogger logger)
    {
        this.logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        IdentityException identityException = context.Exception as IdentityException ??
            IdentityException.InternalError();

        string statusCodeName = IdentityException.GetHttpStatusCode(identityException.ErrorCode);
        var httpStatusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), statusCodeName);

        var apiError = new ApiError()
        {
            Code = identityException.ErrorCode.ToString(),
            Message = identityException.GetMessage(),
            PossibleCauses = identityException.GetPossibleCauses(),
            RecommendedAction = identityException.GetRecommendedAction(),
        };

        this.logger.LogError(
            context.Exception,
            $"Transforming the exception to API error: {JsonSerializer.Serialize(apiError)}");
        context.Result = new ObjectResult(apiError);
        context.HttpContext.Response.StatusCode = (int)httpStatusCode;
    }
}

internal class ApiError
{
    /// <summary>
    /// Gets or sets the error code for the API error.
    /// </summary>
    public string Code { get; set; } = default!;

    /// <summary>
    /// Gets or sets the message for the error.
    /// </summary>
    public string Message { get; set; } = default!;

    /// <summary>
    /// Gets or sets the possible causes for the error.
    /// </summary>
    public string PossibleCauses { get; set; } = default!;

    /// <summary>
    /// Gets or sets the recommended action for the error.
    /// </summary>
    public string RecommendedAction { get; set; } = default!;
}
