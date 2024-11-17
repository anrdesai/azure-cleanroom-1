// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Identity.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.CleanRoomSidecar.Identity.CredentialStore;

public static class SecretStoreFactory
{
    /// <summary>
    /// Gets the secret store.
    /// </summary>
    /// <param name="secretStore">The secret store configuration.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The secret store instance.</returns>
    public static ISecretStore GetSecretStore(SecretStore secretStore, ILogger logger)
    {
        return secretStore.Type switch
        {
            "AzureKeyVault" => new KeyVaultSecretStore(
                secretStore.Endpoint,
                secretStore.KeyVaultConfiguration.ManagedIdentityClientId,
                logger),

            "CGS" => new CgsSecretStore(secretStore.Endpoint, logger),

            _ => throw new NotSupportedException($"Type: {secretStore.Type} is not supported.")
        };
    }
}
