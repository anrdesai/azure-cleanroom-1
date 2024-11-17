// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Cose;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using static AttestationClient.UvmCoseSign1Message;

namespace AttestationClient;

public class SnpReport
{
    public const int RuntimeDataDigestSize = 32;

    // AMD Root Public key.
    public const string AmdRootPublicKey = @"-----BEGIN PUBLIC KEY-----
MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA0Ld52RJOdeiJlqK2JdsV
mD7FktuotWwX1fNgW41XY9Xz1HEhSUmhLz9Cu9DHRlvgJSNxbeYYsnJfvyjx1MfU
0V5tkKiU1EesNFta1kTA0szNisdYc9isqk7mXT5+KfGRbfc4V/9zRIcE8jlHN61S
1ju8X93+6dxDUrG2SzxqJ4BhqyYmUDruPXJSX4vUc01P7j98MpqOS95rORdGHeI5
2Naz5m2B+O+vjsC060d37jY9LFeuOP4Meri8qgfi2S5kKqg/aF6aPtuAZQVR7u3K
FYXP59XmJgtcog05gmI0T/OitLhuzVvpZcLph0odh/1IPXqx3+MnjD97A7fXpqGd
/y8KxX7jksTEzAOgbKAeam3lm+3yKIcTYMlsRMXPcjNbIvmsBykD//xSniusuHBk
gnlENEWx1UcbQQrs+gVDkuVPhsnzIRNgYvM48Y+7LGiJYnrmE8xcrexekBxrva2V
9TJQqnN3Q53kt5viQi3+gCfmkwC0F0tirIZbLkXPrPwzZ0M9eNxhIySb2npJfgnq
z55I0u33wh4r0ZNQeTGfw03MBUtyuzGesGkcw+loqMaq1qR4tjGbPYxCvpCq7+Og
pCCoMNit2uLo9M18fHz10lOMT8nWAUvRZFzteXCm+7PHdYPlmQwUw3LvenJ/ILXo
QPHfbkH0CyPfhl1jWhJFZasCAwEAAQ==
-----END PUBLIC KEY-----";

    public static readonly List<string> TrustedAmdRootPublicKeys = new()
    {
        AmdRootPublicKey
    };

    private string familyId;
    private string imageId;
    private string reportId;
    private string idKeyDigest;
    private uint vmpl;
    private bool smtAllowed;
    private bool debugAllowed;
    private bool migrateMaAllowed;
    private byte bootLoaderSvn;
    private byte teeSvn;
    private byte snpFwSvn;
    private byte microcodeSvn;
    private CoseSign1Message? trustedUvmEndorsement;

    private SnpReport(
        string evidence,
        string platformCertificates,
        string? uvmEndorsements)
    {
        var sevSnpData = SevSnpData.ParseAndVerify(evidence, platformCertificates, uvmEndorsements);

        this.familyId = BitConverter.ToString(sevSnpData.SnpReport.family_id)
            .Replace("-", string.Empty);
        this.imageId = BitConverter.ToString(sevSnpData.SnpReport.image_id)
            .Replace("-", string.Empty);
        this.LaunchMeasurement = BitConverter.ToString(sevSnpData.SnpReport.measurement)
            .Replace("-", string.Empty);
        this.HostData = BitConverter.ToString(sevSnpData.SnpReport.host_data)
            .Replace("-", string.Empty);
        this.ReportData = BitConverter.ToString(sevSnpData.SnpReport.report_data)
            .Replace("-", string.Empty);
        this.reportId = BitConverter.ToString(sevSnpData.SnpReport.report_id)
            .Replace("-", string.Empty);
        this.idKeyDigest = BitConverter.ToString(sevSnpData.SnpReport.id_key_digest)
            .Replace("-", string.Empty);
        this.AuthorKeyDigest = BitConverter.ToString(sevSnpData.SnpReport.author_key_digest)
            .Replace("-", string.Empty);

        this.ProductSvn = sevSnpData.SnpReport.guest_svn;
        this.vmpl = sevSnpData.SnpReport.vmpl;

        this.debugAllowed = sevSnpData.IsDebugAllowed;
        this.migrateMaAllowed = sevSnpData.IsMigrateMaAllowed;
        this.smtAllowed = sevSnpData.IsSmtAllowed;

        this.bootLoaderSvn = sevSnpData.BootloaderSvn;
        this.teeSvn = sevSnpData.TeeSvn;
        this.snpFwSvn = sevSnpData.SnpFwSvn;
        this.microcodeSvn = sevSnpData.MicrocodeSvn;

        this.trustedUvmEndorsement = sevSnpData.UvmEndorsement;
    }

