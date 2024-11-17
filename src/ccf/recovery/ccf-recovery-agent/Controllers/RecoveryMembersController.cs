// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Nodes;
using AttestationClient;
using CoseUtils;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
public class RecoveryMembersController : BaseController
{
    private readonly ILogger logger;
    private readonly IConfiguration configuration;
    private readonly ClientManager clientManager;

    public RecoveryMembersController(
        ILogger logger,
        IConfiguration configuration,
        ClientManager clientManager)
        : base(logger, configuration, clientManager)
    {
        this.logger = logger;
        this.configuration = configuration;
        this.clientManager = clientManager;
    }

    [HttpPost("/members/{memberId}/recoveryMembers/generate")]
    public async Task<IActionResult> GenerateRecoveryMember(
        [FromRoute] string memberId,
        [FromBody] byte[] content)
    {
        // Verify caller is an active member of the consortium.
        (var err, var payload) = await this.VerifyMemberAuthentication(memberId, content);
        if (err != null)
        {
            return this.BadRequest(err);
        }

        var input = JsonSerializer.Deserialize<GenerateRecoveryMemberInput>(payload)!;
        err = ValidateInput(input);
        if (err != null)
        {
            return this.BadRequest(err);
        }

        this.logger.LogInformation(
            $"Requesting recovery service to generate recovery member {input.MemberName}.");

        var wsConfig = await this.clientManager.GetWsConfig();

        // Get attestation report to send in the request.
        var dataContent = new JsonObject
        {
            ["memberName"] = input.MemberName
        };

        (var data, var signature) =
            this.PrepareSignedData(dataContent, wsConfig.Attestation.PrivateKey);

        JsonObject svcContent = Attestation.PrepareSignedDataRequestContent(
            data,
            signature,
            wsConfig.Attestation.PublicKey,
            wsConfig.Attestation.Report);

        var svcClient = await this.GetRecoverySvcClient(input.AgentConfig.RecoveryService);
        var response = await svcClient.PostAsync(
            $"members/generate",
            JsonContent.Create(svcContent));
        await response.ValidateStatusCodeAsync(this.logger);

        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        return this.Ok(body);

        static ODataError? ValidateInput(GenerateRecoveryMemberInput input)
        {
            if (string.IsNullOrEmpty(input.MemberName))
            {
                return new ODataError("InputMissing", "memberName input is required.");
            }

            if (input.AgentConfig == null)
            {
                return new ODataError("InputMissing", "agentConfig input is required.");
            }

            return null;
        }
    }

