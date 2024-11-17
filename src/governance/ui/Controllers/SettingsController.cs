// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using CgsUI.Models;
using Microsoft.AspNetCore.Mvc;

namespace CgsUI.Controllers;

public class SettingsController : Controller
{
    private readonly ILogger<SettingsController> logger;
    private readonly IConfiguration configuration;

    public SettingsController(
        ILogger<SettingsController> logger,
        IConfiguration configuration)
    {
        this.logger = logger;
        this.configuration = configuration;
    }

    public async Task<IActionResult> Index()
    {
        using var client = new HttpClient();
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"{this.configuration.GetEndpoint()}/show");
        }
        catch
        {
            return this.View(new SettingsViewModel
            {
                Connected = false
            });
        }

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var item = (await response.Content.ReadFromJsonAsync<SettingsViewModel>())!;
            try
            {
                item.Updates = (await client.GetFromJsonAsync<UpdatesViewModel>(
                    $"{this.configuration.GetEndpoint()}/checkupdates"))!;
            }
            catch (HttpRequestException re)
            when (re.Message.Contains("The SSL connection could not be established, see inner " +
            "exception."))
            {
                return this.View(new SettingsViewModel
                {
                    Connected = true,
                    Configured = false
                });
            }

            item.Connected = true;
            item.Configured = true;

            return this.View(item);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return this.View(new SettingsViewModel
            {
                Connected = true,
                Configured = false
            });
        }

        var error = await response.Content.ReadAsStringAsync();
        return this.View("Error", new ErrorViewModel
        {
            Content = error
        });
    }

    public async Task<IActionResult> ConstitutionDetail()
    {
        using var client = new HttpClient();
        var content = await client.GetStringAsync(
            $"{this.configuration.GetEndpoint()}/constitution");
        return this.View(new ConstitutionViewModel
        {
            Content = content
        });
    }

    public async Task<IActionResult> JSAppDetail()
    {
        using var client = new HttpClient();
        var t1 = client.GetStringAsync(
            $"{this.configuration.GetEndpoint()}/jsapp/endpoints");
        var t2 = client.GetStringAsync(
            $"{this.configuration.GetEndpoint()}/jsapp/modules");
        await Task.WhenAll(t1, t2);
        string endpoints = (await t1)!;
        string modules = (await t2)!;
        var m = JsonNode.Parse(modules)?.AsObject()!;
        List<(string, string)> moduleToCode = new();
        foreach (var item in m.AsEnumerable())
        {
            moduleToCode.Add(new(item.Key.ToString(), item.Value!.ToString()));
        }

        return this.View(new JSAppViewModel
        {
            Endpoints = JsonNode.Parse(endpoints)!
                .ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
            Modules = moduleToCode
        });
    }
}