    public string ReportData { get; }

    public string HostData { get; }

    public uint ProductSvn { get; }

    public string AuthorKeyDigest { get; }

    public string LaunchMeasurement { get; }

    public static SnpReport VerifySnpAttestation(
        string evidence,
        string platformCertificates,
        string? uvmEndorsements)
    {
        return new SnpReport(evidence, platformCertificates, uvmEndorsements);
    }

    public byte[] GetProductMeasurement()
    {
        return new byte[] { this.bootLoaderSvn, this.teeSvn, this.snpFwSvn, this.microcodeSvn };
    }

    public bool GetIsDebuggable()
    {
        return this.debugAllowed;
    }

    private static bool ComparePublicKey(PublicKey? key, string expectedKey)
    {
        if (key == null || string.IsNullOrEmpty(expectedKey))
        {
            throw new ArgumentException("Key or expected key is null");
        }

        RSACryptoServiceProvider expectedRSAProvider = new();
        expectedRSAProvider.ImportFromPem(expectedKey);

        var expectedKeyBuffer = expectedRSAProvider.ExportRSAPublicKey();
        var publicKey = key.GetRSAPublicKey();
        if (publicKey == null)
        {
            throw new ArgumentException("Public RSA key was not found");
        }

        var publicKeyBuffer = publicKey.ExportRSAPublicKey();
        return expectedKeyBuffer.SequenceEqual(publicKeyBuffer);
    }

    internal class SevSnpData
    {
        public SevSnpData(
            ReportStructures.AttestationReportV1 snpVmReport,
            string certificateChain,
            byte[] reportBuffer,
            Endorsements? endorsements = null)
        {
            this.SnpReport = snpVmReport;
            this.CertificateChain = certificateChain;
            this.ReportBuffer = reportBuffer;
            this.Endorsements = endorsements;

            this.VerifyReportSignature();
            this.VerifyUvmEndosement();
        }

        public X509Certificate2? RootCert { get; private set; }

        public X509Certificate2? LeafCert { get; private set; }

        public string CertificateChain { get; }

        public Endorsements? Endorsements { get; }

        public CoseSign1Message? UvmEndorsement { get; private set; }

        public ReportStructures.AttestationReportV1 SnpReport { get; }

        public byte[] ReportBuffer { get; }

        public bool IsDebugAllowed => (this.SnpReport.policy & 0x80000) != 0;

        public bool IsMigrateMaAllowed => (this.SnpReport.policy & 0x40000) != 0;

        public bool IsRsvdTrue => (this.SnpReport.policy & 0x20000) != 0;

        public bool IsSmtAllowed => (this.SnpReport.policy & 0x10000) != 0;

        public byte BootloaderSvn => (byte)(this.SnpReport.reported_tcb & 0xFF);

        public byte TeeSvn => (byte)((this.SnpReport.reported_tcb >> 8) & 0xFF);

        public byte SnpFwSvn => (byte)((this.SnpReport.reported_tcb >> 48) & 0xFF);

        public byte MicrocodeSvn => (byte)((this.SnpReport.reported_tcb >> 56) & 0xFF);

        public static SevSnpData ParseAndVerify(
            string evidence,
            string platformCertificates,
            string? uvmEndorsements)
        {
            var decodedReport = Convert.FromBase64String(evidence);
            var attestationReport = ReportStructures.Marshall<ReportStructures.AttestationReportV1>(
                decodedReport);

            var decodedCertChain = Convert.FromBase64String(platformCertificates);
            var certChain = Encoding.UTF8.GetString(decodedCertChain);

            Endorsements? endorsements = null;
            if (!string.IsNullOrEmpty(uvmEndorsements))
            {
                endorsements = new Endorsements(uvmEndorsements);
            }

            return new SevSnpData(attestationReport, certChain, decodedReport, endorsements);
        }

        // Verifies the presented UVM endorsement if presented and return the trusted endorsement
        // as a result.
        private void VerifyUvmEndosement()
        {
            if (this.Endorsements == null)
            {
                return;
            }

            CoseSign1Message uvmEndorsement = this.Endorsements.GetDecodedUvmEndorsement();

            var message = new UvmCoseSign1Message(uvmEndorsement);
            message.Verify();

            UvmPayload uvmPayload = message.GetUvmPayload();

            var launchMeasurement = BitConverter.ToString(this.SnpReport.measurement)
                .Replace("-", string.Empty);

            var endorsedLaunchMeasurement =
                string.IsNullOrEmpty(launchMeasurement) ? null : launchMeasurement?.ToLower();
            var presentedLaunchMeasurement =
                string.IsNullOrEmpty(uvmPayload.LaunchMeasurement) ? null :
                uvmPayload.LaunchMeasurement.ToLower();
            if (endorsedLaunchMeasurement == null || presentedLaunchMeasurement == null ||
                presentedLaunchMeasurement != endorsedLaunchMeasurement)
            {
                throw new Exception("Uvm endorsement does not match endorsed launch measurement.");
            }

            this.UvmEndorsement = uvmEndorsement;
        }

