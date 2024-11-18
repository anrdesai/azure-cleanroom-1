// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CcfProvider;

public class NetworkNode
{
    //  "last_written": 3,
    //  "node_data": {
    //    "id": "testnet-a-1",
    //    "infraProviderData": null
    //  },
    //  "node_id": "303061fb588f35c16912d1db9ea31d93f4cdcad0bc45716a532002904ecd335b",
    //  "primary": false,
    //  "rpc_interfaces": {
    //    "debug_interface": {
    //      "bind_address": "0.0.0.0:8082",
    //      "endorsement": {
    //        "authority": "Node"
    //      },
    //      "published_address": "testnet-a-1:8082"
    //    },
    //    "primary_rpc_interface": {
    //      "bind_address": "0.0.0.0:8080",
    //      "published_address": "testnet-a-1:8080"
    //    }
    //  },
    //  "status": "Trusted"
    //}
    [JsonPropertyName("last_written")]
    public int LastWritten { get; set; } = default!;

    [JsonPropertyName("node_data")]
    public NodeData NodeData { get; set; } = default!;

    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = default!;

    [JsonPropertyName("primary")]
    public bool Primary { get; set; } = default!;

    [JsonPropertyName("rpc_interfaces")]
    public JsonObject RpcInterfaces { get; set; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;
}