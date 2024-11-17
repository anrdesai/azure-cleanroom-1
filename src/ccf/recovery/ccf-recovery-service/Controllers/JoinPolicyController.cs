// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
public class JoinPolicyController : BaseController
{
    private readonly ILogger logger;
    private readonly IConfiguration configuration;
    private readonly CcfRecoveryService service;
    private readonly IPolicyStore policyStore;

    public JoinPolicyController(
        ILogger logger,
        IConfiguration configuration,
        CcfRecoveryService service,
        IPolicyStore policyStore)
        : base(logger, configuration, policyStore)
    {
        this.logger = logger;
        this.configuration = configuration;
        this.service = service;
        this.policyStore = policyStore;
    }

    [HttpPost("/network/joinpolicy/set")]
    public async Task<IActionResult> SetNetworkJoinPolicy(
        [FromBody] JsonObject content)
    {
        var reportInfo = await this.VerifyAttestationReport(content);

        // Extract the join policy from the signed incoming payload.
        var data = this.GetSignedData<JoinPolicyInput>(reportInfo.PublicKey, content);
        var joinPolicy = data.JoinPolicy;

        // We block an agent from removing itself from the join policy as that can lead to a
        // dead lock situation where a network can then no longer access the recovery service.
        if (!joinPolicy.Snp.HostData.Any(x => x == reportInfo.HostData.ToLower()))
        {
            throw new ApiException(
                System.Net.HttpStatusCode.BadRequest,
                "CannotRemoveSelf",
                $"Incoming join policy does not include the calling agent's hostData value " +
                $"{reportInfo.HostData.ToLower()}. A recovery agent is blocked from removing " +
                $"itself from the join policy as that can lead to a dead lock situation " +
                $"where the CCF network can then no longer access the recovery service.");
        }

        await this.policyStore.SetNetworkJoinPolicy(joinPolicy);
        return this.Ok();
    }

    public class JoinPolicyInput
    {
        [JsonPropertyName("joinPolicy")]
        public NetworkJoinPolicy JoinPolicy { get; set; } = default!;
    }
}
