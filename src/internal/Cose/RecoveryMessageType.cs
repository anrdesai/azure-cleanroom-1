// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CoseUtils;

public class RecoveryMessageType
{
    public static readonly RecoveryMessageType GenerateMember = new("generate_member");

    public static readonly RecoveryMessageType ActivateMember = new("activate_member");

    public static readonly RecoveryMessageType RecoveryShare = new("recovery_share");

    public static readonly RecoveryMessageType SetNetworkJoinPolicy = new("set_network_join_policy");

    private RecoveryMessageType(string value)
    {
        this.Value = value;
    }

    public string Value { get; }
}
