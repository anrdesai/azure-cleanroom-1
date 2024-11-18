// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Controllers;

public interface IPolicyStore
{
    Task<NetworkJoinPolicy> GetNetworkJoinPolicy();

    Task SetNetworkJoinPolicy(NetworkJoinPolicy joinPolicy);

    Task<NetworkSecurityPolicy> GetSecurityPolicy();
}
