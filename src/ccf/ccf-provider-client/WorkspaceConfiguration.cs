// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Controllers;

public class WorkspaceConfiguration
{
    public string SigningCert { get; set; } = default!;

    public string SigningKey { get; set; } = default!;

    public System.Collections.IDictionary EnvironmentVariables { get; set; } = default!;
}