// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace AttestationClient;

public class AttestationReportKey
{
    public AttestationReportKey(string publicKey, string privateKey, AttestationReport report)
    {
        this.PublicKey = publicKey;
        this.PrivateKey = privateKey;
        this.Report = report;
    }

    // PEM encoded string.
    [JsonPropertyName("publicKey")]
    public string PublicKey { get; } = default!;

    // PEM encoded string.
    [JsonPropertyName("privateKey")]
    public string PrivateKey { get; } = default!;

    [JsonPropertyName("report")]
    public AttestationReport Report { get; } = default!;
}