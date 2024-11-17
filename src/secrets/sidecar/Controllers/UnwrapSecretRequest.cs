// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Controllers;

public class UnwrapSecretRequest
{
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = default!;

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = default!;

    [JsonPropertyName("kid")]
    public string Kid { get; set; } = default!;

    [JsonPropertyName("akvEndpoint")]
    public string AkvEndpoint { get; set; } = default!;

    [JsonPropertyName("kek")]
    public KekInfo Kek { get; set; } = default!;
}

public class KekInfo
{
    [JsonPropertyName("kid")]
    public string Kid { get; set; } = default!;

    [JsonPropertyName("akvEndpoint")]
    public string AkvEndpoint { get; set; } = default!;

    [JsonPropertyName("maaEndpoint")]
    public string MaaEndpoint { get; set; } = default!;
}