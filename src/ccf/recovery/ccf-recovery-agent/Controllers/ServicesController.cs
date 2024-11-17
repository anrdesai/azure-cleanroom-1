// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Security.Cryptography;
using System.Text;
using AttestationClient;
using CcfCommon;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
public class ServicesController : ControllerBase
{
    private const string DefaultServiceCertLocation = "/app/service/service-cert.pem";

    private readonly ILogger logger;
    private readonly IConfiguration configuration;

    public ServicesController(
        ILogger logger,
        IConfiguration configuration)
    {
        this.logger = logger;
        this.configuration = configuration;
    }

    [HttpGet("/report")]
    public async Task<IActionResult> GetAgentReport()
    {
        // This API currently requires no attestation report input. Any client
        // can query for a member's public information.
        RecoveryAgentReport report = await GetReport();
        return this.Ok(report);

        async Task<RecoveryAgentReport> GetReport()
        {
            var serviceCertLocation =
                this.configuration[SettingName.ServiceCertLocation] ?? DefaultServiceCertLocation;
            if (!Path.Exists(serviceCertLocation))
            {
                throw new ApiException(
                    HttpStatusCode.NotFound,
                    "ServiceCertNotFound",
                    "Could not locate the service certificate for this service.");
            }

            var serviceCert = await System.IO.File.ReadAllTextAsync(serviceCertLocation);

            string platform;
            AttestationReport? report = null;
            if (CcfUtils.IsSevSnp())
            {
                platform = "snp";
                var bytes = Encoding.UTF8.GetBytes(serviceCert);
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(bytes);
                report = await Attestation.GetReportAsync(hash);
            }
            else
            {
                platform = "virtual";
            }

            return new RecoveryAgentReport
            {
                Platform = platform,
                Report = report,
                ServiceCert = serviceCert,
            };
        }
    }
}
