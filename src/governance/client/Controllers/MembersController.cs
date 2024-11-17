// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using CoseUtils;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
public class MembersController : ClientControllerBase
{
    public MembersController(
        ILogger<MembersController> logger,
        IHttpContextAccessor httpContextAccessor)
        : base(logger, httpContextAccessor)
    {
    }

    [HttpGet("/members")]
    public async Task<JsonObject> Get()
    {
        var ccfClient = await this.CcfClientManager.GetGovClient();
        using HttpResponseMessage response = await ccfClient.GetAsync("gov/members");
        await response.ValidateStatusCodeAsync(this.Logger);
        response.LogRequest(this.Logger);
        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
        return jsonResponse!;
    }

    [HttpPost("/members/statedigests/update")]
    public async Task<JsonObject> GetStateDigestUpdate()
    {
        var ccfClient = await this.CcfClientManager.GetGovClient();
        var wsConfig = this.CcfClientManager.GetWsConfig();
        var payload = await GovernanceCose.CreateGovCoseSign1Message(
            wsConfig,
            GovMessageType.StateDigest,
            null);
        using HttpRequestMessage request = Cose.CreateHttpRequestMessage(
            $"gov/members/state-digests/{wsConfig.MemberId}:update" +
            $"?api-version={this.CcfClientManager.GetGovApiVersion()}",
            payload);
        using HttpResponseMessage response = await ccfClient.SendAsync(request);
        await response.ValidateStatusCodeAsync(this.Logger);
        response.LogRequest(this.Logger);
        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
        return jsonResponse!;
    }

    [HttpPost("/members/statedigests/ack")]
    public async Task<string> AckStateDigest()
    {
        var ccfClient = await this.CcfClientManager.GetGovClient();
        var wsConfig = this.CcfClientManager.GetWsConfig();
        var stateDigest = await this.GetStateDigestUpdate();
        var payload = await GovernanceCose.CreateGovCoseSign1Message(
            wsConfig,
            GovMessageType.Ack,
            stateDigest.ToJsonString());
        using HttpRequestMessage request = Cose.CreateHttpRequestMessage(
            $"gov/members/state-digests/{wsConfig.MemberId}:ack" +
            $"?api-version={this.CcfClientManager.GetGovApiVersion()}",
            payload);
        using HttpResponseMessage response = await ccfClient.SendAsync(request);
        await response.ValidateStatusCodeAsync(this.Logger);
        response.LogRequest(this.Logger);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        return jsonResponse!;
    }
}
