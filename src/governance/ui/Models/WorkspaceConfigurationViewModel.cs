// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CgsUI.Models;

public class WorkspaceConfigurationViewModel
{
    public IFormFile SigningCertPemFile { get; set; } = default!;

    public IFormFile SigningKeyPemFile { get; set; } = default!;

    public IFormFile? ServiceCertPemFile { get; set; } = default!;

    public string CcfEndpoint { get; set; } = default!;
}
