// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CcfProvider;

public class AgentRequest
{
    public string MemberName { get; set; } = default!;

    public AgentConfig? AgentConfig { get; set; } = default!;
}