// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CcfProvider;

public static class WorkspaceDirectories
{
    public static string GetNetworkDirectory(string networkName, InfraType infraType)
    {
        var infraTypeFolderName = infraType.ToString().ToLower();
        string wsDir =
            Environment.GetEnvironmentVariable("WORKSPACE_DIR") ?? Directory.GetCurrentDirectory();
        return wsDir + $"/{infraTypeFolderName}/{networkName}";
    }

    public static string GetNodeDirectory(
        string nodeName,
        string networkName,
        InfraType infraType)
    {
        return GetNetworkDirectory(networkName, infraType) + $"/{nodeName}";
    }

    public static string GetConfigurationDirectory(
        string nodeName,
        string networkName,
        InfraType infraType)
    {
        return GetNodeDirectory(nodeName, networkName, infraType) + $"/config_data";
    }
}
