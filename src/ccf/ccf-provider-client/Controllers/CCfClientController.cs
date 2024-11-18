// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CAciCcfProvider;
using CcfProvider;
using CcfRecoveryProvider;
using Microsoft.AspNetCore.Mvc;
using VirtualCcfProvider;

namespace Controllers;

public abstract class CCfClientController : ControllerBase
{
    private readonly ILogger logger;
    private readonly IConfiguration configuration;

    public CCfClientController(
        ILogger logger,
        IConfiguration configuration)
    {
        this.logger = logger;
        this.configuration = configuration;
    }

    protected ICcfNodeProvider GetNodeProvider(InfraType infraType)
    {
        switch (infraType)
        {
            case InfraType.@virtual:
                return new DockerNodeProvider(this.logger, this.configuration);
            case InfraType.virtualaci:
                return new AciNodeProvider(this.logger, this.configuration);
            case InfraType.caci:
                return new CAciNodeProvider(this.logger, this.configuration);
            default:
                throw new NotSupportedException($"Infra type '{infraType}' is not supported.");
        }
    }

    protected ICcfRecoveryServiceInstanceProvider GetRecoverySvcInstanceProvider(
        RsInfraType infraType)
    {
        switch (infraType)
        {
            case RsInfraType.@virtual:
                return new DockerRecoveryServiceInstanceProvider(this.logger, this.configuration);
            case RsInfraType.caci:
                return new CAciRecoveryServiceInstanceProvider(this.logger, this.configuration);
            default:
                throw new NotSupportedException($"Infra type '{infraType}' is not supported.");
        }
    }

    protected CcfRecoveryServiceProvider GetRecoveryServiceProvider(
        string infraType)
    {
        var type = Enum.Parse<RsInfraType>(infraType, ignoreCase: true);
        ICcfRecoveryServiceInstanceProvider provider = this.GetRecoverySvcInstanceProvider(type);
        var ccfRecoverySvcProvider = new CcfRecoveryServiceProvider(
            this.logger,
            provider);
        return ccfRecoverySvcProvider;
    }
}
