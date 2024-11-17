// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Controllers;

public class ClientManager
{
    private readonly ILogger logger;
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private WorkspaceConfiguration wsConfig = default!;
    private HttpClient ccfAppClient = default!;
    private IConfiguration config;

    public ClientManager(
        ILogger logger,
        IConfiguration config)
    {
        this.logger = logger;
        this.config = config;
    }

    public async Task<HttpClient> GetAppClient()
    {
        await this.InitializeAppClient();
        return this.ccfAppClient;
    }

    public async Task<WorkspaceConfiguration> GetWsConfig()
    {
        await this.InitializeWsConfig();
        return this.wsConfig;
    }

    private async Task InitializeAppClient()
    {
        await this.InitializeWsConfig();
        if (this.ccfAppClient == null)
        {
            try
            {
                await this.semaphore.WaitAsync();
                if (this.ccfAppClient == null)
                {
                    this.ccfAppClient = this.InitializeClient();
                }
            }
            finally
            {
                this.semaphore.Release();
            }
        }
    }

    private HttpClient InitializeClient()
    {
        var client = new HttpClient();
        return client;
    }

    private async Task InitializeWsConfig()
    {
        if (this.wsConfig == null)
        {
            try
            {
                await this.semaphore.WaitAsync();
                if (this.wsConfig == null)
                {
                    this.wsConfig = await this.InitializeWsConfigFromEnvironment();
                }
            }
            finally
            {
                this.semaphore.Release();
            }
        }
    }

    private async Task<WorkspaceConfiguration> InitializeWsConfigFromEnvironment()
    {
        if (string.IsNullOrEmpty(this.config[SettingName.CcrgovPrivKey]))
        {
            throw new ArgumentException($"{SettingName.CcrgovPrivKey} setting must be specified.");
        }

        if (string.IsNullOrEmpty(this.config[SettingName.CcrgovPubKey]))
        {
            throw new ArgumentException($"{SettingName.CcrgovPubKey} setting must be specified.");
        }

        if (string.IsNullOrEmpty(this.config[SettingName.MaaRequest]))
        {
            throw new ArgumentException(
                $"{SettingName.MaaRequest} setting must be specified.");
        }

        var wsConfig = new WorkspaceConfiguration();

        wsConfig.PrivateKey =
            await File.ReadAllTextAsync(this.config[SettingName.CcrgovPrivKey]!);

        wsConfig.PublicKey =
            await File.ReadAllTextAsync(this.config[SettingName.CcrgovPubKey]!);

        var content = await File.ReadAllTextAsync(this.config[SettingName.MaaRequest]!);
        wsConfig.MaaRequest = JsonSerializer.Deserialize<JsonObject>(content)!;
        return wsConfig;
    }
}
