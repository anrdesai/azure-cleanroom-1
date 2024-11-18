// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CcfProvider;

public class Consensus
{
    // Example schema:
    //  "details": {
    //    "acks": {
    //      "0722170378994a615d375788bbd7ce85ec124d234aa9dada326bb1a2070737b7": {
    //        "last_received_ms": 113,
    //        "seqno": 30
    //      },
    //      "1311d56b6d79df4eb8fede1a6fe39335f304a74ee0ef6bf90cf3182116004182": {
    //        "last_received_ms": 119,
    //        "seqno": 30
    //      },
    //      "a8c5a2dd8e0dcda13a17e9e9ccf04133f19bf5c9909eb01c22487090438d350b": {
    //        "last_received_ms": 3516396,
    //        "seqno": 30
    //      },
    //      "decad8f7b99046c5c9446718cf976b6a82094f247ce05cc802688f70cc2bfc6d": {
    //        "last_received_ms": 123,
    //        "seqno": 30
    //      }
    //    },
    //    "configs": [
    //      {
    //        "idx": 17,
    //        "nodes": {
    //          "0722170378994a615d375788bbd7ce85ec124d234aa9dada326bb1a2070737b7": {
    //            "address": "testnet-a-2:8081"
    //          },
    //          "1311d56b6d79df4eb8fede1a6fe39335f304a74ee0ef6bf90cf3182116004182": {
    //            "address": "testnet-a-3:8081"
    //          },
    //          "a8c5a2dd8e0dcda13a17e9e9ccf04133f19bf5c9909eb01c22487090438d350b": {
    //    "address": "testnet-a-4:8081"
    //          },
    //          "d05e7eabf38b325c030f1c452ebbbf035a60c566a096b7bb02abdb2fd2c86564": {
    //    "address": "testnet-a-0:8081"
    //          },
    //          "decad8f7b99046c5c9446718cf976b6a82094f247ce05cc802688f70cc2bfc6d": {
    //    "address": "testnet-a-1:8081"
    //          }
    //        },
    //        "rid": 17
    //      }
    //    ],
    //    "current_view": 2,
    //    "leadership_state": "Leader",
    //    "membership_state": "Active",
    //    "primary_id": "d05e7eabf38b325c030f1c452ebbbf035a60c566a096b7bb02abdb2fd2c86564",
    //    "reconfiguration_type": "OneTransaction",
    //    "ticking": true
    //  }
    //}
    [JsonPropertyName("details")]
    public Details Details { get; set; } = default!;
}

public class Details
{
    [JsonPropertyName("acks")]
    public Dictionary<string, Acks> Acks { get; set; } = default!;

    [JsonPropertyName("current_view")]
    public int CurrentView { get; set; } = default;

    [JsonPropertyName("leadership_state")]
    public string LeadershipState { get; set; } = default!;

    [JsonPropertyName("membership_state")]
    public string MembershipState { get; set; } = default!;

    [JsonPropertyName("primary_id")]
    public string PrimaryId { get; set; } = default!;

    [JsonPropertyName("reconfiguration_type")]
    public string ReconfigurationType { get; set; } = default!;

    [JsonPropertyName("ticking")]
    public bool Ticking { get; set; } = default!;
}

public class Acks
{
    [JsonPropertyName("last_received_ms")]
    public int LastReceivedMs { get; set; } = default!;

    [JsonPropertyName("seq_no")]
    public int SeqNo { get; set; } = default!;
}