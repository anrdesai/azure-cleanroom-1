// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Controllers;
using EntityDataModel;

namespace ArmClient;

public interface IAsyncOperationsManager
{
    Task RecordOperationStart(
        string operationId,
        string method,
        string path,
        CancellationTokenSource cts);

    Task RecordOperationEnd(
        string operationId,
        string status,
        int statuCode,
        JsonObject? result,
        ODataError? error);

    Task<OperationStatus?> GetOperationStatus(string operationId);

    Task<bool> CancelOperation(string operationId);
}
