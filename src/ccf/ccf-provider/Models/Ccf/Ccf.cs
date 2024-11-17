// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Controllers;

public static class Ccf
{
    public class MemberInfo
    {
        [JsonPropertyName("cert")]
        public string Cert { get; set; } = default!;

        [JsonPropertyName("member_data")]
        public JsonObject MemberData { get; set; } = default!;

        [JsonPropertyName("public_encryption_key")]
        public string PublicEncryptionKey { get; set; } = default!;

        [JsonPropertyName("status")]
        public string Status { get; set; } = default!;
    }

    public class JoinPolicyInfo
    {
        [JsonPropertyName("sgx")]
        public JsonObject Sgx { get; set; } = default!;

        [JsonPropertyName("snp")]
        public SnpSection Snp { get; set; } = default!;

        [JsonPropertyName("measurements")]
        public JsonObject Measurements { get; set; } = default!;

        [JsonPropertyName("uvmEndorsements")]
        public JsonObject UvmEndorsements { get; set; } = default!;

        public class SnpSection
        {
            [JsonPropertyName("hostData")]
            public Dictionary<string, string> HostData { get; set; } = default!;
        }
    }
}
