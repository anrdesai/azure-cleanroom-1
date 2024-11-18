// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CgsUI.Models;

public class OidcIssuerViewModel
{
    public bool Enabled { get; set; } = default!;

    public string IssuerUrl { get; set; } = default!;

    public TenantDataInfo TenantData { get; set; } = default!;

    public class TenantDataInfo
    {
        public string TenantId { get; set; } = default!;

        public string IssuerUrl { get; set; } = default!;
    }
}
