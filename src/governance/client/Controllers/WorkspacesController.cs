// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
public class WorkspacesController : ClientControllerBase
{
    public WorkspacesController(
        ILogger<WorkspacesController> logger,
        IHttpContextAccessor httpContextAccessor)
        : base(logger, httpContextAccessor)
    {
    }

    [HttpPost("/configure")]
    public async Task<IActionResult> SetWorkspaceConfig(
        [FromForm] WorkspaceConfigurationModel model)
    {
        // Signing cert and signing key need to be mandatorily configured. CCF endpoint and
        // service certificate are optional.
        if (model?.SigningCertPemFile == null || model.SigningCertPemFile.Length <= 0)
        {
            return this.BadRequest("No file was uploaded.");
        }

        if (model?.SigningKeyPemFile == null || model.SigningKeyPemFile.Length <= 0)
        {
            return this.BadRequest("No file was uploaded.");
        }

        using var reader = new StreamReader(model.SigningCertPemFile.OpenReadStream());
        string signingCert = await reader.ReadToEndAsync();

        using var reader2 = new StreamReader(model.SigningKeyPemFile.OpenReadStream());
        string signingKey = await reader2.ReadToEndAsync();

        string ccfEndpoint = string.Empty;
        if (!string.IsNullOrEmpty(model.CcfEndpoint))
        {
            ccfEndpoint = model.CcfEndpoint.Trim();
        }

        string serviceCertPem = string.Empty;
        if (model.ServiceCertPemFile != null)
        {
            using (var reader3 = new StreamReader(model.ServiceCertPemFile.OpenReadStream()))
            {
                serviceCertPem = await reader3.ReadToEndAsync();
            }
        }
        else if (!string.IsNullOrEmpty(ccfEndpoint))
        {
            var ep = new Uri(ccfEndpoint);
            if (ep.Host.ToLower().EndsWith("confidential-ledger.azure.com"))
            {
                string ccfEndpointName = ep.Host.Split(".")[0];
                using var client = new HttpClient();
                var response = await client.GetFromJsonAsync<JsonObject>(
                    $"https://identity.confidential-ledger.core.azure.com/ledgerIdentity" +
                    $"/{ccfEndpointName}");
                serviceCertPem = response!["ledgerTlsCertificate"]!.ToString()!;
            }
        }

        CcfClientManager.SetSigningDefaults(signingCert, signingKey);

        // Set workspace configuration values only if the CCF endpoint is specified as part of the
        // configure call. This serves as a default value when running in single CCF client mode.
        if (!string.IsNullOrEmpty(ccfEndpoint))
        {
            CcfClientManager.SetCcfDefaults(ccfEndpoint, serviceCertPem);
        }

        return this.Ok("Workspace details configured successfully.");
    }

    [HttpGet("/show")]
    public async Task<IActionResult> Show([FromQuery] bool? signingKey = false)
    {
        WorkspaceConfiguration copy;
        var wsConfig = this.CcfClientManager.GetWsConfig();
        if (wsConfig != null)
        {
            copy =
                JsonSerializer.Deserialize<WorkspaceConfiguration>(
                    JsonSerializer.Serialize(wsConfig))!;
            if (!signingKey.GetValueOrDefault())
            {
                copy.SigningKey = "<redacted>";
            }

            try
            {
                var ccfClient = await this.CcfClientManager.GetGovClient();
                using HttpResponseMessage response = await ccfClient.GetAsync("gov/members");
                await response.ValidateStatusCodeAsync(this.Logger);
                var jsonResponse = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
                copy.MemberData = jsonResponse[copy.MemberId]?["member_data"]?.AsObject();
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, "Failed to fetch members. Ignoring.");
            }
        }
        else
        {
            copy = new WorkspaceConfiguration();
        }

