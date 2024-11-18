// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json.Nodes;
using ArmClient;
using Common;
using Controllers;
using EntityDataModel;
using Microsoft.AspNetCore.Mvc;

namespace MetaRpEmulator;

[ApiController]
[Produces("application/json")]
public class AsyncController(
    ILogger logger,
    HttpClient serviceHttpClient,
    BackgroundWorkerQueue bgQueue,
    IAsyncOperationsManager opManager) : Controller
{
    private readonly BackgroundWorkerQueue bgQueue = bgQueue;
    private readonly ILogger logger = logger;
    private HttpClient serviceHttpClient = serviceHttpClient;
    private IAsyncOperationsManager opManager = opManager;

    [HttpPost]
    [HttpPut]
    [Route("/{**catchAll}")]
    public async Task<IActionResult> PerformAction(
        [FromRoute] string catchAll,
        CancellationToken cancellationToken,
        [FromBody] JsonObject? resource = null)
    {
        var path = this.HttpContext.GetResourceRelativeUri().Replace("%2F", "/");

        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(this.HttpContext.Request.Method),
            RequestUri = new Uri(path, UriKind.Relative)
        };
        if (resource != null)
        {
            requestMessage.Content = new StringContent(
                resource.ToString(),
                Encoding.UTF8,
                "application/json");
        }

        // Following:
        // https://github.com/microsoft/api-guidelines/blob/vNext/azure/ConsiderationsForServiceDesign.md#long-running-action-operations
        requestMessage.CopyHeaders(this.HttpContext.Request.Headers);
        var operationId = requestMessage.AddOperationStatusHeader();

        // Record operation start before seeing the response else there can be a race condition
        // where in service tries to update the operation before recording was done. We
        // can make a spurious entry if operation never got started.
        var cts = new CancellationTokenSource();
        await this.opManager.RecordOperationStart(
            operationId,
            requestMessage.Method.ToString(),
            path,
            cts);

        this.bgQueue.QueueBackgroundWorkItem(async () =>
        {
            JsonObject? result = null;
            ODataError? error = null;
            string status;
            int statusCode;
            try
            {
                // Passing cts.Token so that if the /cancel API gets invoked then this request
                // will get cancelled. As these calls can be long running its up to the
                // server on how to react to a client side cancellation. The server should get
                // a connection aborted and handle that as appropriate.
                var response = await this.serviceHttpClient.SendAsync(requestMessage, cts.Token);
                await response.ValidateStatusCodeAsync(this.logger);
                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    result = JsonNode.Parse(content)!.AsObject();
                }

                status = nameof(OperationState.Succeeded);
                statusCode = (int)response.StatusCode;
            }
            catch (Exception e)
            {
                status = nameof(OperationState.Failed);
                (statusCode, error) = ODataError.FromException(e);
            }

            await this.opManager.RecordOperationEnd(operationId, status, statusCode, result, error);
        });

        this.HttpContext.AddOperationStatusResponseHeader(operationId);
        var response = new JsonObject
        {
            ["id"] = operationId
        };

        return this.Accepted(response);
    }

    [HttpGet]
    [Route("/operations/{operationId}")]
    public async Task<IActionResult> GetOperationStatusAsync([FromRoute] string operationId)
    {
        OperationStatus? opStatus = await this.opManager.GetOperationStatus(operationId);
        if (opStatus == null)
        {
            return this.NotFound();
        }

        return this.Ok(opStatus);
    }

    [HttpPost]
    [Route("/operations/{operationId}/cancel")]
    public async Task<IActionResult> CancelOpeation([FromRoute] string operationId)
    {
        bool cancelled = await this.opManager.CancelOperation(operationId);
        return this.Ok(cancelled);
    }

    [HttpGet]
    [Route("/show")]
    public IActionResult Show()
    {
        var view = new WsConfig
        {
            EnvironmentVariables = Environment.GetEnvironmentVariables(),
            TargetEndpoint = this.serviceHttpClient.BaseAddress!.ToString()
        };

        return this.Ok(view);
    }

    public class WsConfig
    {
        public System.Collections.IDictionary EnvironmentVariables { get; set; } = default!;

        public string TargetEndpoint { get; set; } = default!;
    }
}