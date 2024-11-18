// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace Controllers;

public class AllowAllPolicyStore : IPolicyStore
{
    private readonly NetworkSecurityPolicy securityPolicy;
    private readonly NetworkJoinPolicy joinPolicy;

    public AllowAllPolicyStore(ILogger logger)
    {
        this.securityPolicy = new NetworkSecurityPolicy();
        this.joinPolicy = new NetworkJoinPolicy
        {
            Snp = new()
            {
                HostData = ["73973b78d70cc68353426de188db5dfc57e5b766e399935fb73a61127ea26d20"]
            }
        };

        logger.LogInformation($"NetworkJoinPolicy: {JsonSerializer.Serialize(this.joinPolicy)}");
    }

    public Task SetNetworkJoinPolicy(NetworkJoinPolicy joinPolicy)
    {
        throw new NotSupportedException("SetNetworkJoinPolicy is not supported when using allow " +
            "all policy.");
    }

    public Task<NetworkJoinPolicy> GetNetworkJoinPolicy()
    {
        return Task.FromResult(this.joinPolicy);
    }

    public Task<NetworkSecurityPolicy> GetSecurityPolicy()
    {
        return Task.FromResult(this.securityPolicy);
    }
}
