// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;

namespace Controllers;

public class ApiException : Exception
{
    public ApiException(HttpStatusCode statusCode, string code, string message)
        : base(message)
    {
        this.StatusCode = statusCode;
        this.Code = code;
    }

    public ApiException(HttpStatusCode statusCode, ODataError error)
        : this(statusCode, error.Error.Code, error.Error.Message)
    {
    }

    public ApiException(ODataError error)
        : this(error.Error.Code, error.Error.Message)
    {
    }

    public ApiException(string code, string message)
        : base(message)
    {
        this.StatusCode = HttpStatusCode.InternalServerError;
        this.Code = code;
    }

    public string Code { get; set; } = default!;

    public HttpStatusCode StatusCode { get; set; } = default!;
}
