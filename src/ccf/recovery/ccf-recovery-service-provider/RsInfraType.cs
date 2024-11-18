// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CcfRecoveryProvider;

#pragma warning disable SA1300 // Element should begin with upper-case letter
public enum RsInfraType
{
    /// <summary>
    /// Recovery service is started in Confidential ACI instances ie in an SEV-SNP environment.
    /// Meant for production.
    /// </summary>
    caci,

    /// <summary>
    /// Recovery service is started in Docker containers. Meant for local dev/test.
    /// </summary>
    @virtual,
}
#pragma warning restore SA1300 // Element should begin with upper-case letter
