// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Controllers;

public class SigningConfiguration
{
    public string SigningCert { get; set; } = default!;

    public string SigningKey { get; set; } = default!;

    public string MemberId { get; set; } = default!;
}