        private void VerifyReportSignature()
        {
            this.VerifyCertificateChain();

            if (this.RootCert == null)
            {
                throw new Exception("AMD Root certificate was not found.");
            }

            if (TrustedAmdRootPublicKeys.FirstOrDefault(
                (trustedKey) => ComparePublicKey(this.RootCert.PublicKey, trustedKey)) == null)
            {
                throw new Exception("AMD RootKey did not match expected value.");
            }

            if (this.LeafCert == null)
            {
                throw new Exception("Leaf cert cannot be null");
            }

            var leafCertKey = this.LeafCert.GetECDsaPublicKey();
            if (leafCertKey == null)
            {
                throw new Exception("Leaf cert does not have an ECDSA public key");
            }

            var keyParameters = leafCertKey.ExportParameters(false).Q.X;
            if (keyParameters == null)
            {
                throw new Exception("Could not determine length of public key in leaf certificate");
            }

            var keyLengthBytes = keyParameters.Length;

            // Extract & format the signature from the report
            // AMD firmware outputs the R & S components in Litte Endian, we need to convert it to
            // Big Endian
            var reversedRComponent =
                this.SnpReport.signature.RComponent.Take(keyLengthBytes).Reverse();
            var reversedSComponent =
                this.SnpReport.signature.SComponent.Take(keyLengthBytes).Reverse();
            var reportSignature = reversedRComponent.Concat(reversedSComponent).ToArray();
            if (!this.IsReportSignatureValid(this.ReportBuffer, reportSignature))
            {
                throw new Exception("VCEK signature validation failed");
            }
        }

        private void VerifyCertificateChain()
        {
            var certCollection = new X509Certificate2Collection();
            var regex = new Regex(
                @"-----BEGIN CERTIFICATE-----(.*?)-----END CERTIFICATE-----",
                RegexOptions.Singleline);
            MatchCollection matches = regex.Matches(this.CertificateChain);

            foreach (Match match in matches)
            {
                certCollection.Import(Encoding.UTF8.GetBytes(match.Value));
            }

            // Example certCollection:
            // [0] SEV-VCEK->SEV-Milan
            // [1] SEV-Milan->ARK-Milan
            // [2] ARK-Milan->ARK-Milan
            using (var chain = new X509Chain())
            {
                chain.ChainPolicy.DisableCertificateDownloads = true;
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationFlags =
                    X509VerificationFlags.AllowUnknownCertificateAuthority;

                var parsedRootCert = certCollection[certCollection.Count - 1];
                var parsedLeafCert = certCollection[0];
                foreach (var cert in certCollection.Reverse())
                {
                    if (!chain.Build(cert))
                    {
                        throw new Exception($"'{cert.Subject}' certificate chain was not valid.");
                    }

                    chain.ChainPolicy.CustomTrustStore.Add(cert);
                }

                if (chain.ChainStatus.Length > 0)
                {
                    StringBuilder sb = new("VCEK certificate chain was not valid.");
                    foreach (var cert in chain.ChainStatus)
                    {
                        sb.Append(cert.ToString());
                    }

                    throw new Exception(sb.ToString());
                }
            }

            this.LeafCert = certCollection[0];
            this.RootCert = certCollection[certCollection.Count - 1];
        }

        private bool IsReportSignatureValid(byte[] rawReportData, byte[] expectedSignature)
        {
            if (this.LeafCert == null)
            {
                throw new Exception("Leaf cert cannot be null");
            }

            var ecdsa = this.LeafCert.GetECDsaPublicKey();
            if (ecdsa == null)
            {
                throw new Exception("Leaf cert does not have an ECDSA public key");
            }

            var result = ecdsa.VerifyData(
                rawReportData.Take(this.ComputeDataSizeBeforeSignature()).ToArray(),
                expectedSignature,
                HashAlgorithmName.SHA384);
            return result;
        }

        private int ComputeDataSizeBeforeSignature()
        {
            return
                Marshal.SizeOf(this.SnpReport) -
                Marshal.SizeOf(this.SnpReport.signature);
        }
    }
}