        copy.EnvironmentVariables = Environment.GetEnvironmentVariables();
        return this.Ok(copy);
    }

    [HttpGet("/constitution")]
    public async Task<IActionResult> GetConstitution()
    {
        var ccfClient = await this.CcfClientManager.GetGovClient();
        using HttpResponseMessage response =
            await ccfClient.GetAsync(
                $"gov/service/constitution?" +
                $"api-version={this.CcfClientManager.GetGovApiVersion()}");
        await response.ValidateStatusCodeAsync(this.Logger);
        var content = (await response.Content.ReadAsStringAsync())!;
        return this.Ok(content);
    }

    [HttpGet("/service/info")]
    public async Task<IActionResult> GetServiceInfo()
    {
        var ccfClient = await this.CcfClientManager.GetGovClient();
        using HttpResponseMessage response =
            await ccfClient.GetAsync(
                $"gov/service/info?api-version={this.CcfClientManager.GetGovApiVersion()}");
        await response.ValidateStatusCodeAsync(this.Logger);
        var content = (await response.Content.ReadAsStringAsync())!;
        return this.Ok(content);
    }

    [HttpGet("/jsapp/endpoints")]
    public async Task<IActionResult> GetJSAppEndpoints()
    {
        var ccfClient = await this.CcfClientManager.GetGovClient();
        using HttpResponseMessage response = await ccfClient.GetAsync(
            $"gov/service/javascript-app?" +
            $"api-version={this.CcfClientManager.GetGovApiVersion()}");
        await response.ValidateStatusCodeAsync(this.Logger);
        var content = (await response.Content.ReadAsStringAsync())!;
        return this.Ok(content);
    }

    [HttpGet("/jsapp/modules")]
    public async Task<IActionResult> JSAppModules()
    {
        var ccfClient = await this.CcfClientManager.GetGovClient();
        using HttpResponseMessage response = await ccfClient.GetAsync(
            $"gov/service/javascript-modules?" +
            $"api-version={this.CcfClientManager.GetGovApiVersion()}");
        await response.ValidateStatusCodeAsync(this.Logger);
        var modules = (await response.Content.ReadFromJsonAsync<JsonObject>())!;

        List<string> moduleNames = new();
        List<Task<string>> fetchModuleTasks = new();
        foreach (var item in modules["value"]!.AsArray().AsEnumerable())
        {
            var moduleName = item!.AsObject()["moduleName"]!.ToString();
            moduleNames.Add(moduleName);
        }

        // Sort the module names in alphabetical order so that we return the response ordered by
        // name.
        moduleNames = moduleNames.OrderBy(x => x, StringComparer.Ordinal).ToList();
        foreach (var moduleName in moduleNames)
        {
            var escapedString = Uri.EscapeDataString(moduleName);
            Task<string> fetchModuleTask = ccfClient.GetStringAsync(
            $"gov/service/javascript-modules/{escapedString}?" +
            $"api-version={this.CcfClientManager.GetGovApiVersion()}");
            fetchModuleTasks.Add(fetchModuleTask);
        }

        await Task.WhenAll(fetchModuleTasks);
        var modulesResponse = new JsonObject();
        for (int i = 0; i < moduleNames.Count; i++)
        {
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
            string content = (await fetchModuleTasks[i])!;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
            modulesResponse[moduleNames[i]] = content;
        }

        return this.Ok(modulesResponse);
    }

    [HttpGet("/jsapp/modules/list")]
    public async Task<IActionResult> ListJSAppModules()
    {
        var ccfClient = await this.CcfClientManager.GetGovClient();
        using HttpResponseMessage response = await ccfClient.GetAsync(
            $"gov/service/javascript-modules?" +
            $"api-version={this.CcfClientManager.GetGovApiVersion()}");
        await response.ValidateStatusCodeAsync(this.Logger);
        var content = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        return this.Ok(content);
    }

    [HttpGet("/jsapp/modules/{moduleName}")]
    public async Task<IActionResult> GetJSAppModule([FromRoute] string moduleName)
    {
        var ccfClient = await this.CcfClientManager.GetGovClient();
        using HttpResponseMessage response = await ccfClient.GetAsync(
            $"gov/service/javascript-modules/{moduleName}?api-version=" +
            $"{this.CcfClientManager.GetGovApiVersion()}");
        await response.ValidateStatusCodeAsync(this.Logger);
        var content = (await response.Content.ReadAsStringAsync())!;
        return this.Ok(content);
    }

    [HttpGet("/jsapp/bundle")]
    public async Task<IActionResult> GetJSAppBundle()
    {
        // There is not direct API to retrieve the original bundle that was submitted via set_jsapp.
        var ccfClient = await this.CcfClientManager.GetGovClient();
        var version = this.CcfClientManager.GetGovApiVersion();
        if (version == "2023-06-01-preview")
        {
            // For older CCF clusters get the bundle using the classic API as
            // javascript-modules API came with 5.0 GA release.
            return await this.GetJSAppBundleClassic();
        }

        JsonObject modules, endpoints;

        using (HttpResponseMessage response = await ccfClient.GetAsync(
            $"gov/service/javascript-modules?" +
            $"api-version={this.CcfClientManager.GetGovApiVersion()}"))
        {
            await response.ValidateStatusCodeAsync(this.Logger);
            modules = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        }

        using (HttpResponseMessage response = await ccfClient.GetAsync(
            $"gov/service/javascript-app?" +
            $"api-version={this.CcfClientManager.GetGovApiVersion()}"))
        {
            await response.ValidateStatusCodeAsync(this.Logger);
            endpoints = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        }

        List<string> moduleNames = new();
        List<Task<string>> fetchModuleTasks = new();
        foreach (var item in modules["value"]!.AsArray().AsEnumerable())
        {
            var moduleName = item!.AsObject()["moduleName"]!.ToString();
            moduleNames.Add(moduleName);
        }

        // Sort the module names in alphabetical order so that we return the response ordered by
        // name.
        moduleNames = moduleNames.OrderBy(x => x, StringComparer.Ordinal).ToList();
        foreach (var moduleName in moduleNames)
        {
            var escapedString = Uri.EscapeDataString(moduleName);
            Task<string> fetchModuleTask = ccfClient.GetStringAsync(
            $"gov/service/javascript-modules/{escapedString}?" +
            $"api-version={this.CcfClientManager.GetGovApiVersion()}");
            fetchModuleTasks.Add(fetchModuleTask);
        }

        var endpointsInProposalFormat = new JsonObject();
        foreach (KeyValuePair<string, JsonNode?> apiSpec in
            endpoints["endpoints"]!.AsObject().AsEnumerable())
        {
            // Need to transform the endpoints output to the format that the proposal expects.
            // "/contracts": {
            //   "GET": {
            //     "authnPolicies": [
            //       "member_cert",
            //       "user_cert"
            //     ],
            //     "forwardingRequired": "sometimes",
            //     "jsModule": "/endpoints/contracts.js",
            // =>
            // "/contracts": {
            //   "get": {
            //     "authn_policies": [
            //       "member_cert",
            //       "user_cert"
            //     ],
            //     "forwarding_required": "sometimes",
            //     "js_module": "endpoints/contracts.js",=>
            string api = apiSpec.Key;
            if (endpointsInProposalFormat[api] == null)
            {
                endpointsInProposalFormat[api] = new JsonObject();
            }

            foreach (KeyValuePair<string, JsonNode?> verbSpec in
                apiSpec.Value!.AsObject().AsEnumerable())
            {
                string verb = verbSpec.Key!.ToLower();
                var value = new JsonObject();
                foreach (var item3 in verbSpec.Value!.AsObject().AsEnumerable())
                {
                    var key = ToSnakeCase(item3.Key);

                    // open_api needs to openapi and not open_api to match bundle schema.
                    if (key == "open_api")
                    {
                        key = "openapi";
                    }

                    value[key] = item3.Value?.DeepClone();
                }

                // Remove leading / ie "js_module": "/foo/bar" => "js_module": "foo/bar"
                value["js_module"] = value["js_module"]!.ToString().TrimStart('/');

                endpointsInProposalFormat[api]!.AsObject()[verb] = value;

                static string ToSnakeCase(string input)
                {
                    return Regex.Replace(input, "([a-z])([A-Z])", "$1_$2").ToLower();
                }
            }
        }

        await Task.WhenAll(fetchModuleTasks);
        var modulesArray = new JsonArray();
        for (int i = 0; i < moduleNames.Count; i++)
        {
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
            string content = (await fetchModuleTasks[i])!;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
            modulesArray.Add(new JsonObject
            {
                ["name"] = moduleNames[i].TrimStart('/'),
                ["module"] = content
            });
        }

        return this.Ok(new JsonObject
        {
            ["metadata"] = new JsonObject
            {
                ["endpoints"] = endpointsInProposalFormat
            },
            ["modules"] = modulesArray
        });
    }

    private async Task<IActionResult> GetJSAppBundleClassic()
    {
        // There is not direct API to retrieve the original bundle that was submitted via set_jsapp.
        var ccfClient = await this.CcfClientManager.GetGovClient();
        JsonObject modules, endpoints;
        using (HttpResponseMessage response = await ccfClient.GetAsync($"gov/kv/modules"))
        {
            await response.ValidateStatusCodeAsync(this.Logger);
            modules = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        }

        using (HttpResponseMessage response = await ccfClient.GetAsync($"gov/kv/endpoints"))
        {
            await response.ValidateStatusCodeAsync(this.Logger);
            endpoints = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        }

        var endpointsInProposalFormat = new JsonObject();
        foreach (KeyValuePair<string, JsonNode?> item in endpoints!.AsEnumerable())
        {
            // Need to transform the endpoints output to the format that the proposal expects.
            // "GET /contracts": { "js_module": "/foo/bar", ...} =>
            // "/contracts": { "get": { "js_module": "foo/bar", ... }
            string[] parts = item.Key.Split(" ");
            string verb = parts[0]!.ToLower();
            string api = parts[1]!;
            if (endpointsInProposalFormat[api] == null)
            {
                endpointsInProposalFormat[api] = new JsonObject();
            }

            var value = item.Value!.DeepClone();

            // Remove leading / ie "js_module": "/foo/bar" => "js_module": "foo/bar"
            value["js_module"] = value["js_module"]!.ToString().TrimStart('/');

            // The /endpoints API is not returning mode value for PUT/POST. Need to fill it
            // or else proposal submission fails.
            if ((verb == "put" || verb == "post") && value["mode"] == null)
            {
                value["mode"] = "readwrite";
            }

            endpointsInProposalFormat[api]!.AsObject()[verb] = value;
        }

        var modulesArray = new JsonArray();
        foreach (KeyValuePair<string, JsonNode?> item in modules!.AsEnumerable())
        {
            modulesArray.Add(new JsonObject
            {
                ["name"] = item.Key.TrimStart('/'),
                ["module"] = item.Value?.DeepClone()
            });
        }

        return this.Ok(new JsonObject
        {
            ["metadata"] = new JsonObject
            {
                ["endpoints"] = endpointsInProposalFormat
            },
            ["modules"] = modulesArray
        });
    }

    public class ConfigView
    {
        public WorkspaceConfiguration? Config { get; set; } = default!;

        public System.Collections.IDictionary EnvironmentVariables { get; set; } = default!;
    }
}
