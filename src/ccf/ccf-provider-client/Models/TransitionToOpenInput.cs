// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Controllers;

public class TransitionToOpenInput
{
    public string InfraType { get; set; } = default!;

    // PEM encoded certificate string.
    public string? PreviousServiceCertificate { get; set; } = default!;

    public JsonObject? ProviderConfig { get; set; }
}