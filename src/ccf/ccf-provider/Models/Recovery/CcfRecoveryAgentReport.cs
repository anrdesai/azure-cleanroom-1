// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace CcfProvider;

public class CcfRecoveryAgentReport
{
    public string Name { get; set; } = default!;

    public string Endpoint { get; set; } = default!;

    public JsonObject Report { get; set; } = default!;

    public bool Verified { get; set; }
}