function Generate-MemberCert-And-Keys {
    [CmdletBinding()]
    param
    (
        [string]$memberName
    )

    $outDir = "$($MyInvocation.PSScriptRoot)/sandbox_common"

    if (Test-Path $outDir/$($memberName)_cert.pem)
    {
        Write-Host "Skipping creation of private key and certificate for $memberName. Reusing existing"
    }
    else 
    {
        Write-Host "Generating member certificate and key for $memberName"
        $path = $(Resolve-Path $PSScriptRoot/../../../governance/keygenerator.sh)
        $result = bash $path --name $memberName --out $outDir --gen-enc-key
    }

    $certPemBase64 = cat "$outDir/$($memberName)_cert.pem" | base64 --wrap=0
    return $certPemBase64
}

function Invite-Collaborator {
    [CmdletBinding()]
    param (
        [string]$memberCertPemBase64,
        [string]$identifier,
        [string]$tenantId,
        [string]$clientPort
    )

    $outDir = "$($MyInvocation.PSScriptRoot)/sandbox_common"
    $memberCertBytes = [System.Convert]::FromBase64String($memberCertPemBase64)
    $memberCertPem = [System.Text.Encoding]::UTF8.GetString($memberCertBytes)
    $memberCertPem | Out-File "$outDir/$($identifier)_cert.pem"

    Write-Host "Adding member $identifier to the consortium"
    Add-Member -memberCertPemFilePath "$outDir/$($identifier)_cert.pem" `
        -identifier $identifier `
        -tenantId $tenantId `
        -port $clientPort
}

function Accept-Invite {
    [CmdletBinding()]
    param (
        [string]$clientPort
    )

    Activate-Current-Member -port $clientPort
}