// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;

namespace Identity.CredentialManager;

public interface ITokenCredentialProvider
{
    /// <summary>
    /// Gets a <see cref="TokenCredential"/> that can be used to fetch tokens from AAD.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="clientId">The client ID.</param>
    /// <returns>A <see cref="TokenCredential"/> instance.</returns>
    Task<TokenCredential> GetTokenCredentialAsync(string tenantId, string clientId);
}
