// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CoseUtils;

public class CoseSignRequest
{
    public string Algorithm { get; set; } = default!;

    public string Certificate { get; set; } = default!;

    public string PrivateKey { get; set; } = default!;

    public Dictionary<string, string>? ProtectedHeaders { get; set; }

    public Dictionary<string, string>? UnprotectedHeaders { get; set; }

    public string? Payload { get; set; }
}