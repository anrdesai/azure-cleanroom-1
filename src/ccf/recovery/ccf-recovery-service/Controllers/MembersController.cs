// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json.Nodes;
using AttestationClient;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
public class MembersController : BaseController
{
    private readonly ILogger logger;
    private readonly IConfiguration configuration;
    private readonly CcfRecoveryService service;
    private readonly IPolicyStore policyStore;

    public MembersController(
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

    [HttpGet("/members")]
    public async Task<IActionResult> GetMembers()
    {
        // This API currently requires no attestation report input. Any client
        // can query for a member's public information.
        List<string> members = await this.service.GetMembers();
        return this.Ok(members);
    }

    [HttpGet("/members/{memberName}")]
    public async Task<IActionResult> GetMember([FromRoute] string memberName)
    {
        // This API currently requires no attestation report input. Any client
        // can query for a member's public information.
        RecoveryMember? member = await this.service.GetMember(memberName);
        if (member != null)
        {
            return this.Ok(member);
        }

        return this.NotFound(
            new ODataError("MemberNotFound", $"Member {memberName} was not found."));
    }

    [HttpGet("/members/{memberName}/report")]
    public async Task<IActionResult> GetMemberReport([FromRoute] string memberName)
    {
        // This API currently requires no attestation report input. Any client
        // can query for a member's public information.
        RecoveryMemberReport? report = await this.service.GetMemberReport(memberName);
        if (report != null)
        {
            return this.Ok(report);
        }

        return this.NotFound(
            new ODataError("MemberNotFound", $"Member {memberName} was not found."));
    }

    [HttpPost("/members/generate")]
    public async Task<IActionResult> GenerateMember(
        [FromBody] JsonObject content)
    {
        var reportInfo = await this.VerifyAttestationReport(content);
        string publicKey = reportInfo.PublicKey;

        // Extract member name from the signed incoming payload.
        var data = this.GetSignedData<JsonObject>(publicKey, content);
        string? memberName = data["memberName"]?.ToString();
        if (string.IsNullOrEmpty(memberName))
        {
            throw new ApiException(
                HttpStatusCode.BadRequest,
                "MemberNameMissing",
                "memberName input must be supplied.");
        }

        RecoveryMember member = await this.service.GenerateMember(memberName);
        return this.Ok(member);
    }

    [HttpPost("/members/generateStateDigestMessage")]
    public async Task<IActionResult> GenerateStateDigestMessage(
        [FromBody] JsonObject content)
    {
        var reportInfo = await this.VerifyAttestationReport(content);
        string publicKey = reportInfo.PublicKey;

        // Extract member name from the signed incoming payload.
        var data = this.GetSignedData<JsonObject>(publicKey, content);
        string? memberName = data["memberName"]?.ToString();
        if (string.IsNullOrEmpty(memberName))
        {
            throw new ApiException(
                HttpStatusCode.BadRequest,
                "MemberNameMissing",
                "memberName input must be supplied.");
        }

        byte[] coseSigedMessage = await this.service.GenerateStateDigestMessage(memberName);

        // Send response encrypted with the incoming public key.
        var wrappedValue = Attestation.WrapRsaOaepAesKwpValue(coseSigedMessage, publicKey);
        var result = new JsonObject
        {
            ["message"] = Convert.ToBase64String(wrappedValue)
        };

        return this.Ok(result);
    }

    [HttpPost("/members/generateStateDigestAckMessage")]
    public async Task<IActionResult> GenerateStateDigestAckMessage(
        [FromBody] JsonObject content)
    {
        var reportInfo = await this.VerifyAttestationReport(content);
        string publicKey = reportInfo.PublicKey;

        // Extract member name from the signed incoming payload.
        var data = this.GetSignedData<JsonObject>(publicKey, content);
        string? memberName = data["memberName"]?.ToString();
        if (string.IsNullOrEmpty(memberName))
        {
            throw new ApiException(
                HttpStatusCode.BadRequest,
                "MemberNameMissing",
                "memberName input must be supplied.");
        }

        var stateDigest = data["stateDigest"]?.AsObject();
        if (stateDigest == null)
        {
            throw new ApiException(
                HttpStatusCode.BadRequest,
                "StateDigestMissing",
                "stateDigest input must be supplied.");
        }

        byte[] coseSigedMessage = await this.service.GenerateStateDigestAckMessage(
            memberName,
            stateDigest);
        var wrappedValue = Attestation.WrapRsaOaepAesKwpValue(coseSigedMessage, publicKey);
        var result = new JsonObject
        {
            ["message"] = Convert.ToBase64String(wrappedValue)
        };

        return this.Ok(result);
    }

    [HttpPost("/members/generateRecoveryShareMessage")]
    public async Task<IActionResult> GenerateRecoveryShareMessage(
        [FromBody] JsonObject content)
    {
        var reportInfo = await this.VerifyAttestationReport(content);
        string publicKey = reportInfo.PublicKey;

        // Extract member name from the signed incoming payload.
        var data = this.GetSignedData<JsonObject>(publicKey, content);
        string? memberName = data["memberName"]?.ToString();
        if (string.IsNullOrEmpty(memberName))
        {
            throw new ApiException(
                HttpStatusCode.BadRequest,
                "MemberNameMissing",
                "memberName input must be supplied.");
        }

        var encryptedShare = data["encryptedShare"]?.AsObject();
        if (encryptedShare == null)
        {
            throw new ApiException(
                HttpStatusCode.BadRequest,
                "EncryptedShareMissing",
                "encryptedShare input must be supplied.");
        }

        byte[] coseSigedMessage = await this.service.GenerateRecoveryShareMessage(
            memberName,
            encryptedShare);
        var wrappedValue = Attestation.WrapRsaOaepAesKwpValue(coseSigedMessage, publicKey);
        var result = new JsonObject
        {
            ["message"] = Convert.ToBase64String(wrappedValue)
        };

        return this.Ok(result);
    }
}
