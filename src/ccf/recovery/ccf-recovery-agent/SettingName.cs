// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Controllers;

public static class SettingName
{
    public const string CcfEndpoint = "CCF_ENDPOINT";
    public const string CcfEndpointCert = "CCF_ENDPOINT_CERT";
    public const string CcfEndpointSkipTlsVerify = "CCF_ENDPOINT_SKIP_TLS_VERIFY";

    public const string CcfRecoverySvcEndpoint = "CCF_RECOVERY_SVC_ENDPOINT";
    public const string CcfRecoverySvcEndpointCert = "CCF_RECOVERY_SVC_ENDPOINT_CERT";
    public const string CcfRecoverySvcEndpointSkipTlsVerify =
        "CCF_RECOVERY_SVC_ENDPOINT_SKIP_TLS_VERIFY";

    public const string ServiceCertLocation = "SERVICE_CERT_LOCATION";
}