    [HttpPost("/members/{memberId}/recoveryMembers/activate")]
    public async Task<IActionResult> ActivateRecoveryMember(
        [FromRoute] string memberId,
        [FromBody] byte[] content)
    {
        // Verify caller is an active member of the consortium.
        (var err, var payload) = await this.VerifyMemberAuthentication(memberId, content);
        if (err != null)
        {
            return this.BadRequest(err);
        }

        var input = JsonSerializer.Deserialize<ActivateRecoveryMemberInput>(payload)!;
        err = ValidateInput(input);
        if (err != null)
        {
            return this.BadRequest(err);
        }

        this.logger.LogInformation(
            $"Requesting recovery service to activate member {input.MemberName}.");

        var wsConfig = await this.clientManager.GetWsConfig();

        // To complete member activation the steps are:
        // - Request recovery service to generate the sate digest message which would then be
        //   submitted to CCF to get the state digest.
        // - The response from CCF would be sent to recovery service to ack it.
        // - The ack will then be submitted to CCF.
        var svcClient = await this.GetRecoverySvcClient(input.AgentConfig.RecoveryService);

        var recoveryMember = (await svcClient.GetFromJsonAsync<JsonObject>(
            $"members/{input.MemberName}"))!;
        var signingCert = recoveryMember["signingCert"]!.ToString();
        using var cert = X509Certificate2.CreateFromPem(signingCert);
        var recoveryMemberId = cert.GetCertHashString(HashAlgorithmName.SHA256).ToLower();

        // Get attestation report content to send in the request.
        var dataContent = new JsonObject
        {
            ["memberName"] = input.MemberName
        };

        (var data, var signature) =
            this.PrepareSignedData(dataContent, wsConfig.Attestation.PrivateKey);

        JsonObject svcContent = Attestation.PrepareSignedDataRequestContent(
            data,
            signature,
            wsConfig.Attestation.PublicKey,
            wsConfig.Attestation.Report);
        byte[] stateDigestMessage;
        using (var generateDigestResponse = await svcClient.PostAsync(
            $"members/generateStateDigestMessage",
            JsonContent.Create(svcContent)))
        {
            await generateDigestResponse.ValidateStatusCodeAsync(this.logger);

            var body = (await generateDigestResponse.Content.ReadFromJsonAsync<JsonObject>())!;
            var wrappedValue = Convert.FromBase64String(body["message"]!.ToString());
            stateDigestMessage = Attestation.UnwrapRsaOaepAesKwpValue(
                wrappedValue,
                wsConfig.Attestation.PrivateKey);
        }

        JsonObject stateDigest;
        var ccfClient = await this.clientManager.GetCcfClient();
        using (HttpRequestMessage request = Cose.CreateHttpRequestMessage(
            $"gov/members/state-digests/{recoveryMemberId}:update" +
            $"?api-version={this.clientManager.GetGovApiVersion()}",
            stateDigestMessage))
        {
            using HttpResponseMessage stateDigestResponse = await ccfClient.SendAsync(request);
            await stateDigestResponse.ValidateStatusCodeAsync(this.logger);
            stateDigest = (await stateDigestResponse.Content.ReadFromJsonAsync<JsonObject>())!;
        }

        // Get attestation report and the state digest content to send in the request.
        dataContent = new JsonObject
        {
            ["memberName"] = input.MemberName,
            ["stateDigest"] = stateDigest
        };
        (data, signature) =
            this.PrepareSignedData(dataContent, wsConfig.Attestation.PrivateKey);
        svcContent = Attestation.PrepareSignedDataRequestContent(
            data,
            signature,
            wsConfig.Attestation.PublicKey,
            wsConfig.Attestation.Report);

        byte[] stateDigestAckMessage;
        using (var generateAckResponse = await svcClient.PostAsync(
            $"members/generateStateDigestAckMessage",
            JsonContent.Create(svcContent)))
        {
            await generateAckResponse.ValidateStatusCodeAsync(this.logger);

            var body = (await generateAckResponse.Content.ReadFromJsonAsync<JsonObject>())!;
            var wrappedValue = Convert.FromBase64String(body["message"]!.ToString());
            stateDigestAckMessage = Attestation.UnwrapRsaOaepAesKwpValue(
                wrappedValue,
                wsConfig.Attestation.PrivateKey);
        }

        string ackResponse;
        using (HttpRequestMessage request = Cose.CreateHttpRequestMessage(
            $"gov/members/state-digests/{recoveryMemberId}:ack" +
            $"?api-version={this.clientManager.GetGovApiVersion()}",
            stateDigestAckMessage))
        {
            using HttpResponseMessage stateDigestResponse = await ccfClient.SendAsync(request);
            await stateDigestResponse.ValidateStatusCodeAsync(this.logger);
            ackResponse = (await stateDigestResponse.Content.ReadAsStringAsync())!;
        }

        return this.Ok(ackResponse);

        static ODataError? ValidateInput(ActivateRecoveryMemberInput input)
        {
            if (string.IsNullOrEmpty(input.MemberName))
            {
                return new ODataError("InputMissing", "name input is required.");
            }

            if (input.AgentConfig == null)
            {
                return new ODataError("InputMissing", "agentConfig input is required.");
            }

            return null;
        }
    }

