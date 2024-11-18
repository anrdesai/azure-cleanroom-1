// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Controllers;

public class SigningPrivateKeyInfo : SigningKeyInfo
{
    public string SigningKey { get; set; } = default!;
}