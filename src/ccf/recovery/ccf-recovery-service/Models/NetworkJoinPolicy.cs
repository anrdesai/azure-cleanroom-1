// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Controllers;

public class NetworkJoinPolicy
{
    [JsonPropertyName("snp")]
    public SnpSection Snp { get; set; } = default!;

    public class SnpSection
    {
        [JsonPropertyName("hostData")]
        public List<string> HostData { get; set; } = default!;
    }
}
