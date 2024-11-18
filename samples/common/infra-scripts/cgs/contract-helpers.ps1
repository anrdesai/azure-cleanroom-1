Import-Module $PSScriptRoot/../../../governance/scripts/cgs.psm1 -Force -DisableNameChecking

function Propose-Cleanroom-Spec {
    [CmdletBinding()]
    param (
        [string]$cleanroomSpecPath,
        [string]$contractId,
        [string]$clientPort
    )

    Write-Host "Checking if contract already exists"
    $contract = Get-Contract -id $contractId -port $clientPort | ConvertFrom-Json
    $cleanroomSpec = Get-Content -Raw $cleanroomSpecPath

    if ($null -ne $contract.error -and $contract.error.code -eq "ContractNotFound")
    {
        Write-Host "Creating contract $contractId"
        $contractCreationResult = Create-Contract -data "$cleanroomSpec" -id $contractId -port $clientPort

        Write-Host "$contractCreationResult"

        $contract = (Get-Contract -id $contractId -port $clientPort | ConvertFrom-Json)
    }
    elseif ($null -ne $contract.data)
    {
        Write-Host "Contract $contractId exists, continuing"
    }

    if ($contract.state -eq "Draft")
    {
        $proposal = (Propose-Contract -version $contract.version -id $contractId -port $clientPort |
            ConvertFrom-Json)
        Write-Host $proposal

        $acceptResult = Accept-Cleanroom-Spec -contractId $contractId -clientPort $clientPort
        Write-Host "$acceptResult"
    }
}

function Accept-Cleanroom-Spec {
    [CmdletBinding()]
    param (
        [string]$contractId,
        [string]$clientPort
    )

    $contract = $(Get-Contract -id $contractId -port $clientPort | ConvertFrom-Json)

    if ($contract.state -ne "Accepted")
    {
        if (Validate-Cleanroom-Spec -contract $contract -clientPort $clientPort)
        {
            Vote-Contract -id $contractId -proposalId $contract.proposalId -vote accept -port $clientPort
        }
        else
        {
            Write-Host -ForegroundColor Red "Validation failed for cleanroom specification"
            exit 1
        }
    }
    else
    {
        Write-Host "Skipping voting as contract has already been accepted"
    }

}

function Validate-Cleanroom-Spec {
    [CmdletBinding()]
    param (
        [string]$contract,
        [string]$clientPort
    )

    if ($null -ne $contract)
    {
        return $true
    }

    return $false
}

function Get-Cleanroom-Spec {
    [CmdletBinding()]
    param (
        [string]$contractId,
        [string]$clientPort
    )

    $contract = (Get-Contract -id $contractId -port $clientPort | ConvertFrom-Json)
    return $contract
}

function Propose-Arm-Template {
    [CmdletBinding()]
    param (
        [string]$contractId,
        [string]$specFilePath,
        [string]$clientPort
    )

    Write-Host "Checking if ARM template has already been proposed"
    $deploymentSpec = Get-DeploymentSpec -contractId $contractId -port $clientPort | ConvertFrom-Json

    if ($deploymentSpec.proposalIds.Length -gt 0 -or ($deploymentSpec.data | ConvertTo-Json) -ne "{}")
    {
        Write-Host "A deployment spec has already been proposed/accepted, continuing"
    }
    else
    {
        $proposal = $(Propose-DeploymentSpec-From-File -contractId $contractId -specFilePath $specFilePath  -port $clientPort | ConvertFrom-Json)

        $vote = Vote-Proposal -proposalId $proposal.proposalId -vote accept -port $clientPort
    }
}

function Get-Arm-Template {
    [CmdletBinding()]
    param (
        [string]$contractId,
        [string]$clientPort
    )

    $deploymentSpec = (Get-DeploymentSpec -contractId $contractId -port $clientPort | ConvertFrom-Json)
    CheckLastExitCode

    return $deploymentSpec.data
}

