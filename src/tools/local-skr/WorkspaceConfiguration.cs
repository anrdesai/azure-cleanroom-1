// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Controllers;

public class WorkspaceConfiguration
{
    public string PrivateKey { get; set; } = default!;

    public string PublicKey { get; set; } = default!;

    public JsonObject MaaRequest { get; set; } = default!;
}