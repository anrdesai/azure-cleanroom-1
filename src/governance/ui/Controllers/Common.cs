// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CgsUI.Controllers;

public static class Common
{
    public static string ConnectedTo { get; set; } = default!;

    public static string Name { get; set; } = default!;

    public static string MemberId { get; set; } = default!;

    public static string GetEndpoint(this IConfiguration configuration)
    {
        return configuration["cgsclientEndpoint"] ?? "http://localhost:9290";
    }
}
