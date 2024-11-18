// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Controllers;

public class WebContext
{
    public const string ApiVersionParam = "api-version";

    public WebContext(ActionExecutingContext actionContext)
        : this(actionContext.HttpContext)
    {
        this.OperationName = ((ControllerBase)actionContext.Controller)
            .ControllerContext.ActionDescriptor.ActionName;
    }

    public WebContext(HttpContext httpContext)
    {
        this.ApiVersion = this.GetApiVersion(httpContext.Request);
        this.ClientRequestId = this.GetClientRequestId(httpContext.Request);
        this.CorrelationId = this.GetCorrelationRequestId(httpContext.Request);
        this.Url = httpContext.Request.Path.Value!;
        this.HttpMethod = httpContext.Request.Method;
        this.OperationName = "NA";
    }

    public static string WebContextIdentifer => "WebContext";

    public string? Culture { get; }

    public string ClientRequestId { get; }

    public Guid CorrelationId { get; }

    public string? ApiVersion { get; }

    public string Url { get; }

    public string OperationName { get; }

    public string HttpMethod { get; }

    private string? GetApiVersion(HttpRequest request)
    {
        request.Query.TryGetValue(ApiVersionParam, out StringValues apiVersion);
        return apiVersion.FirstOrDefault();
    }

    private string GetClientRequestId(HttpRequest request)
    {
        request.Headers.TryGetValue(
            CustomHttpHeader.MsClientRequestId,
            out StringValues clientRequestId);

        return clientRequestId.FirstOrDefault() ?? Guid.NewGuid().ToString();
    }

    private Guid GetCorrelationRequestId(HttpRequest request)
    {
        request.Headers.TryGetValue(
            CustomHttpHeader.MsCorrelationRequestId,
            out StringValues correlationRequestId);

        if (!Guid.TryParse(correlationRequestId.FirstOrDefault(), out Guid correlationId))
        {
            correlationId = Guid.NewGuid();
        }

        return correlationId;
    }
}