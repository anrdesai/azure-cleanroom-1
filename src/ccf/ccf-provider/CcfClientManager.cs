// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Controllers;
using Microsoft.Extensions.Logging;

namespace CcfProvider;

public class CcfClientManager
{
    private readonly ILogger logger;
    private WorkspaceConfiguration wsConfig = default!;
    private HttpClientManager httpClientManager;

    public CcfClientManager(ILogger logger)
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

    public Task<HttpClient> GetGovClient(string ccfEndpoint, string serviceCert)
    {
        var client = this.httpClientManager.GetOrAddClient(
            ccfEndpoint,
            serviceCert,
            "gov-client",
            retryPolicy: HttpRetries.Policies.GetDefaultRetryPolicy(this.logger));
        return Task.FromResult(client);
    }

    public string GetGovApiVersion()
    {
        return "2024-07-01";
    }

    private async Task InitializeWsConfig()
    {
        if (this.wsConfig == null)
        {
            throw new Exception("Invoke /configure first to setup signing cert and key details");
        }

        await Task.CompletedTask;
    }
}
