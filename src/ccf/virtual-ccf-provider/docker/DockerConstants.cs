// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualCcfProvider;

public class DockerConstants
{
    public const string CcfNetworkResourceNameTag = "ccf-network/resource-name";
    public const string CcfNetworkTypeTag = "ccf-network/type";
    public const string CcfNetworkNameTag = "ccf-network/network-name";

    public const string CcfRecoveryServiceResourceNameTag = "ccf-recovery-service/resource-name";
    public const string CcfRecoveryServiceTypeTag = "ccf-recovery-service/type";
    public const string CcfRecoveryServiceNameTag = "ccf-recovery-service/recovery-service-name";

    public const string ServiceFolderMountPath = "/app/service";
    public const string ServiceCertPemFilePath = $"{ServiceFolderMountPath}/service-cert.pem";
}
