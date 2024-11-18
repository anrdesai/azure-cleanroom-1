// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using CcfProvider;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
public class WorkspacesController : ControllerBase
{
    private readonly ILogger logger;
    private readonly CcfClientManager ccfClientManager;
    private readonly RecoveryAgentClientManager agentClientManager;

    public WorkspacesController(
        CcfClientManager ccfClientManager,
        RecoveryAgentClientManager agentClientManager,
        ILogger logger)
    {
        this.ccfClientManager = ccfClientManager;
        this.agentClientManager = agentClientManager;
        this.logger = logger;
    }

    [HttpPost("/configure")]
    public async Task<IActionResult> SetWorkspaceConfig(
        [FromForm] WorkspaceConfigurationModel model)
    {
        if (model?.SigningCertPemFile == null || model.SigningCertPemFile.Length <= 0)
        {
            return this.BadRequest("No file was uploaded.");
        }

        if (model?.SigningKeyPemFile == null || model.SigningKeyPemFile.Length <= 0)
        {
            return this.BadRequest("No file was uploaded.");
        }

        string signingCert;
        string signingKey;
        using (var reader = new StreamReader(model.SigningCertPemFile.OpenReadStream()))
        {
            signingCert = await reader.ReadToEndAsync();
        }

        using (var reader = new StreamReader(model.SigningKeyPemFile.OpenReadStream()))
        {
            signingKey = await reader.ReadToEndAsync();
        }

        this.ccfClientManager.SetWsConfig(new CcfProvider.WorkspaceConfiguration
        {
            SigningCert = signingCert,
            SigningKey = signingKey
        });
        this.agentClientManager.SetWsConfig(new CcfProvider.WorkspaceConfiguration
        {
            SigningCert = signingCert,
            SigningKey = signingKey
        });
        return this.Ok("Workspace details configured successfully.");
    }

    [HttpGet("/show")]
    public IActionResult Show([FromQuery] bool? signingKey = false)
    {
        WorkspaceConfiguration copy;
        var wsConfig = this.ccfClientManager.TryGetWsConfig();
        if (wsConfig != null)
        {
            copy = JsonSerializer.Deserialize<WorkspaceConfiguration>(
                JsonSerializer.Serialize(wsConfig))!;
            if (!signingKey.GetValueOrDefault())
            {
                copy.SigningKey = "<redacted>";
            }
        }
        else
        {
            copy = new WorkspaceConfiguration();
        }

        copy.EnvironmentVariables = Environment.GetEnvironmentVariables();
        return this.Ok(copy);
    }
}
