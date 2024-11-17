// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.CleanRoomSidecar.Identity.CredentialStore;

/// <summary>
/// Interface to represent a secret store.
/// </summary>
public interface ISecretStore
{
    /// <summary>
    /// Gets a secret from the underlying secret store.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <returns>The value of the secret.</returns>
    Task<string> GetSecretAsync(string name);

    /// <summary>
    /// Gets a certificate from the underlying secret store.
    /// </summary>
    /// <param name="name">The certificate name.</param>
    /// <returns>The certificate.</returns>
    Task<X509Certificate2> GetCertificateAsync(string name);
}
