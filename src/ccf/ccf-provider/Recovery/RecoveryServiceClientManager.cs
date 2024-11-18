// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Controllers;
using Microsoft.Extensions.Logging;

namespace CcfProvider;

public class RecoveryServiceClientManager
{
    private readonly ILogger logger;
    private HttpClientManager httpClientManager;

    public RecoveryServiceClientManager(ILogger logger)
    {
        this.logger = logger;
        this.httpClientManager = new(logger);
    }

    public Task<HttpClient> GetClient(RecoveryServiceConfig recoveryService)
    {
        var client = this.httpClientManager.GetOrAddClient(
            recoveryService.Endpoint,
            recoveryService.ServiceCert,
            "recovery-service",
            retryPolicy: HttpRetries.Policies.GetDefaultRetryPolicy(this.logger));
        return Task.FromResult(client);
    }

    private class ServiceClient
    {
        public HttpClient HttpClient { get; set; } = default!;
    }
}
