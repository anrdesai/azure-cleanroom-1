// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace CcfProvider;

public class NodeStorageProviderFactory
{
    public static INodeStorageProvider Create(
        string networkName,
        JsonObject? providerConfig,
        InfraType infraType,
        ILogger logger)
    {
        var nodeStorageType = providerConfig.GetNodeStorageType(infraType);
        switch (nodeStorageType)
        {
            case NodeStorageType.LocalFs:
                return new LocalFsNodeStorageProvider(
                    networkName,
                    providerConfig,
                    infraType,
                    logger);
            case NodeStorageType.DockerHostFs:
                return new DockerHostFsNodeStorageProvider(
                    networkName,
                    providerConfig,
                    infraType,
                    logger);
            case NodeStorageType.AzureFiles:
                return new AzureFilesNodeStorageProvider(
                    networkName,
                    providerConfig,
                    infraType,
                    logger);
            default:
                throw new NotSupportedException($"{infraType} is not supported. Fix this.");
        }
    }
}