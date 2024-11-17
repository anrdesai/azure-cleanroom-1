// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CcfProvider;

public interface IDirectory
{
    // Eg: /mnt/ro-snapshots/snapshots
    string MountPath { get; }
}