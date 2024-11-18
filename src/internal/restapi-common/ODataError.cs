// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Docker.DotNet;
using Microsoft.Rest.TransientFaultHandling;

namespace Controllers;

public class ODataError
{
    public ODataError(string code, string message)
    {
        this.Error.Code = code;
        this.Error.Message = message;
    }

    public ErrorResponse Error { get; set; } = new();

    public static (int statuCode, ODataError error) FromException(Exception e)
    {
        int statusCode = 500;
        string code = e.GetType().Name;
        string message = e.Message;
        if (e is ApiException ae)
        {
            code = ae.Code;
            message = ae.Message;
            statusCode = (int)ae.StatusCode;
        }
        else if (e is Azure.RequestFailedException rfe)
        {
            code = rfe.ErrorCode ?? code;
            message = rfe.Message;
            statusCode = rfe.Status;
        }
        else if (e is DockerApiException de)
        {
            statusCode = (int)de.StatusCode;
            code = de.StatusCode.ToString();
        }
        else if (e is HttpRequestWithStatusException se)
        {
            try
            {
                var o = JsonSerializer.Deserialize<ODataError>(se.Message);
                if (o != null && !string.IsNullOrEmpty(o.Error?.Code))
                {
                    code = o.Error.Code;
                    message = o.Error.Message;
                }
                else
                {
                    code = se.StatusCode.ToString();
                    message = se.Message;
                }
            }
            catch
            {
                code = se.StatusCode.ToString();
                message = se.Message;
            }

            statusCode = (int)se.StatusCode;
        }

        var error = new ODataError(code, message);
        return (statusCode, error);
    }

    public class ErrorResponse
    {
        public string Code { get; set; } = default!;

        public string Message { get; set; } = default!;
    }
}
