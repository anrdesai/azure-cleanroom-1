// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AttestationClient;

public static class Signing
{
    public static byte[] SignData(
        byte[] data,
        string signingKey,
        RSASignaturePaddingMode paddingMode)
    {
        byte[] signature;
        using RSA privateKey = RSA.Create();
        privateKey.ImportFromPem(signingKey);
        signature = privateKey.SignData(
            data,
            HashAlgorithmName.SHA256,
            paddingMode == RSASignaturePaddingMode.Pss ?
                RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1);
        return signature;
    }

    public static bool VerifyDataUsingCert(
        byte[] data,
        byte[] signature,
        string signingCert,
        RSASignaturePaddingMode paddingMode)
    {
        using var cert = X509Certificate2.CreateFromPem(signingCert);
        return VerifyDataUsingCert(data, signature, cert, paddingMode);
    }

    public static bool VerifyDataUsingCert(
        byte[] data,
        byte[] signature,
        X509Certificate2 signingCert,
        RSASignaturePaddingMode paddingMode)
    {
        using RSA publicKey = signingCert.GetRSAPublicKey()!;
        bool result = publicKey.VerifyData(
            data,
            signature,
            HashAlgorithmName.SHA256,
            paddingMode == RSASignaturePaddingMode.Pss ?
                RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1);
        return result;
    }

    public static bool VerifyDataUsingKey(
        byte[] data,
        byte[] signature,
        string publicKey,
        RSASignaturePaddingMode paddingMode)
    {
        using RSA key = RSA.Create();
        key.ImportFromPem(publicKey);
        bool result = key.VerifyData(
            data,
            signature,
            HashAlgorithmName.SHA256,
            paddingMode == RSASignaturePaddingMode.Pss ?
                RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1);
        return result;
    }
}
