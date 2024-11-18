// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Controllers;

namespace EntityDataModel;

public class OperationStatus
{
    public string Id { get; set; } = default!;

    public string Method { get; set; } = default!;

    public string Path { get; set; } = default!;

    public int? StatusCode { get; set; } = default;

    public string Status { get; set; } = default!;

    public string StartTime { get; set; } = default!;

    public string? EndTime { get; set; }

    public ODataError? Error { get; set; }

    public JsonObject? Result { get; set; } = default!;
}