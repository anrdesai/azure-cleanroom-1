// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Controllers;

public class NetworkSecurityPolicySigner
{
    public string Certificate { get; set; } = default!;

    public JsonObject SignerData { get; set; } = default!;
}