// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
public class CAController : ClientControllerBase
{
    public CAController(
        ILogger<CAController> logger,
        IHttpContextAccessor httpContextAccessor)
        : base(logger, httpContextAccessor)
    {
    }

    [HttpPost("/contracts/{contractId}/ca/generateSigningKey")]
    public async Task<JsonObject> GenerateSigningKey([FromRoute] string contractId)
    {
        var appClient = this.CcfClientManager.GetAppClient();
        using (HttpRequestMessage request =
            new(HttpMethod.Post, $"app/contracts/{contractId}/ca/generateSigningKey"))
        {
            using HttpResponseMessage response = await appClient.SendAsync(request);
            this.Response.CopyHeaders(response.Headers);
            await response.ValidateStatusCodeAsync(this.Logger);
            await response.WaitAppTransactionCommittedAsync(this.Logger, this.CcfClientManager);
            this.Response.StatusCode = (int)response.StatusCode;
            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonObject>();

            // TODO (gsinha): Remove this workaround once CCF supports certificate generation.
            var cacertFile = await this.GenerateCACertificateForCgs(contractId);
            await this.UpdateCACertificateInCgs(contractId, cacertFile);

            return jsonResponse!;
        }
    }

    [HttpPost("/contracts/{contractId}/ca/exposeSigningKey")]
    public async Task<JsonObject> ExposeSigningKey([FromRoute] string contractId)
    {
        var appClient = this.CcfClientManager.GetAppClient();
        using (HttpRequestMessage request =
            new(HttpMethod.Post, $"app/contracts/{contractId}/ca/exposeSigningKey"))
        {
            using HttpResponseMessage response = await appClient.SendAsync(request);
            this.Response.CopyHeaders(response.Headers);
            await response.ValidateStatusCodeAsync(this.Logger);
            await response.WaitAppTransactionCommittedAsync(this.Logger, this.CcfClientManager);
            this.Response.StatusCode = (int)response.StatusCode;
            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
            return jsonResponse!;
        }
    }

    [HttpGet("/contracts/{contractId}/ca/info")]
    public async Task<JsonObject> GetInfo([FromRoute] string contractId)
    {
        var appClient = this.CcfClientManager.GetAppClient();
        using (HttpRequestMessage request =
            new(HttpMethod.Get, $"app/contracts/{contractId}/ca/info"))
        {
            using HttpResponseMessage response = await appClient.SendAsync(request);
            this.Response.CopyHeaders(response.Headers);
            await response.ValidateStatusCodeAsync(this.Logger);
            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
            return jsonResponse!;
        }
    }

    private async Task<string> GenerateCACertificateForCgs(string contractId)
    {
        var appClient = this.CcfClientManager.GetAppClient();
        using (HttpRequestMessage request = new(
            HttpMethod.Post,
            $"app/contracts/{contractId}/ca/exposeSigningKey"))
        {
            using HttpResponseMessage response = await appClient.SendAsync(request);
            await response.ValidateStatusCodeAsync(this.Logger);
            var jsonResponse = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            string privateKeyPem = jsonResponse["privateKey"]!.ToString();
            string privateKeyFile = Path.Combine(Path.GetTempPath(), $"{contractId}-privk.pem");
            string caCertFile = Path.Combine(Path.GetTempPath(), $"{contractId}-cert.pem");
            await System.IO.File.WriteAllTextAsync(privateKeyFile, privateKeyPem);
            try
            {
                await this.Bash($"Scripts/gencacert.sh --privk {privateKeyFile} --out {caCertFile}");
            }
            finally
            {
                System.IO.File.Delete(privateKeyFile);
            }

            return caCertFile;
        }
    }

    private async Task UpdateCACertificateInCgs(string contractId, string caCertFile)
    {
        var appClient = this.CcfClientManager.GetAppClient();
        using (HttpRequestMessage request = new(
            HttpMethod.Post,
            $"app/contracts/{contractId}/ca/updateCert"))
        {
            var caCertPem = await System.IO.File.ReadAllTextAsync(caCertFile);
            var content = new JsonObject
            {
                ["caCert"] = caCertPem
            };
            request.Content = new StringContent(
                content.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await appClient.SendAsync(request);
            await response.ValidateStatusCodeAsync(this.Logger);
        }
    }

    private Task<int> Bash(string cmd)
    {
        var source = new TaskCompletionSource<int>();
        var escapedArgs = cmd.Replace("\"", "\\\"");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{escapedArgs}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };
        process.Exited += (sender, args) =>
        {
            this.Logger.LogWarning(process.StandardError.ReadToEnd());
            this.Logger.LogInformation(process.StandardOutput.ReadToEnd());
            if (process.ExitCode == 0)
            {
                source.SetResult(0);
            }
            else
            {
                source.SetException(new Exception($"Command `{cmd}` failed with exit code " +
                    $"`{process.ExitCode}`"));
            }

            process.Dispose();
        };

        try
        {
            process.Start();
        }
        catch (Exception e)
        {
            this.Logger.LogError(e, "Command {} failed", cmd);
            source.SetException(e);
        }

        return source.Task;
    }
}
