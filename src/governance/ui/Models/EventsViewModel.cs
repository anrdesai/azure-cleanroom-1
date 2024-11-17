// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace CgsUI.Models;

public class EventsViewModel
{
    public Event[] Value { get; set; } = default!;
}

public class Event
{
    public string Scope { get; set; } = default!;

    public string Id { get; set; } = default!;

    public string Timestamp { get; set; } = default!;

    public string Timestamp_Iso { get; set; } = default!;

    public JsonObject Data { get; set; } = default!;
}
