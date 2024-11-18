// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CgsUI.Models;

public class JSAppViewModel
{
    public string Endpoints { get; set; } = default!;

    public List<(string, string)> Modules { get; set; } = default!;
}
