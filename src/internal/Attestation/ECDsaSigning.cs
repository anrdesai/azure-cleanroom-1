// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AttestationClient;

public static class ECDsaSigning
{
    public static byte[] SignData(
        byte[] data,
        string signingKey)
    {
        byte[] signature;
        using ECDsa privateKey = ECDsa.Create();
        privateKey.ImportFromPem(signingKey);
        signature = privateKey.SignData(data, HashAlgorithmName.SHA256);
        return signature;
    }

    public static bool VerifyDataUsingCert(
        byte[] data,
        byte[] signature,
        string signingCert)
    {
        using var cert = X509Certificate2.CreateFromPem(signingCert);
        return VerifyDataUsingCert(data, signature, cert);
    }

    public static bool VerifyDataUsingCert(
        byte[] data,
        byte[] signature,
        X509Certificate2 signingCert)
    {
        using ECDsa publicKey = signingCert.GetECDsaPublicKey()!;
        bool result = publicKey.VerifyData(
            data,
            signature,
            HashAlgorithmName.SHA256);
        return result;
    }

    public static bool VerifyDataUsingKey(
        byte[] data,
        byte[] signature,
        string publicKey)
    {
        using ECDsa key = ECDsa.Create();
        key.ImportFromPem(publicKey);
        bool result = key.VerifyData(
            data,
            signature,
            HashAlgorithmName.SHA256);
        return result;
    }
}
