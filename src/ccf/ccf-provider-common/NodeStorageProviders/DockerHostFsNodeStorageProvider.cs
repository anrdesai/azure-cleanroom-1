// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.ResourceManager.ContainerInstance;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace CcfProvider;

internal class DockerHostFsNodeStorageProvider : INodeStorageProvider
{
    private const string DockerReadOnlyStorageMountPath = "/mnt/ro-storage";
    private const string DockerNodeStorageMountPath = "/mnt/storage";
    private readonly string networkName;
    private readonly JsonObject? providerConfig;
    private readonly InfraType infraType;
    private readonly ILogger logger;

    public DockerHostFsNodeStorageProvider(
        string networkName,
        JsonObject? providerConfig,
        InfraType infraType,
        ILogger logger)
    {
        this.networkName = networkName;
        this.providerConfig = providerConfig;
        this.infraType = infraType;
        this.logger = logger;
    }

    public NodeStorageType GetNodeStorageType()
    {
        return NodeStorageType.DockerHostFs;
    }

    public Task<bool> NodeStorageDirectoryExists(string nodeName)
    {
        var nodeWorkspaceDir = WorkspaceDirectories.GetNodeDirectory(
            nodeName,
            this.networkName,
            this.infraType);
        string storageFolder = "/storage";
        var nodeStorageDir = nodeWorkspaceDir + storageFolder;
        return Task.FromResult(Directory.Exists(nodeStorageDir));
    }

    public Task CreateNodeStorageDirectory(string nodeName)
    {
        var nodeWorkspaceDir = WorkspaceDirectories.GetNodeDirectory(
            nodeName,
            this.networkName,
            this.infraType);
        string storageFolder = "/storage";
        var nodeStorageDir = nodeWorkspaceDir + storageFolder;
        Directory.CreateDirectory(nodeStorageDir);
        return Task.CompletedTask;
    }

    public Task DeleteNodeStorageDirectory(string nodeName)
    {
        var nodeWorkspaceDir = WorkspaceDirectories.GetNodeDirectory(
            nodeName,
            this.networkName,
            this.infraType);
        string storageFolder = "/storage";
        var nodeStorageDir = nodeWorkspaceDir + storageFolder;
        this.logger.LogWarning($"Removing {nodeStorageDir} folder.");
        Directory.Delete(nodeStorageDir, recursive: true);
        return Task.CompletedTask;
    }

