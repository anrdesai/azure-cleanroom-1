// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test;

[TestClass]
public class SecretTests : TestBase
{
    [TestMethod]
    public async Task InsertAndGetSecret()
    {
        string contractId = this.ContractId;
        string secretName = Guid.NewGuid().ToString().Substring(0, 8);
        this.Logger.LogInformation($"secretName: {secretName}");
        string secretNameUrl = $"contracts/{contractId}/secrets/{secretName}";

        // Add a secret as member0.
        string secretId;
        using (HttpRequestMessage request = new(HttpMethod.Put, secretNameUrl))
        {
            request.Content = new StringContent(
                new JsonObject
                {
                    ["value"] = "somesecret"
                }.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await this.CgsClient_Member0.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responseBody = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            secretId = responseBody["secretId"]!.ToString();
            Assert.IsTrue(!string.IsNullOrEmpty(secretId));
        }

        // Getting secret before a clean room policy is set should fail.
        string secretIdUrl = $"secrets/{secretId}";
        using (HttpRequestMessage request = new(HttpMethod.Post, secretIdUrl))
        {
            using HttpResponseMessage response = await this.GovSidecarClient.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ODataError>())!.Error;
            Assert.AreEqual("VerifySnpAttestationFailed", error.Code);
            Assert.AreEqual(
                "The clean room policy is missing. Please propose a new clean room policy.",
                error.Message);
        }

        await this.ProposeContractAndAcceptAllowAllCleanRoomPolicy(contractId);

        // As the contract and clean room policy was proposed and accepted above get secret
        // should now succeed.
        using (HttpRequestMessage request = new(HttpMethod.Post, secretIdUrl))
        {
            using HttpResponseMessage response = await this.GovSidecarClient.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responseBody = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            string secretValue = responseBody["value"]!.ToString();
            Assert.AreEqual("somesecret", secretValue);
        }
    }

