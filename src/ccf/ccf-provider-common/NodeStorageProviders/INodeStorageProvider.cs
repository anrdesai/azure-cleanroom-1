// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.ResourceManager.ContainerInstance;
using Docker.DotNet.Models;

namespace CcfProvider;

public interface INodeStorageProvider
{
    Task CreateNodeStorageDirectory(string nodeName);

    Task DeleteNodeStorageDirectory(string nodeName);

    NodeStorageType GetNodeStorageType();

    Task<(IDirectory rwLedgerDir, IDirectory rwSnapshotsDir, IDirectory? logsDir)>
        GetReadWriteLedgerSnapshotsDir(string nodeName);

    Task<(List<IDirectory> roLedgerDirs, IDirectory? roSnapshotsDir)>
        GetReadonlyLedgerSnapshotsDir();

    Task<(List<IDirectory> roLedgerDirs, IDirectory? roSnapshotsDir)>
        GetReadonlyLedgerSnapshotsDirForNetwork(string targetNetworkName);

    Task<List<string>> GetNodesWithLedgers();

    Task AddNodeStorageProviderConfiguration(
        string nodeConfigDataDir,
        IDirectory rwLedgerDir,
        IDirectory rwSnapshotsDir,
        List<IDirectory>? roLedgerDirs,
        IDirectory? roSnapshotsDir);

    Task<bool> NodeStorageDirectoryExists(string nodeName);

    Task UpdateCreateContainerParams(
        string nodeName,
        List<IDirectory>? roLedgerDirs,
        IDirectory? roSnapshotsDir,
        CreateContainerParameters createContainerParams);

    Task UpdateCreateContainerGroupParams(
        IDirectory rwLedgerDir,
        IDirectory rwSnapshotsDir,
        List<IDirectory>? roLedgerDirs,
        IDirectory? roSnapshotsDir,
        ContainerGroupData createContainerParams);

    Task CopyLatestSnapshot(IDirectory? roSnapshotsDir, IDirectory rwSnapshotsDir);

    Task DeleteUncommittedLedgerFiles(string nodeName);
}
