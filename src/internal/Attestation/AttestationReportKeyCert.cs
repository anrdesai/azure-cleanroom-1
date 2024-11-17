// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace AttestationClient;

public class AttestationReportKeyCert : AttestationReportKey
{
    public AttestationReportKeyCert(
        string certificate,
        string publicKey,
        string privateKey,
        AttestationReport report)
        : base(publicKey, privateKey, report)
    {
        this.Certificate = certificate;
    }

    // PEM encoded string.
    [JsonPropertyName("certificate")]
    public string Certificate { get; } = default!;
}