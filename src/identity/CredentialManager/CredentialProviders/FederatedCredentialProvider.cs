// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using Azure.Identity;
using Identity.CredentialManager;
using Microsoft.Azure.CleanRoomSidecar.Identity.Utils;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.CleanRoomSidecar.Identity.CredentialProviders;

/// <summary>
/// A credential provider that manages federated credentials.
/// </summary>
internal class FederatedCredentialProvider : ITokenCredentialProvider
{
    private static HttpClient httpClient = new();
    private readonly ILogger logger;
    private readonly string audience;
    private readonly string subject;
    private readonly string idTokenEndpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="FederatedCredentialProvider"/> class.
    /// </summary>
    /// <param name="idTokenEndpoint">
    /// The endpoint at which the source token can be obtained.
    /// </param>
    /// <param name="subject">The subject claim.</param>
    /// <param name="audience">The audience claim.</param>
    /// <param name="logger">The logger to be used.</param>
    public FederatedCredentialProvider(
        string idTokenEndpoint,
        string subject,
        string audience,
        ILogger logger)
    {
        this.logger = logger;
        this.subject = subject;
        this.audience = audience;
        this.idTokenEndpoint = idTokenEndpoint;
    }

    /// <inheritdoc/>
    public async Task<TokenCredential> GetTokenCredentialAsync(string tenantId, string clientId)
    {
        await Task.CompletedTask;
        return new ClientAssertionCredential(
            tenantId,
            clientId,
            async (cToken) =>
            {
                string url = $"{this.idTokenEndpoint.TrimEnd('/')}/oauth/token?" +
                    $"sub={this.subject}&tenantId={tenantId}&aud={this.audience}";

                this.logger.LogInformation($"Fetching client assertion from '{url}'");
                HttpResponseMessage response = await httpClient.PostAsync(url, null);
                await response.ValidateStatusCodeAsync(this.logger);
                var result = await response.Content.ReadFromJsonAsync<JsonObject>();
                return result?["value"]?.ToString();
            });
    }
}
