// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Azure.ResourceManager.ContainerInstance;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace CcfProvider;

internal class LocalFsNodeStorageProvider : INodeStorageProvider
{
    public LocalFsNodeStorageProvider(
        string networkName,
        JsonObject? providerConfig,
        InfraType infraType,
        ILogger logger)
    {
    }

    public NodeStorageType GetNodeStorageType()
    {
        return NodeStorageType.LocalFs;
    }

    public Task<bool> NodeStorageDirectoryExists(string nodeName)
    {
        return Task.FromResult(false);
    }

    public Task CreateNodeStorageDirectory(string nodeName)
    {
        return Task.CompletedTask;
    }

    public Task DeleteNodeStorageDirectory(string nodeName)
    {
        return Task.CompletedTask;
    }

    public Task DeleteUncommittedLedgerFiles(string nodeName)
    {
        return Task.CompletedTask;
    }

    public Task<(IDirectory rwLedgerDir, IDirectory rwSnapshotsDir, IDirectory? logsDir)>
    GetReadWriteLedgerSnapshotsDir(string nodeName)
    {
        IDirectory rwLedgerDir = new FsDirectory
        {
            MountPath = $"/app/ledger"
        };

        IDirectory rwSnapshotsDir = new FsDirectory
        {
            MountPath = $"/app/snapshots"
        };

        IDirectory logsDir = new FsDirectory
        {
            MountPath = $"/app/logs"
        };

        return Task.FromResult((rwLedgerDir, rwSnapshotsDir, (IDirectory?)logsDir));
    }

    public Task<(List<IDirectory> roLedgerDirs, IDirectory? roSnapshotsDir)>
        GetReadonlyLedgerSnapshotsDir()
    {
        List<IDirectory> roLedgerDirs = new();
        return Task.FromResult((roLedgerDirs, (IDirectory?)null));
    }

    public Task<(List<IDirectory> roLedgerDirs, IDirectory? roSnapshotsDir)>
    GetReadonlyLedgerSnapshotsDirForNetwork(string targetNetworkName)
    {
        List<IDirectory> roLedgerDirs = new();
        return Task.FromResult((roLedgerDirs, (IDirectory?)null));
    }

    public Task<List<string>> GetNodesWithLedgers()
    {
        return Task.FromResult(new List<string>());
    }

    public Task CopyLatestSnapshot(IDirectory? roSnapshotsDir, IDirectory rwSnapshotsDir)
    {
        return Task.CompletedTask;
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
        return Task.CompletedTask;
    }

    public Task UpdateCreateContainerGroupParams(
        IDirectory rwLedgerDir,
        IDirectory rwSnapshotsDir,
        List<IDirectory>? roLedgerDirs,
        IDirectory? roSnapshotsDir,
        ContainerGroupData createContainerParams)
    {
        return Task.CompletedTask;
    }

    internal class FsDirectory : IDirectory
    {
        public string MountPath { get; set; } = default!;
    }
}
