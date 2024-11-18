// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using CgsUI.Models;
using Microsoft.AspNetCore.Mvc;

namespace CgsUI.Controllers;

public class MembersController : Controller
{
    private readonly ILogger<MembersController> logger;
    private readonly IConfiguration configuration;

    public MembersController(
        ILogger<MembersController> logger,
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
            var item = await client.GetFromJsonAsync<JsonObject>(
                $"{this.configuration.GetEndpoint()}/members");
            return this.View(new MembersViewModel
            {
                Members = item!.ToJsonString(new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                })
            });
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
