// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Identity.Configuration;
using Microsoft.Extensions.Logging;
using azAd = Azure.Identity;

namespace Identity.CredentialManager;

/// <summary>
/// Class that represents a Managed Identity Credential in Azure AD.
/// </summary>
public class ManagedIdentityCredential : ICredential<AccessToken>
{
    private readonly ManagedIdentity managedIdentity;
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedIdentityCredential"/> class.
    /// </summary>
    /// <param name="managedIdentity">The managed identity.</param>
    /// <param name="logger">The logger.</param>
    public ManagedIdentityCredential(
        ManagedIdentity managedIdentity,
        ILogger logger)
    {
        this.managedIdentity = managedIdentity;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AccessToken> GetTokenAsync(string scope, string tenantId)
    {
        this.logger.LogTrace($"Fetching token for scope: {scope} and tenant: " +
            $"{tenantId} using client ID: {this.managedIdentity.ClientId}.");

        return await new azAd.ManagedIdentityCredential(this.managedIdentity.ClientId)
            .GetTokenAsync(
            new TokenRequestContext(
                scopes: scope.FormatScope(),
                tenantId: tenantId));
    }
}