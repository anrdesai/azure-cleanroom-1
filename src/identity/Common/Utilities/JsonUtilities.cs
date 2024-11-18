// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Utilities;

public static class JsonUtilities
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        // Newtonsoft.Json NullValueHandling.Ignore equivalent
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize<T>(T content)
    {
        return JsonSerializer.Serialize(content, Options);
    }

    public static T? Deserialize<T>(string content)
    {
        return JsonSerializer.Deserialize<T>(content, Options);
    }

    public static T? Deserialize<T>(JsonNode? node)
    {
        return node.Deserialize<T>(Options);
    }

    public static string SafeToString<T>(this T input)
    {
        try
        {
            return JsonSerializer.Serialize(
                input,
                options: Options);
        }
        catch (Exception e)
        {
            StringBuilder sb = new();
            sb.AppendLine(
                $"Hit exception in SafeToString for {input?.GetType()}. " +
                $"Exception: {e.ToString()}");
            return sb.ToString();
        }
    }
}
