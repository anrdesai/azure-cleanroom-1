// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Common;

public enum OperationState
{
#pragma warning disable SA1602 // Enumeration items should be documented
    Accepted,
    Succeeded,
    Failed,
    Canceled
#pragma warning restore SA1602 // Enumeration items should be documented
}
