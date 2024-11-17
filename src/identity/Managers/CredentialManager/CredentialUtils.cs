// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Identity.CredentialManager;

/// <summary>
/// Class that defines utility methods for credentials.
/// </summary>
internal static class CredentialUtils
{
    /// <summary>
    /// Gets the scope for the token request.
    /// </summary>
    /// <param name="scope">The resource that requires authentication.</param>
    /// <returns>The scopes.</returns>
    internal static string[] FormatScope(this string scope)
    {
#pragma warning disable SA1010 // Opening square brackets should be spaced correctly
        return [scope];
#pragma warning restore SA1010 // Opening square brackets should be spaced correctly
    }
}