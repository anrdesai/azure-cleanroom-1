// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using CcfProvider;
using Controllers;

namespace CcfProviderClient;

internal class Startup : ApiStartup
{
    public Startup(IConfiguration config)
        : base(config, Assembly.GetExecutingAssembly().GetName().Name!)
    {
    }

    public override void OnConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<CcfClientManager>();
        services.AddSingleton<RecoveryAgentClientManager>();
        services.AddSingleton<RecoveryServiceClientManager>();
    }
}