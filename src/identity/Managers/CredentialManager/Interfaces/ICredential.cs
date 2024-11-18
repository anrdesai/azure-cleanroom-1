// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Identity.CredentialManager;

/// <summary>
/// Interface abstracting the code from the type of credential.
/// </summary>
/// <typeparam name="TAccessToken">The type of access token.</typeparam>
public interface ICredential<TAccessToken>
{
    /// <summary>
    /// Gets an access token.
    /// </summary>
    /// <param name="scope">The scope for the token.</param>
    /// <param name="tenantId">The tenant Id.</param>
    /// <returns>A <typeparamref name="TAccessToken"/> instance.</returns>
    Task<TAccessToken> GetTokenAsync(
        string scope,
        string tenantId);
}