// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Azure.Core;
using Azure.Identity;
using Constants;
using Microsoft.Extensions.Logging;
using OpenTelemetry;

namespace Identity.CredentialManager;

/// <summary>
/// Class that represents the Default Azure Credential in Azure AD.
/// </summary>
public class DefaultCredential : ICredential<AccessToken>
{
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCredential" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DefaultCredential(ILogger logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<AccessToken> GetTokenAsync(string scope, string tenantId)
    {
        this.logger.LogInformation(
            $"Fetching token for scope: {scope} and tenant: " +
            $"{tenantId} using default credential.");

        return await new DefaultAzureCredential().GetTokenAsync(
            new TokenRequestContext(scopes: scope.FormatScope(), tenantId: tenantId));
    }
}