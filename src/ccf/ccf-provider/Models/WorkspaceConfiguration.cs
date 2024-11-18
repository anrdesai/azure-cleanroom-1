// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CcfProvider;

public class WorkspaceConfiguration
{
    public string SigningCert { get; set; } = default!;

    public string SigningKey { get; set; } = default!;
}