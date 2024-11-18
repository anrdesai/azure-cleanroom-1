// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Constants;
using Controllers;
using IdentitySidecar.Utilities;
using Metrics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Azure.IdentitySidecar.Telemetry.Metrics;
using Utilities;

namespace IdentitySidecar.Filters;

internal class GlobalActionFilter : IActionFilter
{
    private readonly ILogger logger;
    private readonly IMetricsEmitter metricsEmitter;
    private readonly IConfiguration configuration;

    public GlobalActionFilter(
        ILogger logger,
        IConfiguration configuration,
        IMetricsEmitter metricsEmitter)
    {
        this.logger = logger;
        this.configuration = configuration;
        this.metricsEmitter = metricsEmitter;
    }

    public string ActionDurationStart => "ActionDurationStart";

    public void OnActionExecuting(ActionExecutingContext actionContext)
    {
        var webContext = new WebContext(actionContext);
        webContext.SetLoggingContext();

        actionContext.HttpContext.Items[this.ActionDurationStart] = MonotonicTime.Now;
        actionContext.HttpContext.Items[WebContext.WebContextIdentifer] = webContext;

        Activity.Current?.SetTag(BaggageItemName.ClientRequestId, webContext.ClientRequestId);
        Activity.Current?.SetTag(BaggageItemName.CorrelationRequestId, webContext.CorrelationId);

        this.metricsEmitter.Log(IdentityMetric.RestApiStarted(
            webContext.OperationName,
            webContext.HttpMethod,
            webContext.ApiVersion,
            JsonUtilities.Serialize(webContext)));
    }

    public void OnActionExecuted(ActionExecutedContext actionContext)
    {
        var webContext = (WebContext)actionContext.HttpContext.Items[
            WebContext.WebContextIdentifer]!;

        webContext.SetLoggingContext();
        var actionStart =
            (MonotonicTime)actionContext.HttpContext.Items[this.ActionDurationStart]!;
        TimeSpan timeTaken = MonotonicTime.Now - actionStart;

        Activity.Current?.SetTag(BaggageItemName.ClientRequestId, webContext.ClientRequestId);
        Activity.Current?.SetTag(BaggageItemName.CorrelationRequestId, webContext.CorrelationId);

        if (actionContext.Exception == null)
        {
            this.metricsEmitter.Log(IdentityMetric.RestApiSucceeded(
                (long)timeTaken.TotalMilliseconds,
                webContext.OperationName,
                webContext.HttpMethod,
                webContext.ApiVersion,
                webContext.Url));
        }
        else
        {
            ExceptionDimensions ed = actionContext.Exception.GetDimensions();
            this.metricsEmitter.Log(IdentityMetric.RestApiFailed(
                (long)timeTaken.TotalMilliseconds,
                webContext.OperationName,
                ed.ErrorCode,
                webContext.HttpMethod,
                webContext.ApiVersion,
                webContext.Url));
        }
    }
}