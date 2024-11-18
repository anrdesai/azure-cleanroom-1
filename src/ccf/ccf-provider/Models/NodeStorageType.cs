// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CcfProvider;

public enum NodeStorageType
{
    /// <summary>
    /// Node will use the local file system exposed within the container to back its ledger and
    /// snapshots folder.
    /// </summary>
    LocalFs,

    /// <summary>
    /// Only valid for for docker provider type. Node will use the docker host's file system
    /// to mount directories on the host into the container to back its ledger and snapshots folder.
    /// </summary>
    DockerHostFs,

    /// <summary>
    /// Node will use Azure file share mounted in the container to back its ledger and snapshots
    /// folder.
    /// </summary>
    AzureFiles
}