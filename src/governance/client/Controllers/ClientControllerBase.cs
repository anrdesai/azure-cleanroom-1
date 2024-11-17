// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

public abstract class ClientControllerBase : ControllerBase
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public ClientControllerBase(
        ILogger logger,
        IHttpContextAccessor httpContextAccessor)
    {
        this.Logger = logger;
        this.httpContextAccessor = httpContextAccessor;

        string? ccfEndpoint = this.GetHeader("x-ms-ccf-endpoint");

        string? serviceCertPem = this.GetServiceCertPem("x-ms-service-cert");

        this.CcfClientManager = new CcfClientManager(
            this.Logger,
            ccfEndpoint,
            serviceCertPem);
    }

    protected ILogger Logger { get; }

    protected CcfClientManager CcfClientManager { get; }

    protected string? GetHeader(string header)
    {
        if (this.httpContextAccessor.HttpContext != null &&
            this.httpContextAccessor.HttpContext.Request.Headers.TryGetValue(
                header,
                out var value))
        {
            return value.ToString();
        }

        return null;
    }

    protected string? GetServiceCertPem(string serviceCertHeader)
    {
        string? serviceCertBase64 = this.GetHeader(serviceCertHeader);
        if (serviceCertBase64 != null)
        {
            byte[] bytes = Convert.FromBase64String(serviceCertBase64);
            return Encoding.UTF8.GetString(bytes);
        }

        return null;
    }
}
