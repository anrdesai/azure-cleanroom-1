// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Security.Cryptography.Cose;
using System.Text;
using System.Text.Json.Nodes;
using AttestationClient;
using CoseUtils;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

public abstract class BaseController : ControllerBase
{
    private readonly ILogger logger;
    private readonly IConfiguration configuration;
    private readonly ClientManager clientManager;

    public BaseController(
        ILogger logger,
        IConfiguration configuration,
        ClientManager clientManager)
    {
        this.logger = logger;
        this.configuration = configuration;
        this.clientManager = clientManager;
    }

    protected Task<HttpClient> GetRecoverySvcClient(RecoveryServiceConfig? recoveryService)
    {
        return this.clientManager.GetRecoverySvcClient(
            recoveryService?.Endpoint,
            recoveryService?.ServiceCert);
    }

    protected async Task<(ODataError? err, string body)> VerifyMemberAuthentication(
        string memberId,
        byte[] content)
    {
        string body = string.Empty;
        var ccfClient = await this.clientManager.GetCcfClient();
        var members = (await ccfClient.GetFromJsonAsync<Dictionary<string, Ccf.MemberInfo>>(
            "gov/members"))!;
        if (!members.TryGetValue(memberId, out var member))
        {
            return (
                new ODataError("MemberIdNotFound", $"Member with ID '{memberId}' was not found."),
                body);
        }

        if (member.Status != "Active")
        {
            return (
                new ODataError("MemberNotActive", $"Member with ID '{memberId}' is not active."),
                body);
        }

        CoseSign1Message sign1Message = CoseMessage.DecodeSign1(content);
        if (!Cose.Verify(sign1Message, member.Cert))
        {
            return (
                new ODataError("SignatureVerificationFailed", "Payload verification failed."),
                body);
        }

        body = Encoding.UTF8.GetString(sign1Message.Content!.Value.Span);
        return (null, body);
    }

    protected (byte[] dataBytes, byte[] signature) PrepareSignedData(
        JsonObject data,
        string privateKey)
    {
        var paddingMode = RSASignaturePaddingMode.Pss;
        var dataBytes = Encoding.UTF8.GetBytes(data.ToJsonString());
        var signature = Signing.SignData(dataBytes, privateKey, paddingMode);
        return (dataBytes, signature);
    }
}
