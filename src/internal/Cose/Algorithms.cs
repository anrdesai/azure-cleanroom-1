// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CoseUtils;

/// <summary>
/// Values from spec: https://www.rfc-editor.org/rfc/rfc8152#section-8.
/// </summary>
public enum Algorithms
{
#pragma warning disable SA1602 // Enumeration items should be documented
    ES256 = -7,
    ES384 = -35,
    ES512 = -36,
    EdDSA = -8,
#pragma warning restore SA1602 // Enumeration items should be documented
}