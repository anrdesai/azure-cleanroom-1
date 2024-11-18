// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CcfProvider;

public class NetworkNodeList
{
    [JsonPropertyName("nodes")]
    public List<NetworkNode> Nodes { get; set; } = default!;
}