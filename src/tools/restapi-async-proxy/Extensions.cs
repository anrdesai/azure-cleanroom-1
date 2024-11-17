// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Web;
using Microsoft.AspNetCore.Http.Extensions;

namespace MetaRpEmulator;

public static class Extensions
{
    public static void CopyHeaders(
        this HttpRequestMessage requestMessage,
        IHeaderDictionary headerDictionary)
    {
        foreach (var header in headerDictionary)
        {
            requestMessage.Headers.TryAddWithoutValidation(
                header.Key,
                [.. header.Value]);
        }
    }

    public static string GetResourceRelativeUri(this HttpContext httpContext)
    {
        var path = UriHelper.BuildRelative(
            path: httpContext.Request.Path,
            query: httpContext.Request.QueryString);

        return path;
    }

    public static string AddOperationStatusHeader(this HttpRequestMessage requestMessage)
    {
        var uri = requestMessage.RequestUri!.OriginalString;
        var operationId = GetOperationIdHeaderValue(requestMessage) ?? Guid.NewGuid().ToString("N");
        var operationStatusId = $"/operations/{operationId}";
        int queryParamIndex = uri.IndexOf('?');
        if (queryParamIndex != -1)
        {
            var query = HttpUtility.ParseQueryString(uri.Substring(queryParamIndex));
            string? apiVersion = query["api-version"];
            if (apiVersion != null)
            {
                operationStatusId += $"?api-version={apiVersion}";
            }
        }

        requestMessage.Headers.Add("Operation-Id", operationStatusId);
        return operationId;
    }

    public static void AddOperationStatusResponseHeader(
        this HttpContext httpContext,
        string operationId)
    {
        string operationUrl;
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        if (requestHeaders.Referer != null)
        {
            operationUrl = requestHeaders.Referer.Scheme
                + "://"
                + requestHeaders.Referer.Authority.TrimEnd('/');
        }
        else
        {
            operationUrl =
                httpContext.Request.Scheme + "://" + httpContext.Request.Host;
        }

        httpContext.Response.Headers["Operation-Location"] = operationUrl +
            $"/operations/{operationId}";
        httpContext.Response.Headers["Operation-Id"] = operationId;
    }

    private static string? GetOperationIdHeaderValue(HttpRequestMessage request)
    {
        string? operationId = null;
        if (request.Headers.TryGetValues("Operation-Id", out var values))
        {
            operationId = values!.First();
        }

        return operationId;
    }
}
