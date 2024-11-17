// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CcfProvider;

namespace Controllers;

public class RecoveryServiceApiInput
{
    public RecoveryServiceConfig RecoveryService { get; set; } = default!;
}