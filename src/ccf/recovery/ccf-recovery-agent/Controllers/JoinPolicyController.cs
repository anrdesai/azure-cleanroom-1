// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using AttestationClient;
using Microsoft.AspNetCore.Mvc;
using static Controllers.Ccf;

namespace Controllers;

[ApiController]
public class JoinPolicyController : BaseController
{
    private readonly ILogger logger;
    private readonly IConfiguration configuration;
    private readonly ClientManager clientManager;

    public JoinPolicyController(
        ILogger logger,
        IConfiguration configuration,
        ClientManager clientManager)
        : base(logger, configuration, clientManager)
    {
        this.logger = logger;
        this.configuration = configuration;
        this.clientManager = clientManager;
    }

    [HttpPost("/members/{memberId}/network/joinpolicy/set")]
    public async Task<IActionResult> SetNetworkJoinPolicy(
        [FromRoute] string memberId,
        [FromBody] byte[] content)
    {
        // Verify caller is an active member of the consortium.
        (var err, var payload) = await this.VerifyMemberAuthentication(memberId, content);
        if (err != null)
        {
            return this.BadRequest(err);
        }

        var input = JsonSerializer.Deserialize<SetNetworkJoinPolicyInput>(payload)!;
        err = ValidateInput(input);
        if (err != null)
        {
            return this.BadRequest(err);
        }

        var wsConfig = await this.clientManager.GetWsConfig();
        var ccfClient = await this.clientManager.GetCcfClient();
        var joinPolicyInfo = await ccfClient.GetFromJsonAsync<JoinPolicyInfo>(
            $"/gov/service/join-policy" +
            $"?api-version={this.clientManager.GetGovApiVersion()}");
        if (joinPolicyInfo?.Snp?.HostData == null)
        {
            err = new ODataError("CcfJoinPolicyNotSet", "CCF network is reporting no join policy.");
            return this.BadRequest(err);
        }

        if (joinPolicyInfo.Snp.HostData.Keys.Count == 0)
        {
            err = new ODataError(
                "CcfJoinPolicyHostDataEmpty",
                "CCF network is reporting no host data values in the join policy.");
            return this.BadRequest(err);
        }

        // Get attestation report content to send in the request.
        var hostDataArray = new JsonArray();
        joinPolicyInfo.Snp.HostData.Keys.ToList().ForEach(x => hostDataArray.Add(x));
        var dataContent = new JsonObject
        {
            ["joinPolicy"] = new JsonObject
            {
                ["snp"] = new JsonObject
                {
                    ["hostData"] = hostDataArray
                }
            }
        };

        this.logger.LogInformation(
            $"Requesting recovery service to set network join policy as " +
            $"{JsonSerializer.Serialize(dataContent)}.");

        (var data, var signature) =
            this.PrepareSignedData(dataContent, wsConfig.Attestation.PrivateKey);

        JsonObject svcContent = Attestation.PrepareSignedDataRequestContent(
            data,
            signature,
            wsConfig.Attestation.PublicKey,
            wsConfig.Attestation.Report);

        var svcClient = await this.GetRecoverySvcClient(input.AgentConfig.RecoveryService);
        var response = await svcClient.PostAsync(
            $"network/joinpolicy/set",
            JsonContent.Create(svcContent));
        await response.ValidateStatusCodeAsync(this.logger);
        return this.Ok();

        static ODataError? ValidateInput(SetNetworkJoinPolicyInput input)
        {
            if (input.AgentConfig == null)
            {
                return new ODataError("InputMissing", "agentConfig input is required.");
            }

            return null;
        }
    }
}
