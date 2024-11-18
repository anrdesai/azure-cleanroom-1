// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.CleanRoomSidecar.Identity.CredentialStore;

/// <summary>
/// An Azure Key Vault based secret store.
/// </summary>
internal class KeyVaultSecretStore : ISecretStore
{
    private readonly ILogger logger;
    private readonly string keyVaultUrl;
    private readonly string managedIdentityClientId;
    private readonly TokenCredential accessTokenCredential;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyVaultSecretStore"/> class.
    /// </summary>
    /// <param name="keyVaultUrl">The Key Vault URL.</param>
    /// <param name="managedIdentityClientId">The managed identity client ID.</param>
    /// <param name="logger">The logger.</param>
    public KeyVaultSecretStore(string keyVaultUrl, string managedIdentityClientId, ILogger logger)
    {
        this.keyVaultUrl = keyVaultUrl;
        this.managedIdentityClientId = managedIdentityClientId;
        this.logger = logger;
        this.accessTokenCredential = string.IsNullOrWhiteSpace(
            this.managedIdentityClientId) ?
            new DefaultAzureCredential() :
            new ManagedIdentityCredential(this.managedIdentityClientId);
    }

    /// <inheritdoc/>
    public async Task<X509Certificate2> GetCertificateAsync(string name)
    {
        var certificateClient = new CertificateClient(
            new Uri(this.keyVaultUrl),
            this.accessTokenCredential);

        return await certificateClient.DownloadCertificateAsync(name);
    }

    /// <inheritdoc/>
    public async Task<string> GetSecretAsync(string name)
    {
        var secretClient = new SecretClient(new Uri(this.keyVaultUrl), this.accessTokenCredential);

        KeyVaultSecret secretValue = await secretClient.GetSecretAsync(name);

        return secretValue.Value;
    }
}
