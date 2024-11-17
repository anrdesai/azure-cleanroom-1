// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Test;

internal class StatusWithReasonResponse
{
    public string Status { get; set; } = default!;

    public ErrorResponse Reason { get; set; } = default!;

    public class ErrorResponse
    {
        public string Code { get; set; } = default!;

        public string Message { get; set; } = default!;
    }
}