    [HttpPost("/members/{memberId}/recoveryMembers/submitRecoveryShare")]
    public async Task<IActionResult> SubmitRecoveryShare(
        [FromRoute] string memberId,
        [FromBody] byte[] content)
    {
        // Verify caller is an active member of the consortium.
        (var err, var payload) = await this.VerifyMemberAuthentication(memberId, content);
        if (err != null)
        {
            return this.BadRequest(err);
        }

        var input = JsonSerializer.Deserialize<SubmitRecoveryShareInput>(payload)!;
        err = ValidateInput(input);
        if (err != null)
        {
            return this.BadRequest(err);
        }

        this.logger.LogInformation(
            $"Requesting recovery service for recovery share for member {input.MemberName}.");

        var wsConfig = await this.clientManager.GetWsConfig();

        // To complete recovery share submission the steps are:
        // - Get encrypted share from CCF
        // - Request recovery service to decrypt and return the recovery_share message
        //   which would then be submitted to CCF to get the state digest.
        // - Submit recovery_share message to CCF.
        var svcClient = await this.GetRecoverySvcClient(input.AgentConfig.RecoveryService);

        var recoveryMember = (await svcClient.GetFromJsonAsync<JsonObject>(
            $"members/{input.MemberName}"))!;
        var signingCert = recoveryMember["signingCert"]!.ToString();
        using var cert = X509Certificate2.CreateFromPem(signingCert);
        var recoveryMemberId = cert.GetCertHashString(HashAlgorithmName.SHA256).ToLower();

        var ccfClient = await this.clientManager.GetCcfClient();
        var encryptedShare = await ccfClient.GetFromJsonAsync<JsonObject>(
            $"/gov/recovery/encrypted-shares/{recoveryMemberId}" +
            $"?api-version={this.clientManager.GetGovApiVersion()}");

        // Get attestation report and encrypted share content to send in the request.
        var dataContent = new JsonObject
        {
            ["memberName"] = input.MemberName,
            ["encryptedShare"] = encryptedShare
        };
        (var data, var signature) =
            this.PrepareSignedData(dataContent, wsConfig.Attestation.PrivateKey);
        var svcContent = Attestation.PrepareSignedDataRequestContent(
            data,
            signature,
            wsConfig.Attestation.PublicKey,
            wsConfig.Attestation.Report);

        byte[] recoveryShareMessage;
        using (var genRecoveryShareResponse = await svcClient.PostAsync(
            $"members/generateRecoveryShareMessage",
            JsonContent.Create(svcContent)))
        {
            await genRecoveryShareResponse.ValidateStatusCodeAsync(this.logger);

            var body = (await genRecoveryShareResponse.Content.ReadFromJsonAsync<JsonObject>())!;
            var wrappedValue = Convert.FromBase64String(body["message"]!.ToString());
            recoveryShareMessage = Attestation.UnwrapRsaOaepAesKwpValue(
                wrappedValue,
                wsConfig.Attestation.PrivateKey);
        }

        JsonObject? recoveryShareResponse;
        using (HttpRequestMessage request = Cose.CreateHttpRequestMessage(
            $"gov/recovery/members/{recoveryMemberId}:recover" +
            $"?api-version={this.clientManager.GetGovApiVersion()}",
            recoveryShareMessage))
        {
            using HttpResponseMessage response = await ccfClient.SendAsync(request);
            await response.ValidateStatusCodeAsync(this.logger);
            await response.WaitGovTransactionCommittedAsync(this.logger, ccfClient);
            recoveryShareResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
        }

        return this.Ok(recoveryShareResponse);

        static ODataError? ValidateInput(SubmitRecoveryShareInput input)
        {
            if (string.IsNullOrEmpty(input.MemberName))
            {
                return new ODataError("InputMissing", "name input is required.");
            }

            if (input.AgentConfig == null)
            {
                return new ODataError("InputMissing", "agentConfig input is required.");
            }

            return null;
        }
    }
}
