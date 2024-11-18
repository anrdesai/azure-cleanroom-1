// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Rest.TransientFaultHandling;

namespace Controllers;

public static class HttpResponseMessageExtensions
{
    public static void LogRequest(this HttpResponseMessage response, ILogger logger)
    {
        if (response is null)
        {
            return;
        }

        var request = response.RequestMessage;
        logger.LogInformation($"{request?.Method} ");
        logger.LogInformation($"{request?.RequestUri} ");
        logger.LogInformation($"HTTP/{request?.Version}");
    }

    public static async Task ValidateStatusCodeAsync(
        this HttpResponseMessage response,
        ILogger logger)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();

            logger.LogError(
                $"{response.RequestMessage!.Method} request for resource: " +
                $"{response.RequestMessage.RequestUri} " +
                $"failed with statusCode {response.StatusCode}, " +
                $"reasonPhrase: {response.ReasonPhrase} and content: {content}.");

            throw new HttpRequestWithStatusException(content)
            {
                StatusCode = response.StatusCode
            };
        }
    }
}