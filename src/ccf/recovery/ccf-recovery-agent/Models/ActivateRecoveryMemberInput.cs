// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Controllers;

internal class ActivateRecoveryMemberInput
{
    public AgentConfig AgentConfig { get; set; } = default!;

    public string MemberName { get; set; } = default!;
}