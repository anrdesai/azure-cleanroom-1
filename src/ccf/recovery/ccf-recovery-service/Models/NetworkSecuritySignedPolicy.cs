// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Controllers;

public class NetworkSecuritySignedPolicy
{
    public string Policy { get; set; } = default!;

    public Dictionary<string, string> Signatures { get; set; } = default!;
}