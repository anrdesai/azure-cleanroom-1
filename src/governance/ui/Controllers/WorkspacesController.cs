// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Mime;
using System.Text;
using CgsUI.Models;
using Microsoft.AspNetCore.Mvc;

namespace CgsUI.Controllers;

public class WorkspacesController : Controller
{
    private readonly ILogger<WorkspacesController> logger;
    private readonly IConfiguration configuration;

    public WorkspacesController(
        ILogger<WorkspacesController> logger,
        IConfiguration configuration)
    {
        this.logger = logger;
        this.configuration = configuration;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            using var client = new HttpClient();
            var items = await client.GetFromJsonAsync<List<ContractViewModel>>(
                $"{this.configuration.GetEndpoint()}/contracts");
            return this.View(items);
        }
        catch (HttpRequestException re)
        {
            return this.View("Error", new ErrorViewModel
            {
                Content = re.Message
            });
        }
    }

    // GET: Workspaces/Configure
    [Route("Workspaces/Configure")]
    public IActionResult Configure()
    {
        return this.View();
    }

    // POST: Workspaces/Configure
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Workspaces/Configure")]
    public async Task<IActionResult> Configure(WorkspaceConfigurationViewModel model)
    {
        if (model?.SigningCertPemFile == null || model.SigningCertPemFile.Length <= 0)
        {
            return this.BadRequest("No file was uploaded.");
        }

        if (model?.SigningKeyPemFile == null || model.SigningKeyPemFile.Length <= 0)
        {
            return this.BadRequest("No file was uploaded.");
        }

        if (string.IsNullOrWhiteSpace(model?.CcfEndpoint))
        {
            return this.BadRequest("No CCF endpoint specified.");
        }

        bool result = Uri.TryCreate(model.CcfEndpoint, UriKind.Absolute, out var uriResult)
        && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        if (!result)
        {
            return this.BadRequest($"CCF endpoint value '{model.CcfEndpoint}' is not a valid URL.");
        }

        string configureUrl =
            $"{this.configuration.GetEndpoint()}/configure";
        using (HttpRequestMessage request = new(HttpMethod.Post, configureUrl))
        {
            using MultipartFormDataContent multipartContent = new()
            {
                {
                    new StringContent(
                    model.CcfEndpoint,
                    Encoding.UTF8,
                    MediaTypeNames.Text.Plain),
                    "CcfEndpoint"
                },
                {
                    new StreamContent(
                    model.SigningCertPemFile.OpenReadStream()),
                    "SigningCertPemFile",
                    "SigningCertPemFile"
                },
                {
                    new StreamContent(
                    model.SigningKeyPemFile.OpenReadStream()),
                    "SigningKeyPemFile",
                    "SigningKeyPemFile"
                }
            };

            if (model.ServiceCertPemFile != null)
            {
                multipartContent.Add(
                    new StreamContent(
                        model.ServiceCertPemFile.OpenReadStream()),
                    "ServiceCertPemFile",
                    "ServiceCertPemFile");
            }

            request.Content = multipartContent;
            using var client = new HttpClient();
            using var response = await client.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return this.RedirectToAction("Index", "Settings");
            }
            else
            {
                return this.View("Error", new ErrorViewModel
                {
                    Content = await response.Content.ReadAsStringAsync()
                });
            }
        }
    }
}
