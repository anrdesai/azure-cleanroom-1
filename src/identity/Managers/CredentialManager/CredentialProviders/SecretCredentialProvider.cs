// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Identity;
using Identity.Configuration;
using Microsoft.Azure.CleanRoomSidecar.Identity.CredentialStore;
using Microsoft.Extensions.Logging;

namespace Identity.CredentialManager.CredentialProviders;

/// <summary>
/// A token credential provider backed by a secret.
/// </summary>
internal class SecretCredentialProvider : ITokenCredentialProvider
{
    private readonly ILogger logger;
    private readonly string secret;
    private readonly SecretStore secretStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretCredentialProvider"/> class.
    /// </summary>
    /// <param name="secretStore">The secret store.</param>
    /// <param name="logger">The logger to be used.</param>
    /// <param name="secret">The secret.</param>
    public SecretCredentialProvider(SecretStore secretStore, ILogger logger, string secret)
    {
        this.secretStore = secretStore;
        this.logger = logger;
        this.secret = secret;
    }

    /// <inheritdoc/>
    public async Task<TokenCredential> GetTokenCredentialAsync(string tenantId, string clientId)
    {
        ISecretStore store = SecretStoreFactory.GetSecretStore(this.secretStore, this.logger);
        string secretValue = await store.GetSecretAsync(this.secret);
        return new ClientSecretCredential(tenantId, clientId, secretValue);
    }
}
