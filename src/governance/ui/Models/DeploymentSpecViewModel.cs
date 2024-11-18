// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace CgsUI.Models;

public class DeploymentSpecViewModel
{
    public string[] ProposalIds { get; set; } = default!;

    public JsonObject Data { get; set; } = default!;
}