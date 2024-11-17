// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Controllers;

public class WebContext
{
    public WebContext(ActionExecutingContext actionContext)
        : this(actionContext.HttpContext)
    {
    }

    public WebContext(HttpContext httpContext)
    {
        this.GovernanceApiPathPrefix =
            this.GetGovernanceApiPathPrefix(httpContext.Request);
    }

    public static string WebContextIdentifer => "WebContext";

    public string? GovernanceApiPathPrefix { get; }

    private string? GetGovernanceApiPathPrefix(HttpRequest request)
    {
        // Optional governance API path prefix override. If not specified then the
        // value specified in the container config kicks in to build the path.
        request.Headers.TryGetValue(
            "x-ms-ccr-governance-api-path-prefix",
            out StringValues pathPrefixValue);
        var pathPrefix = pathPrefixValue.FirstOrDefault();
        return pathPrefix;
    }
}
