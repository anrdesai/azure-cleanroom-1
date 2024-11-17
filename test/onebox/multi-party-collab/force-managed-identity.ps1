function Force-Managed-Identity {
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory)]
        [string]$deploymentTemplateFile,

        [Parameter(Mandatory)]
        [string[]]$managedIdentities
    )

    #
    # Modify ARM template to use Managed Identity directly (without federation) due to MSFT
    # tenant policy restrictions.
    #
    $template = Get-Content $deploymentTemplateFile | ConvertFrom-Json -AsHashtable

    # Patch identity sidecar config to use MI.
    # x = ["resources"]["properties"]["containers"][x where x["name"] == "identity-sidecar"]
    # y = x["properties"]["environmentVariables"][y where y["name"] == "IdentitySideCarArgs"]
    # config = base64.decode(y["value"])
    # config.replace("ApplicationIdentities", "ManagedIdentities")
    # y["value"] = base64.encode(config)
    $identityContainer = $template.resources.properties.containers.where({ $_.name -eq "identity-sidecar" })
    $identityArgs = $identityContainer.properties.environmentVariables.where({ $_.name -eq "IdentitySideCarArgs" })
    $identityConfig = [Text.Encoding]::Utf8.GetString([Convert]::FromBase64String($identityArgs.value))
    $identityConfig = $identityConfig.replace("ApplicationIdentities", "ManagedIdentities")
    $identityArgs[0].value = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($identityConfig))

    # Add user assigned MIs to the ARM deployment template.
    $userAssignedIdentities = @{}
    foreach ($mi in $managedIdentities) {
        $userAssignedIdentities["$mi"] = @{}
    }

    $template.resources[0].identity = @{
        type                   = "UserAssigned"
        userAssignedIdentities = $userAssignedIdentities
    }

    # Write out patched json.
    $template | ConvertTo-Json -Depth 100 > $deploymentTemplateFile
}