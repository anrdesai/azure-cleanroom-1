// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.CleanRoomSidecar.Identity.Errors;

namespace Utilities;

public static class ExceptionUtilities
{
    public static ExceptionDimensions GetDimensions(this Exception ex)
    {
        string errorCode;

        // Try to get richer data for dimension values if possible.
        if (ex is IdentityException identityException)
        {
            if ((identityException.ErrorCode == IdentityErrorCode.InternalError) &&
                identityException.InnerException != null)
            {
                errorCode = identityException.InnerException.GetType().Name;
            }
            else
            {
                errorCode = identityException.ErrorCode.ToString();
            }
        }
        else
        {
            errorCode = ex.GetType().Name;
        }

        return new ExceptionDimensions
        {
            ErrorCode = errorCode
        };
    }
}

public class ExceptionDimensions
{
    public string ErrorCode { get; set; } = default!;
}