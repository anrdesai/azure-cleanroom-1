// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace CgsUI.Models;

public class SettingsViewModel
{
    public bool Connected { get; set; }

    public bool Configured { get; set; }

    public string CcfEndpoint { get; set; } = default!;

    public string SigningCert { get; set; } = default!;

    public string MemberId { get; set; } = default!;

    public string? Name
    {
        get
        {
            return this.MemberData?["identifier"]?.ToString() ?? this.MemberId;
        }
    }

    public JsonObject MemberData { get; set; } = default!;

    public UpdatesViewModel Updates { get; set; } = default!;
}
