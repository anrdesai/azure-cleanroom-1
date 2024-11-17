function Replace-Strings {
    param (
        [string]$filePath,
        
        [hashtable]$replacements
    )

    $fileContent = Get-Content -Path $((Resolve-Path $filePath).Path)
    $content = ''
    foreach ($line in $fileContent) {
        $content = $content + [environment]::NewLine + $line
    }

    foreach ($key in $replacements.Keys) {
        $content = $content -replace $key, $replacements[$key]
    }
    return $content
}

function CheckLastExitCode() {
    if ($LASTEXITCODE -gt 0) { exit 1 }
}

function Get-SidecarDigestsContainerRegistryUrl {

    if ([System.String]::IsNullOrEmpty($env:SIDECAR_DIGESTS_CONTAINER_REGISTRY_URL)) {
        return "mcr.microsoft.com/cleanroom"
    }
    else {
        return $env:SIDECAR_DIGESTS_CONTAINER_REGISTRY_URL
    }
}

function Get-ContainerDigestTag {

    if ([System.String]::IsNullOrEmpty($env:SIDECAR_DIGESTS_TAG)) {
        return "latest"
    }
    else {
        return $env:SIDECAR_DIGESTS_TAG
    }
}

function Get-CgsContainerRegistryUrl {
    [CmdletBinding()]
    param (
        [Parameter()]
        [string]
        $containerName
    )

    $envVariableMapping = @{
        "cgs-client"       = $env:CLIENT_CONTAINERS_REGISTRY_URL
        "cgs-ui"           = $env:CLIENT_CONTAINERS_REGISTRY_URL
        "cgs-js-app"       = $env:CGS_JS_APP_CONTAINER_REGISTRY_URL
        "cgs-constitution" = $env:CGS_CONSTITUTION_CONTAINER_REGISTRY_URL
    }

    if ([System.String]::IsNullOrEmpty($($envVariableMapping."$containerName"))) {
        return "mcr.microsoft.com/cleanroom"
    }
    else {
        return $envVariableMapping."$containerName"
    }
}

function Get-VersionsDocumentTag {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]
        $containerName
    )

    $envVariableMapping = @{
        "cgs-client"       = $env:CLIENT_CONTAINERS_TAG
        "cgs-ui"           = $env:CLIENT_CONTAINERS_TAG
        "cgs-js-app"       = $env:CGS_JS_APP_TAG
        "cgs-constitution" = $env:CGS_CONSTITUTION_TAG
    }

    if ([System.String]::IsNullOrEmpty($($envVariableMapping."$containerName"))) {
        return "latest"
    }
    else {
        return $envVariableMapping."$containerName"
    }
}

function Get-CgsImage {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]
        $outDir,

        [Parameter(Mandatory = $true)]
        [string]
        $containerName
    )

    $containerRegistryUrl = Get-CgsContainerRegistryUrl -containerName $containerName
    Write-Host "Pulling versions for $containerName from $cgsClientContainerRegistryUrl"
    $tag = Get-VersionsDocumentTag -containerName $containerName
    oras pull "$containerRegistryUrl/versions/${containerName}:$tag" -o $outDir --allow-path-traversal

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Fetching versions failed, defaulting to version 1.0.6"
        return "mcr.microsoft.com/cleanroom/${containerName}:1.0.6"
    }
    else {
        $image = $(Get-Content $outDir/version.yml | ConvertFrom-Yaml)."$containerName".image
        return $image
    }
}