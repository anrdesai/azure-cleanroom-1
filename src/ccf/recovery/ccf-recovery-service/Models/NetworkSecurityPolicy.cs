// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Controllers;

public class NetworkSecurityPolicy
{
    public List<NetworkSecurityPolicySigner> Signers { get; set; } = default!;

    public NetworkSecuritySignedPolicy SignedPolicy { get; set; } = default!;
}