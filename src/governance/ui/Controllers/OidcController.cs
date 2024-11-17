// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CgsUI.Models;
using Microsoft.AspNetCore.Mvc;

namespace CgsUI.Controllers;

public class OidcController : Controller
{
    private readonly ILogger<OidcController> logger;
    private readonly IConfiguration configuration;

    public OidcController(
        ILogger<OidcController> logger,
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
            var item = await client.GetFromJsonAsync<OidcIssuerViewModel>(
                $"{this.configuration.GetEndpoint()}/oidc/issuerInfo");
            return this.View(item);
        }
        catch (HttpRequestException re)
        {
            return this.View("Error", new ErrorViewModel
            {
                Content = re.Message
            });
        }
    }
}
