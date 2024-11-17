// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Controllers;

public class ConfidentialRecoveryInput
{
    public string MemberName { get; set; } = default!;

    public string RecoveryServiceName { get; set; } = default!;
}