    [TestMethod]
    public async Task InsertAndGetSecretPlaceholderContract()
    {
        var govSidecarClient = new HttpClient
        {
            BaseAddress = new Uri(this.Configuration["govSidecarEndpoint"]!)
        };

        // docker-compose setup for ccr-governance is configured with "app/contracts/placeholder"
        // for the api path prefix.
        string contractId = "placeholder";
        string secretName = Guid.NewGuid().ToString().Substring(0, 8);
        this.Logger.LogInformation($"secretName: {secretName}");
        string secretNameUrl = $"contracts/{contractId}/secrets/{secretName}";

        // Add a secret as member0.
        string secretId;
        string secretValue = Guid.NewGuid().ToString().Substring(0, 8);
        using (HttpRequestMessage request = new(HttpMethod.Put, secretNameUrl))
        {
            request.Content = new StringContent(
                new JsonObject
                {
                    ["value"] = secretValue
                }.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await this.CgsClient_Member0.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responseBody = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            secretId = responseBody["secretId"]!.ToString();
            Assert.IsTrue(!string.IsNullOrEmpty(secretId));
        }

        // Re-running this test will find an existing clean room policy for the "default" contract
        // so handle that.
        bool cleanRoomPolicyExists;
        using (HttpRequestMessage request =
            new(HttpMethod.Get, $"contracts/{contractId}/cleanroompolicy"))
        {
            using HttpResponseMessage response = await this.CgsClient_Member0.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responseBody = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            cleanRoomPolicyExists = responseBody["policy"]!.ToJsonString() != "{}";
        }

        string secretIdUrl = $"secrets/{secretId}";
        if (!cleanRoomPolicyExists)
        {
            // Getting secret before a clean room policy is set should fail.
            using (HttpRequestMessage request = new(HttpMethod.Post, secretIdUrl))
            {
                using HttpResponseMessage response = await govSidecarClient.SendAsync(request);
                Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
                var error = (await response.Content.ReadFromJsonAsync<ODataError>())!.Error;
                Assert.AreEqual("VerifySnpAttestationFailed", error.Code);
                Assert.AreEqual(
                    "The clean room policy is missing. Please propose a new clean room policy.",
                    error.Message);
            }

            await this.ProposeContractAndAcceptAllowAllCleanRoomPolicy(contractId);
        }

        // As the contract and clean room policy was proposed and accepted above get secret
        // should now succeed.
        using (HttpRequestMessage request = new(HttpMethod.Post, secretIdUrl))
        {
            using HttpResponseMessage response = await govSidecarClient.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responseBody = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            string value = responseBody["value"]!.ToString();
            Assert.AreEqual(secretValue, value);
        }
    }

    [TestMethod]
    public async Task InsertAndGetSecretBadInputs()
    {
        string contractId = this.ContractId;
        string secretName = Guid.NewGuid().ToString().Substring(0, 8);
        this.Logger.LogInformation($"secretName: {secretName}");

        // Getting a secret directly using the CCF client without presenting any
        // attestation report should fail.
        string dummySecretUrl = $"app/contracts/{contractId}/secrets/doesnotmatter";
        using (HttpRequestMessage request = new(HttpMethod.Post, dummySecretUrl))
        {
            request.Content = new StringContent(
                "{}",
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await this.CcfClient.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ODataError>())!.Error;
            Assert.AreEqual("AttestationMissing", error.Code);
            Assert.AreEqual(
                "Attestation payload must be supplied.",
                error.Message);
        }

        // Not sending encryption data should get caught.
        using (HttpRequestMessage request = new(HttpMethod.Post, dummySecretUrl))
        {
            request.Content = new StringContent(
                new JsonObject
                {
                    // Payload without "encrypt" key.
                    ["attestation"] = "doesnotmatter"
                }.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await this.CcfClient.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ODataError>())!.Error;
            Assert.AreEqual("EncryptionMissing", error.Code);
            Assert.AreEqual(
                "Encrypt payload must be supplied.",
                error.Message);
        }

        // Try fetching a secret with invalid attestation.
        using (HttpRequestMessage request = new(HttpMethod.Post, dummySecretUrl))
        {
            // Payload contains invalid attestation report.
            request.Content = new StringContent(
                new JsonObject
                {
                    ["attestation"] = "invalidinput",
                    ["encrypt"] = "doesnotmatter"
                }.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await this.CcfClient.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ODataError>())!.Error;
            Assert.AreEqual("VerifySnpAttestationFailed", error.Code);
        }

        using (HttpRequestMessage request = new(HttpMethod.Post, dummySecretUrl))
        {
            // Payload contains valid attestation report but no clean room policy has been proposed
            // yet so get secret should fail.
            var attestationReport = JsonSerializer.Deserialize<JsonObject>(
                await File.ReadAllTextAsync("data/attestation-report.json"))!;
            var publicKey = CreateX509Certificate2("foo").PublicKey.ExportSubjectPublicKeyInfo();
            var publicKeyPem = PemEncoding.Write("PUBLIC KEY", publicKey);
            request.Content = new StringContent(
                new JsonObject
                {
                    ["attestation"] = new JsonObject
                    {
                        ["evidence"] = attestationReport["attestation"]!.ToString(),
                        ["endorsements"] = attestationReport["platformCertificates"]!.ToString(),
                        ["uvm_endorsements"] = attestationReport["uvmEndorsements"]!.ToString(),
                    },
                    ["encrypt"] = new JsonObject
                    {
                        ["publicKey"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKeyPem))
                    }
                }.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await this.CcfClient.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ODataError>())!.Error;
            Assert.AreEqual("VerifySnpAttestationFailed", error.Code);
            Assert.AreEqual(
                "The clean room policy is missing. Please propose a new clean room policy.",
                error.Message);
        }

        // Propose and accept the contract and cleanroom policy before testing the next set
        // of scenarios.
        await this.ProposeContractAndAcceptAllowAllCleanRoomPolicy(contractId);

        using (HttpRequestMessage request = new(HttpMethod.Post, dummySecretUrl))
        {
            // Payload contains valid attestation report but public key does not match reportdata.
            var attestationReport = JsonSerializer.Deserialize<JsonObject>(
                await File.ReadAllTextAsync("data/attestation-report.json"))!;
            var publicKey = CreateX509Certificate2("foo").PublicKey.ExportSubjectPublicKeyInfo();
            var publicKeyPem = PemEncoding.Write("PUBLIC KEY", publicKey);
            request.Content = new StringContent(
                new JsonObject
                {
                    ["attestation"] = new JsonObject
                    {
                        ["evidence"] = attestationReport["attestation"]!.ToString(),
                        ["endorsements"] = attestationReport["platformCertificates"]!.ToString(),
                        ["uvm_endorsements"] = attestationReport["uvmEndorsements"]!.ToString(),
                    },
                    ["encrypt"] = new JsonObject
                    {
                        ["publicKey"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKeyPem))
                    }
                }.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await this.CcfClient.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ODataError>())!.Error;
            Assert.AreEqual("ReportDataMismatch", error.Code);
            Assert.AreEqual(
                "Attestation report_data value did not match calculated value.",
                error.Message);
        }

        // Add a secret as member0.
        // Add a very large secret that cannot get wrapped.
        int maxSecretLength = 25600;
        string longSecret = new('*', maxSecretLength + 1);
        using (HttpRequestMessage request =
            new(HttpMethod.Put, $"contracts/{contractId}/secrets/longSecret"))
        {
            request.Content = new StringContent(
                new JsonObject
                {
                    ["value"] = longSecret
                }.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await this.CgsClient_Member0.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ODataError>())!.Error;
            Assert.AreEqual("ValueTooLarge", error.Code);
            Assert.AreEqual(
                $"Length of the value should not exceed {maxSecretLength} characters. " +
                $"Input is {maxSecretLength + 1} characters.",
                error.Message);
        }

        // maxSecretLength should work.
        string secretId;
        longSecret = new('*', maxSecretLength);
        using (HttpRequestMessage request =
            new(HttpMethod.Put, $"contracts/{contractId}/secrets/longSecret"))
        {
            request.Content = new StringContent(
                new JsonObject
                {
                    ["value"] = longSecret
                }.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await this.CgsClient_Member0.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responseBody = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            secretId = responseBody["secretId"]!.ToString();
            Assert.IsTrue(!string.IsNullOrEmpty(secretId));
        }

        using (HttpRequestMessage request =
            new(HttpMethod.Post, $"secrets/{secretId}"))
        {
            using HttpResponseMessage response = await this.GovSidecarClient.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responseBody = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            string secretValue = responseBody["value"]!.ToString();
            Assert.AreEqual(longSecret, secretValue);
        }

        static X509Certificate2 CreateX509Certificate2(string certName)
        {
            var rsa = RSA.Create();
            var req = new CertificateRequest(
                $"cn={certName}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
            return new X509Certificate2(
                cert.Export(X509ContentType.Pfx, string.Empty), string.Empty);
        }
    }
}