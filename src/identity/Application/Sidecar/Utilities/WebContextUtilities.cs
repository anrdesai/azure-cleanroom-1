// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Constants;
using Controllers;
using OpenTelemetry;

namespace IdentitySidecar.Utilities;

public static class WebContextUtilities
{
    public static void SetLoggingContext(this WebContext webContext)
    {
        Baggage.SetBaggage(
            BaggageItemName.CorrelationRequestId,
            webContext.CorrelationId.ToString());
        Baggage.SetBaggage(BaggageItemName.ClientRequestId, webContext.ClientRequestId);
    }
}