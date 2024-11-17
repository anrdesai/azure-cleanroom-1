// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CcfProvider;

public enum DeleteOption
{
    /// <summary>
    /// Leave the storage provisioned for the ledger/snapshots folder intact.
    /// </summary>
    RetainStorage,

    /// <summary>
    /// Deletes the storage provisioned for the ledger/snapshots folder.
    /// </summary>
    DeleteStorage
}