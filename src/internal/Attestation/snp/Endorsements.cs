// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Security.Cryptography.Cose;

namespace AttestationClient;

/// <summary>
/// Endorsements is a representation of the endorsements part of the
/// SevSnp attestation evidence.
/// </summary>
public class Endorsements(string endorsementValue)
{
    // The base64 encoding of the COSE Sign1 document capturing the PRSS signed
    // endorsement of the launch measurement in the SEV-SNP report.
    public string EndorsementValue { get; set; } = endorsementValue;

    public CoseSign1Message GetDecodedUvmEndorsement()
    {
        byte[] decodedCoseSign1MessageString;
        try
        {
            decodedCoseSign1MessageString = Convert.FromBase64String(this.EndorsementValue);
        }
        catch (FormatException ex)
        {
            throw new Exception(
                $"Endorsements does not have expected base64 format: {ex.Message}", ex);
        }

        CoseSign1Message result;
        try
        {
            result = CoseMessage.DecodeSign1(decodedCoseSign1MessageString);
        }
        catch (CryptographicException ex)
        {
            throw new Exception(
                $"Endorsements does not have expected CBOR payload format: {ex.Message}", ex);
        }

        return result;
    }
}