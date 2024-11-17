// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LoadBalancerProvider;

namespace CcfProvider;

public class CcfNetworkHealth
{
    public List<NodeHealth> NodeHealth { get; set; } = default!;

    public LoadBalancerHealth LoadBalancerHealth { get; set; } = default!;
}