// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Test;

internal class ODataError
{
    public ErrorResponse Error { get; set; } = default!;

    public class ErrorResponse
    {
        public string Code { get; set; } = default!;

        public string Message { get; set; } = default!;
    }
}
