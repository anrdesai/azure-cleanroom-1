// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CoseUtils;

public class GovMessageType
{
    public static readonly GovMessageType Proposal = new("proposal");

    public static readonly GovMessageType Ack = new("ack");

    public static readonly GovMessageType StateDigest = new("state_digest");

    public static readonly GovMessageType Ballot = new("ballot");

    public static readonly GovMessageType RecoveryShare = new("recovery_share");

    public static readonly GovMessageType Withdrawal = new("withdrawal");

    private GovMessageType(string value)
    {
        this.Value = value;
    }

    public string Value { get; }
}
