// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CcfRecoveryProvider;

namespace Controllers;

public class RsGenerateSecurityPolicyInput
{
    public string InfraType { get; set; } = default!;

    public NetworkJoinPolicy CcfNetworkJoinPolicy { get; set; } = default!;

    public string? SecurityPolicyCreationOption { get; set; } = default!;
}