function Get-Arm-Template-Proposal {
    [CmdletBinding()]
    param (
        [string]$contractId,
        [string]$clientPort
    )

    $deploymentSpec = (Get-DeploymentSpec -contractId $contractId -port $clientPort | ConvertFrom-Json)

    if ($deploymentSpec.proposalIds.Length -eq 0)
    {
        Write-Host "Deployment spec has no open proposals"
        return "", $($deploymentSpec.data)
    }
    elseif ($deploymentSpec.proposalIds.Length -gt 1)
    {
        Write-Host -ForegroundColor Red "Multiple proposals found"
        exit 1
    }
    else
    {
        $proposal = (Get-Proposal -proposalId $deploymentSpec.proposalIds[0] -port $clientPort | ConvertFrom-Json)
        return ($proposal.proposalId, $proposal.actions.args.spec.data)
    }
}

function Get-Cce-Policy-Proposal {
    [CmdletBinding()]
    param (
        [string]$contractId,
        [string]$clientPort
    )

    $cleanroomPolicy = (Get-CleanRoom-Policy -contractId $contractId -port $clientPort | ConvertFrom-Json)

    if ($cleanroomPolicy.proposalIds.Length -eq 0)
    {
        Write-Host "Cleanroom policy has no open proposals"
        return "", $($cleanroomPolicy.policy)
    }
    elseif ($cleanroomPolicy.proposalIds.Length -gt 1)
    {
        Write-Host -ForegroundColor Red "Multiple proposals found"
        exit 1
    }
    else
    {
        $proposal = (Get-Proposal -proposalId $cleanroomPolicy.proposalIds[0] -port $clientPort | ConvertFrom-Json)
        return ($proposal.proposalId, $proposal.actions.args.claims)
    }
    
}

function Propose-Cce-Policy {
    [CmdletBinding()]
    param (
        [string]$contractId,
        [string]$ccePolicyHash,
        [string]$clientPort
    )

    $policyJson = @"
    {
      "type": "add",
      "claims": {
        "x-ms-sevsnpvm-hostdata": "$ccePolicyHash"
      }
    }
"@
    Write-Host "Checking if CCE policy has already been proposed"
    $policy = Get-CleanRoom-Policy -contractId $contractId -port $clientPort | ConvertFrom-Json

    if ($policy.proposalIds.Length -gt 0 -or ($policy.policy | ConvertTo-Json) -ne "{}")
    {
        Write-Host "A cleanroom policy has already been proposed/accepted, continuing"
    }
    else
    {
        $proposal = $(Propose-CleanRoom-Policy -contractId $contractId -policy $policyJson -port $clientPort | ConvertFrom-Json)
        $vote = Vote-Proposal -proposalId $proposal.proposalId -vote accept -port $clientPort
    }
}

function Verify-Arm-Template {
    [CmdletBinding()]
    param (
        [string]$armTemplatePath,
        [string]$hostData
    )

    # TODO (anrdesai): Add cce policy based validation here.
    if ($armTemplatePath -ne ""-and $hostData -ne "")
    {
        return $true
    }
    return $false
}

function Verify-Cce-Policy {
    [CmdletBinding()]
    param (
        [string]$armTemplatePath,
        [string]$hostData
    )

    # TODO (anrdesai): Add cce policy based validation here.
    if ($armTemplatePath -ne ""-and $hostData -ne "")
    {
        return $true
    }
    return $false
}

function Accept-Arm-Template-Proposal {
    [CmdletBinding()]
    param (
        [string]$armTemplateProposalId,
        [string]$clientPort
    )

    Vote-Proposal -proposalId $armTemplateProposalId -vote accept -port $clientPort
}

function Accept-Cleanroom-Policy {
    [CmdletBinding()]
    param (
        [string]$cleanroomPolicyProposalId,
        [string]$clientPort
    )

    Vote-Proposal -proposalId $cleanroomPolicyProposalId -vote accept -port $clientPort
}