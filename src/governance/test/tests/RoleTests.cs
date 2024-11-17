// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test;

[TestClass]
public class RoleTests : TestBase
{
    /// <summary>
    /// Initialize tests.
    /// </summary>
    [TestInitialize]
    public override void Initialize()
    {
        string? testConfigurationFile = Environment.GetEnvironmentVariable(
            "TEST_CONFIGURATION_FILE");

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables();

        if (!string.IsNullOrEmpty(testConfigurationFile))
        {
            configBuilder.AddJsonFile(testConfigurationFile);
        }

        this.Configuration = configBuilder.Build();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        this.Logger = loggerFactory.CreateLogger<EventTests>();

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
            {
                return true;
            }
        };

        string contractId = Guid.NewGuid().ToString().Substring(0, 8);
        this.Logger.LogInformation($"contractId: {contractId}");
        this.ContractId = contractId;

        this.CcfClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(this.Configuration["roleTesting:ccfEndpoint"]!)
        };

        this.CgsClients = new List<HttpClient>();
        this.CgsClient_Member0 = new HttpClient(handler)
        {
            BaseAddress = new Uri(this.Configuration["roleTesting:cgsClientEndpoint_member0"]!)
        };
        this.CgsClients.Add(this.CgsClient_Member0);

        this.CgsClients.Add(new HttpClient(handler)
        {
            BaseAddress = new Uri(this.Configuration["roleTesting:cgsClientEndpoint_member1"]!)
        });

        this.CgsClients.Add(new HttpClient(handler)
        {
            BaseAddress = new Uri(this.Configuration["roleTesting:cgsClientEndpoint_member2"]!)
        });
    }

    [TestMethod]
    public async Task CheckContractOperatorActions()
    {
        string contractId = this.ContractId;
        await this.ProposeAndAcceptContract(contractId);

        // Enable the contract operator role for member1 along with enabling auto approval for
        // clean room policy/deployment spec proposals. This should result in member1's proposal
        // to get auto-accepted while any other member's proposal should remain open.

        // Enabling auto-approve deployment spec/clean room policy option.
        await this.SetRuntimeOption(RuntimeOption.AutoApproveDeploymentSpec, ActionName.Enable);
        await this.SetRuntimeOption(RuntimeOption.AutoApproveCleanRoomPolicy, ActionName.Enable);

        // Update member1 to become a contract_operator.
        await this.SetRole(Members.Member1, RoleName.ContractOperator, "true");

        // Now a clean room policy proposal from member0 should remain open while from member1
        // should get auto-accepted (no voting was required).
        string proposalId =
            await this.ProposeAllowAllCleanRoomPolicy(contractId, asMember: Members.Member0);
        var proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Open", proposalResponse["proposalState"]!.ToString());

        proposalId =
            await this.ProposeAllowAllCleanRoomPolicy(contractId, asMember: Members.Member1);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Accepted", proposalResponse["proposalState"]!.ToString());

        // Similar behavior for deployment spec.
        var specInput = new JsonObject
        {
            ["armTemplate"] = "something"
        };
        proposalId =
            await this.ProposeDeploymentSpec(contractId, specInput, asMember: Members.Member0);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Open", proposalResponse["proposalState"]!.ToString());

        proposalId =
            await this.ProposeDeploymentSpec(contractId, specInput, asMember: Members.Member1);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Accepted", proposalResponse["proposalState"]!.ToString());

        // Update member1 to no longer be a cgs_operator.
        await this.SetRole(Members.Member1, RoleName.ContractOperator, "false");

        // Now a proposal from member1 should remain open as role was disabled.
        proposalId =
            await this.ProposeAllowAllCleanRoomPolicy(contractId, asMember: Members.Member1);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Open", proposalResponse["proposalState"]!.ToString());

        proposalId =
            await this.ProposeDeploymentSpec(contractId, specInput, asMember: Members.Member1);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Open", proposalResponse["proposalState"]!.ToString());

        // Update member1 to again become be a cgs_operator.
        await this.SetRole(Members.Member1, RoleName.ContractOperator, "true");

        // Disabling the auto-approve options.
        await this.SetRuntimeOption(RuntimeOption.AutoApproveDeploymentSpec, ActionName.Disable);
        await this.SetRuntimeOption(RuntimeOption.AutoApproveCleanRoomPolicy, ActionName.Disable);

        // Now a proposal from member1 should remain open as auto-approve was disabled.
        proposalId =
            await this.ProposeAllowAllCleanRoomPolicy(contractId, asMember: Members.Member1);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Open", proposalResponse["proposalState"]!.ToString());

        proposalId =
            await this.ProposeDeploymentSpec(contractId, specInput, asMember: Members.Member1);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Open", proposalResponse["proposalState"]!.ToString());
    }

    [TestMethod]
    public async Task CheckCgsOperatorActions()
    {
        // Enable the cgs operator role for member2 along with enabling auto approval for
        // clean room policy proposals. This should result in member2's proposal to get
        // auto-accepted while any other member's proposal should remain open.

        // Enabling auto-approve constitution/jsapp proposals.
        await this.SetRuntimeOption(RuntimeOption.AutoApproveConstitution, ActionName.Enable);
        await this.SetRuntimeOption(RuntimeOption.AutoApproveJsApp, ActionName.Enable);

        // Update member2 to become a cgs_operator.
        await this.SetRole(Members.Member2, RoleName.CgsOperator, "true");

        // Now a JS app proposal from member0 should remain open while from member2
        // should get auto-accepted (no voting was required).
        string proposalId = await this.ProposeJsApp(asMember: Members.Member0);
        var proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Open", proposalResponse["proposalState"]!.ToString());

        proposalId = await this.ProposeJsApp(asMember: Members.Member2);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Accepted", proposalResponse["proposalState"]!.ToString());

        // Similar behavior for set constitution.
        proposalId = await this.ProposeConstitution(asMember: Members.Member0);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Open", proposalResponse["proposalState"]!.ToString());

        proposalId = await this.ProposeConstitution(asMember: Members.Member2);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Accepted", proposalResponse["proposalState"]!.ToString());

        // Update member2 to no longer be a cgs_operator.
        await this.SetRole(Members.Member2, RoleName.CgsOperator, "false");

        // Now a proposal from member2 should remain open as role was disabled.
        proposalId = await this.ProposeJsApp(asMember: Members.Member2);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Open", proposalResponse["proposalState"]!.ToString());

        proposalId =
            await this.ProposeConstitution(asMember: Members.Member2);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Open", proposalResponse["proposalState"]!.ToString());

        // Update member2 to again become be a cgs_operator.
        await this.SetRole(Members.Member2, RoleName.CgsOperator, "true");

        // Disabling the auto-approve options.
        await this.SetRuntimeOption(RuntimeOption.AutoApproveJsApp, ActionName.Disable);
        await this.SetRuntimeOption(RuntimeOption.AutoApproveConstitution, ActionName.Disable);

        // Now a proposal from member2 should remain open as auto-approve was disabled.
        proposalId = await this.ProposeJsApp(asMember: Members.Member2);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Open", proposalResponse["proposalState"]!.ToString());

        proposalId =
            await this.ProposeConstitution(asMember: Members.Member2);
        proposalResponse =
            (await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>(
                $"proposals/{proposalId}"))!;
        Assert.AreEqual("Open", proposalResponse["proposalState"]!.ToString());
    }

    private async Task SetRole(int asMember, string roleName, string value)
    {
        var info = await this.CgsClients[asMember].GetFromJsonAsync<JsonObject>("/show");
        string memberId = info!["memberId"]!.ToString();

        using (HttpRequestMessage request = new(HttpMethod.Post, "proposals/create"))
        {
            var proposalContent = new JsonObject
            {
                ["actions"] = new JsonArray
                {
                    {
                        new JsonObject
                        {
                            ["name"] = "set_member_data",
                            ["args"] = new JsonObject
                            {
                                ["member_id"] = memberId,
                                ["member_data"] = new JsonObject
                                {
                                    ["cgs_roles"] = new JsonObject
                                    {
                                        [roleName] = value
                                    }
                                }
                            }
                        }
                    }
                }
            };

            request.Content = new StringContent(
                proposalContent.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await this.CgsClient_Member0.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responseBody = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            string proposalId = responseBody[ProposalIdKey]!.ToString();
            await this.AllMembersAcceptProposal(proposalId);

            info = await this.CgsClients[asMember].GetFromJsonAsync<JsonObject>("/show");
            Assert.AreEqual(
                value,
                info!["memberData"]!["cgs_roles"]![roleName]!.ToString());
        }
    }

    private async Task SetRuntimeOption(string optionName, string actionName)
    {
        string checkOptionStatusUrl = $"runtimeoptions/checkstatus/{optionName}";
        string setUrl = $"runtimeoptions/{optionName}/propose-{actionName}";

        string proposalId;
        using (HttpRequestMessage request = new(HttpMethod.Post, setUrl))
        {
            using HttpResponseMessage response = await this.CgsClient_Member0.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responseBody = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            proposalId = responseBody[ProposalIdKey]!.ToString();
        }

        await this.AllMembersAcceptProposal(proposalId);

        // Status should now be reported as enabled.
        using (HttpRequestMessage request = new(HttpMethod.Post, checkOptionStatusUrl))
        {
            using HttpResponseMessage response = await this.CgsClient_Member0.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var statusResponse =
                (await response.Content.ReadFromJsonAsync<StatusWithReasonResponse>())!;
            var exepctedStatus = actionName == ActionName.Enable ? "enabled" :
                actionName == ActionName.Disable ? "disabled" :
                throw new ArgumentException($"Unsupported actionName: {actionName}");
            Assert.AreEqual(exepctedStatus, statusResponse.Status);
        }
    }

    private async Task<string> ProposeJsApp(int asMember = Members.Member0)
    {
        // Fetch the current app and propose that back as-is.
        var bundle = await this.CgsClient_Member0.GetFromJsonAsync<JsonObject>("jsapp/bundle");

        string proposalId;
        using (HttpRequestMessage request = new(HttpMethod.Post, "proposals/create"))
        {
            var proposalContent = new JsonObject
            {
                ["actions"] = new JsonArray
                {
                    {
                        new JsonObject
                        {
                            ["name"] = "set_js_app",
                            ["args"] = new JsonObject
                            {
                                ["bundle"] = bundle
                            }
                        }
                    }
                }
            };

            request.Content = new StringContent(
                proposalContent.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await this.CgsClients[asMember].SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responseBody = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            proposalId = responseBody[ProposalIdKey]!.ToString();
        }

        return proposalId;
    }

    private async Task<string> ProposeConstitution(int asMember = Members.Member0)
    {
        // Fetch the current constitution propose that back as-is.
        var constitution = await this.CgsClient_Member0.GetStringAsync("constitution");

        string proposalId;
        using (HttpRequestMessage request = new(HttpMethod.Post, "proposals/create"))
        {
            var proposalContent = new JsonObject
            {
                ["actions"] = new JsonArray
                {
                    {
                        new JsonObject
                        {
                            ["name"] = "set_constitution",
                            ["args"] = new JsonObject
                            {
                                ["constitution"] = constitution
                            }
                        }
                    }
                }
            };

            request.Content = new StringContent(
                proposalContent.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await this.CgsClients[asMember].SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responseBody = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            proposalId = responseBody[ProposalIdKey]!.ToString();
        }

        return proposalId;
    }

    public static class RuntimeOption
    {
        public const string AutoApproveCleanRoomPolicy = "autoapprove-cleanroompolicy-proposal";
        public const string AutoApproveDeploymentSpec = "autoapprove-deploymentspec-proposal";
        public const string AutoApproveConstitution = "autoapprove-constitution-proposal";
        public const string AutoApproveJsApp = "autoapprove-jsapp-proposal";
    }

    public static class ActionName
    {
        public const string Enable = "enable";
        public const string Disable = "disable";
    }

    public static class RoleName
    {
        public const string CgsOperator = "cgs_operator";
        public const string ContractOperator = "contract_operator";
    }
}