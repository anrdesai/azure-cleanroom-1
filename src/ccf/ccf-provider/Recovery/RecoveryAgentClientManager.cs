// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Controllers;
using Microsoft.Extensions.Logging;

namespace CcfProvider;

public class RecoveryAgentClientManager
{
    private readonly ILogger logger;
    private WorkspaceConfiguration wsConfig = default!;
    private HttpClientManager httpClientManager;

    public RecoveryAgentClientManager(ILogger logger)
    {
        this.logger = logger;
        this.httpClientManager = new(logger);
    }

    public void SetWsConfig(WorkspaceConfiguration wsConfig)
    {
        this.wsConfig = wsConfig;
    }

    public async Task<WorkspaceConfiguration> GetWsConfig()
    {
        await this.InitializeWsConfig();
        return this.wsConfig;
    }

    public WorkspaceConfiguration TryGetWsConfig()
    {
        return this.wsConfig;
    }

    public async Task CheckWsConfig()
    {
        await this.InitializeWsConfig();
    }

    public Task<HttpClient> GetClient(string agentEndpoint, string serviceCert)
    {
        var client = this.httpClientManager.GetOrAddClient(
            agentEndpoint,
            serviceCert,
            "recovery-agent",
            retryPolicy: HttpRetries.Policies.GetDefaultRetryPolicy(this.logger));
        return Task.FromResult(client);
    }

    private async Task InitializeWsConfig()
    {
        if (this.wsConfig == null)
        {
            throw new Exception("Invoke /configure first to setup signing cert and key details");
        }

        await Task.CompletedTask;
    }

    private class AgentClient
    {
        public HttpClient HttpClient { get; set; } = default!;
    }
}