    public Task DeleteUncommittedLedgerFiles(string nodeName)
    {
        var nodes = new List<string>();
        var networkDir =
            WorkspaceDirectories.GetNetworkDirectory(this.networkName, this.infraType);
        var nodeDirs = Directory.EnumerateDirectories(
            networkDir,
            $"{this.networkName}-*",
            SearchOption.TopDirectoryOnly);
        foreach (string nodeDir in nodeDirs)
        {
            var dirInfo = new DirectoryInfo(nodeDir);
            if (dirInfo.Name != nodeName)
            {
                continue;
            }

            var ledgerDir = nodeDir + "/storage/ledger";
            if (Directory.Exists(ledgerDir))
            {
                this.logger.LogInformation(
                    $"Inspecting folder '{ledgerDir}' for uncommitted ledger files.");
                var latestLedgers =
                    Directory.GetFiles(ledgerDir, "ledger_*")
                    .OrderBy(file =>
                        Path.GetFileName(file).PadForNaturalNumberOrdering());
                foreach (var ledger in latestLedgers)
                {
                    // Avoid .committed files.
                    if (!Path.GetFileName(ledger).Contains('.'))
                    {
                        this.logger.LogInformation(
                            $"Deleting uncommitted ledger file {ledger} on node '{nodeName}'.");
                        File.Delete(ledger);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<(IDirectory rwLedgerDir, IDirectory rwSnapshotsDir, IDirectory? logsDir)>
    GetReadWriteLedgerSnapshotsDir(string nodeName)
    {
        var storageWsPath = WorkspaceDirectories.GetNodeDirectory(
                nodeName,
                this.networkName,
                this.infraType) + "/storage";
        IDirectory rwLedgerDir = new DockerDirectory
        {
            MountPath = $"{DockerNodeStorageMountPath}/ledger",
            WorkspacePath = storageWsPath + "/ledger"
        };

        IDirectory rwSnapshotsDir = new DockerDirectory
        {
            MountPath = $"{DockerNodeStorageMountPath}/snapshots",
            WorkspacePath = storageWsPath + "/snapshots"
        };

        return Task.FromResult((rwLedgerDir, rwSnapshotsDir, (IDirectory?)null));
    }

    public Task<(List<IDirectory> roLedgerDirs, IDirectory? roSnapshotsDir)>
        GetReadonlyLedgerSnapshotsDir()
    {
        return GetReadonlyLedgerSnapshotsDir(this.networkName, this.infraType, this.logger);
    }

    public Task<(List<IDirectory> roLedgerDirs, IDirectory? roSnapshotsDir)>
        GetReadonlyLedgerSnapshotsDirForNetwork(string targetNetworkName)
    {
        return GetReadonlyLedgerSnapshotsDir(targetNetworkName, this.infraType, this.logger);
    }

    public Task CopyLatestSnapshot(IDirectory? roSnapshotsDir, IDirectory rwSnapshotsDir)
    {
        if (roSnapshotsDir == null)
        {
            return Task.CompletedTask;
        }

        var roSnapDir = (DockerDirectory)roSnapshotsDir;
        var rwSnapDir = (DockerDirectory)rwSnapshotsDir;

        if (roSnapDir.WorkspacePath == rwSnapDir.WorkspacePath)
        {
            return Task.CompletedTask;
        }

        var latestNodeSnapshot = Directory.GetFiles(roSnapDir.WorkspacePath, "snapshot_*.committed")
            .OrderBy(file =>
                Path.GetFileName(file).PadForNaturalNumberOrdering())
            .LastOrDefault();
        if (latestNodeSnapshot != null)
        {
            if (!Directory.Exists(rwSnapDir.WorkspacePath))
            {
                this.logger.LogInformation($"Creating destination directory " +
                    $"{rwSnapDir.WorkspacePath} for copying snapshot.");
                Directory.CreateDirectory(rwSnapDir.WorkspacePath);
            }

            string fileName = Path.GetFileName(latestNodeSnapshot);
            string dstFilePath = rwSnapDir.WorkspacePath + $"/{fileName}";
            this.logger.LogInformation($"Copying {latestNodeSnapshot} to {dstFilePath}.");
            Stopwatch timeTaken = Stopwatch.StartNew();
            File.Copy(latestNodeSnapshot, dstFilePath, overwrite: true);
            timeTaken.Stop();
            this.logger.LogInformation($"Copy finished in {timeTaken.ElapsedMilliseconds}ms: " +
                $"{latestNodeSnapshot} to {dstFilePath}.");
        }

        return Task.CompletedTask;
    }

    public Task<List<string>> GetNodesWithLedgers()
    {
        var nodes = GetNodesWithLedgers(this.networkName, this.infraType, this.logger);
        return Task.FromResult(nodes);
    }

    public Task AddNodeStorageProviderConfiguration(
        string nodeConfigDataDir,
        IDirectory rwLedgerDir,
        IDirectory rwSnapshotsDir,
        List<IDirectory>? roLedgerDirs,
        IDirectory? roSnapshotsDir)
    {
        return Task.CompletedTask;
    }

    public Task UpdateCreateContainerParams(
        string nodeName,
        List<IDirectory>? roLedgerDirs,
        IDirectory? roSnapshotsDir,
        CreateContainerParameters createContainerParams)
    {
        var readonlyHostSnapshotDir = (DockerDirectory?)roSnapshotsDir;
        var hostNodeStorageDir =
            GetNodeStorageDirectory(this.networkName, this.infraType, nodeName);
        var binds = createContainerParams.HostConfig.Binds;
        binds.Add($"{hostNodeStorageDir}:{DockerNodeStorageMountPath}");

        if (roLedgerDirs != null)
        {
            foreach (var roLedgerDir in roLedgerDirs)
            {
                var readonlyHostLedgerDir = (DockerDirectory)roLedgerDir;
                binds.Add($"{readonlyHostLedgerDir.HostPath}:{readonlyHostLedgerDir.MountPath}:ro");
            }
        }

        if (readonlyHostSnapshotDir != null)
        {
            binds.Add($"{readonlyHostSnapshotDir.HostPath}:{readonlyHostSnapshotDir.MountPath}:ro");
        }

        return Task.CompletedTask;
    }

    public Task UpdateCreateContainerGroupParams(
        IDirectory rwLedgerDir,
        IDirectory rwSnapshotsDir,
        List<IDirectory>? roLedgerDirs,
        IDirectory? roSnapshotsDir,
        ContainerGroupData createContainerParams)
    {
        throw new NotImplementedException(
            "UpdateCreateContainerGroupParams not supported for DockerHostFs.");
    }

    private static Task<(List<IDirectory> roLedgerDirs, IDirectory? roSnapshotsDir)>
        GetReadonlyLedgerSnapshotsDir(string networkName, InfraType infraType, ILogger logger)
    {
        IDirectory? roLedgerDir = null, roSnapshotsDir = null;

        string? roHostSnapshotsDir = null;
        string roHostLedgerDir;
        string? latestSnapshotNodeName;
        latestSnapshotNodeName = FindNodeWithLatestSnapshot();
        if (latestSnapshotNodeName != null)
        {
            var hostStorageDir =
                GetNodeStorageDirectory(networkName, infraType, latestSnapshotNodeName);
            roHostSnapshotsDir = hostStorageDir + "/snapshots";
            var roWsSnapshotsDir = WorkspaceDirectories.GetNodeDirectory(
                latestSnapshotNodeName,
                networkName,
                infraType) + "/storage/snapshots";
            roSnapshotsDir = new DockerDirectory
            {
                HostPath = roHostSnapshotsDir,
                WorkspacePath = roWsSnapshotsDir,
                MountPath = $"{DockerReadOnlyStorageMountPath}/snapshots",
            };
        }

        List<IDirectory> roLedgerDirs = new();
        var nodes = GetNodesWithLedgers(networkName, infraType, logger);
        int index = 0;
        foreach (var node in nodes)
        {
            string hostNodeStorageDir = GetNodeStorageDirectory(networkName, infraType, node);
            roHostLedgerDir = hostNodeStorageDir + "/ledger";
            roLedgerDir = new DockerDirectory
            {
                HostPath = roHostLedgerDir,
                MountPath = $"{DockerReadOnlyStorageMountPath}/ledger-{index}"
            };
            roLedgerDirs.Add(roLedgerDir);
            index++;
        }

        return Task.FromResult((roLedgerDirs, roSnapshotsDir));

        string? FindNodeWithLatestSnapshot()
        {
            var networkDir =
                WorkspaceDirectories.GetNetworkDirectory(networkName, infraType);
            var nodeDirs = Directory.EnumerateDirectories(
                networkDir,
                $"{networkName}-*",
                SearchOption.TopDirectoryOnly);
            string? latestNodeName = null;
            string? latestSnapshot = null;
            int latestSnapshotSeqNo = 0;
            foreach (string nodeDir in nodeDirs)
            {
                var snapshotsDir = nodeDir + "/storage/snapshots";
                if (Directory.Exists(snapshotsDir))
                {
                    var latestNodeSnapshot =
                        Directory.GetFiles(snapshotsDir, "snapshot_*.committed")
                        .OrderBy(file =>
                            Path.GetFileName(file).PadForNaturalNumberOrdering())
                        .LastOrDefault();
                    if (latestNodeSnapshot == null)
                    {
                        continue;
                    }

                    var nodeName = new DirectoryInfo(nodeDir).Name;
                    logger.LogInformation(
                        $"Latest snapshot on node '{nodeName}' is: {latestNodeSnapshot}.");
                    int latestNodeSnapshostSeqNo =
                        int.Parse(latestNodeSnapshot.Split("_")[1]);
                    if (latestNodeSnapshostSeqNo > latestSnapshotSeqNo)
                    {
                        logger.LogInformation(
                            $"Snapshot '{latestNodeSnapshot}' on node '{nodeName}' is the " +
                            $"latest seen till now. Previous latest was " +
                            $"'{latestSnapshot}' on node '{latestNodeName}'.");
                        latestSnapshot = latestNodeSnapshot;
                        latestSnapshotSeqNo = latestNodeSnapshostSeqNo;
                        latestNodeName = nodeName;
                    }
                }
            }

            if (latestNodeName != null)
            {
                logger.LogInformation(
                    $"Located latest snapshot with seq no {latestSnapshotSeqNo} on node " +
                    $"'{latestNodeName}': '{latestSnapshot}'.");
            }

            return latestNodeName;
        }
    }

    private static List<string> GetNodesWithLedgers(
        string networkName,
        InfraType infraType,
        ILogger logger)
    {
        var nodes = new List<string>();
        var networkDir =
            WorkspaceDirectories.GetNetworkDirectory(networkName, infraType);
        var nodeDirs = Directory.EnumerateDirectories(
            networkDir,
            $"{networkName}-*",
            SearchOption.TopDirectoryOnly);
        foreach (string nodeDir in nodeDirs)
        {
            var ledgerDir = nodeDir + "/storage/ledger";
            if (Directory.Exists(ledgerDir))
            {
                var latestLedgers =
                    Directory.GetFiles(ledgerDir, "ledger_*")
                    .OrderBy(file =>
                        Path.GetFileName(file).PadForNaturalNumberOrdering());
                var nodeName = new DirectoryInfo(nodeDir).Name;
                logger.LogInformation(
                    $"Found following ledger files on node '{nodeName}': " +
                    $"{JsonSerializer.Serialize(latestLedgers, Utils.Options)}.");
                nodes.Add(nodeName);
            }
        }

        return nodes;
    }

    private static string GetNodeStorageDirectory(
        string networkName,
        InfraType infraType,
        string nodeNameInput)
    {
        string hostWorkspaceDir =
            Environment.GetEnvironmentVariable("HOST_WORKSPACE_DIR") ??
            Environment.GetEnvironmentVariable("WORKSPACE_DIR") ??
            Directory.GetCurrentDirectory();

        var infraTypeFolderName = infraType.ToString().ToLower();
        string nodeFolder = $"/{infraTypeFolderName}/{networkName}/{nodeNameInput}";
        var hostNodeWorkspaceDir = hostWorkspaceDir + nodeFolder;
        return hostNodeWorkspaceDir + "/storage";
    }
}

internal class DockerDirectory : IDirectory
{
    public string MountPath { get; set; } = default!;

    public string VolumeMountPath { get; set; } = default!;

    internal string HostPath { get; set; } = default!;

    internal string WorkspacePath { get; set; } = default!;
}