// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
public class ServicesController : ControllerBase
{
    private readonly ILogger logger;
    private readonly IConfiguration configuration;
    private readonly CcfRecoveryService service;
    private readonly IPolicyStore policyStore;

    public ServicesController(
        ILogger logger,
        IConfiguration configuration,
        CcfRecoveryService service,
        IPolicyStore policyStore)
    {
        this.logger = logger;
        this.configuration = configuration;
        this.service = service;
        this.policyStore = policyStore;
    }

    [HttpGet("/report")]
    public async Task<IActionResult> GetServiceReport()
    {
        // This API currently requires no attestation report input. Any client
        // can query for public information.
        RecoveryServiceReport report = await this.service.GetServiceReport();
        return this.Ok(report);
    }

    [HttpGet("/network/joinpolicy")]
    public async Task<IActionResult> GetNetworkJoinPolicy()
    {
        // This API currently requires no attestation report input. Any client
        // can query for public information.
        var joinPolicy = await this.policyStore.GetNetworkJoinPolicy();
        return this.Ok(joinPolicy);
    }

    [HttpGet("/network/securitypolicy")]
    public async Task<IActionResult> GetNetworkSecurityPolicy()
    {
        // This API currently requires no attestation report input. Any client
        // can query for public information.
        return this.Ok(await this.policyStore.GetSecurityPolicy());
    }
}
