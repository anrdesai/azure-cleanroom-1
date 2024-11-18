// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace Controllers;

public static class Utils
{
    public static byte[] UnwrapRsaOaepAesKwpValue(
        this byte[] wrappedValue,
        string privateKeyPem,
        string alg)
    {
        RSAEncryptionPadding padding;
        if (alg == "CKM_RSA_AES_KEY_WRAP")
        {
            // CKM_RSA_AES_KEY_WRAP relies on SHA1 hash.
            padding = RSAEncryptionPadding.OaepSHA1;
        }
        else if (alg == "RSA_AES_KEY_WRAP_256")
        {
            // RSA_AES_KEY_WRAP_256 relies on SHA256 hash.
            padding = RSAEncryptionPadding.OaepSHA256;
        }
        else if (alg == "RSA_AES_KEY_WRAP_384")
        {
            // RSA_AES_KEY_WRAP_384 relies on SHA384 hash.
            padding = RSAEncryptionPadding.OaepSHA384;
        }
        else
        {
            throw new Exception($"Unsupported hash for the wrapping protocol: {alg}");
        }

        // Do the equivalent of ccf.crypto.unwrapKey(algo: RSA-OAEP-AES-KWP) using BouncyCastle.
        // Note (gsinha): Using BouncyCastle as not able to figure out how to do the equivalent
        // with .NET libraries.
        using var privateKey = RSA.Create();
        var firstPart = wrappedValue.Take(256).ToArray();
        var secondPart = wrappedValue.Skip(256).ToArray();
        privateKey.ImportFromPem(privateKeyPem);
        var aesKey = privateKey.Decrypt(firstPart, padding);
        var en = new AesWrapPadEngine();
        en.Init(false, new KeyParameter(aesKey));
        var unwrappedValue = en.Unwrap(secondPart, 0, secondPart.Length);
        return unwrappedValue;
    }
}
