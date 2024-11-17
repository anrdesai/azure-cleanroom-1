[CmdletBinding()]
param
(
    [string]
    $recoveryServiceName = "",

    [string]
    $recoveryMemberName = "",

    [string]
    $governanceClient = "ccf-provider-governance"
)

#https://learn.microsoft.com/en-us/powershell/scripting/learn/experimental-features?view=powershell-7.4#psnativecommanderroractionpreference
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$sandbox_common = "$PSScriptRoot/sandbox_common"
$ccf = $(Get-Content $sandbox_common/ccf.json | ConvertFrom-Json)
$infraType = $ccf.infraType
$recSvcConfigFile = "$sandbox_common/$recoveryServiceName-recoveryServiceConfig.json"

$symbols = [PSCustomObject] @{
    CHECKMARK = ([char]8730)
}

if ($recoveryServiceName -eq "") {
    $recoveryServiceName = $ccf.name
}

$recSvc = (az cleanroom ccf recovery-service show `
        --name $recoveryServiceName `
        --infra-type $infraType `
        --provider-config $sandbox_common/providerConfig.json | ConvertFrom-Json)
$rsvcConfig = @{}
$rsvcConfig.recoveryService = @{}
$rsvcConfig.recoveryService.endpoint = $recSvc.endpoint
$rsvcConfig.recoveryService.serviceCert = $recSvc.serviceCert
$rsvcConfig | ConvertTo-Json -Depth 100 > $recSvcConfigFile

$recoveryServiceReport = (az cleanroom ccf recovery-service api show-report `
        --service-config $recSvcConfigFile | ConvertFrom-Json)

$members = (az cleanroom governance member show `
        --governance-client $governanceClient | ConvertFrom-Json)
$verifiedMembers = 0
$checkMark = $symbols.CHECKMARK
$crossMark = "X"

# For all recovery operator members check that:
# - memberData.recovery_service.hostData matches specified <recovery service>/report.hostData value
# - memberData.recovery_service.hostData matches specified <recovery service>/member/hostData value
# - The signing cert in CCF for the recovery operator matches the member cert in the recovery service
# - The encryption public key in CCF for the recovery operator matches the member cert in the recovery service
foreach ($item in $members.PSObject.Properties) {
    $m = $item.Value
    $failures = @()
    if ($m.member_data.is_recovery_operator -eq $true) {
        if ($recoveryMemberName -ne "" -and $m.member_data.identifier -ne $recoveryMemberName) {
            continue
        }

        Write-Output "Verifying member $($m.member_data.identifier)..."
        $memberInfo = (az cleanroom ccf recovery-service api member show `
                --member-name $m.member_data.identifier `
                --service-config $recSvcConfigFile | ConvertFrom-Json)
        $memberDataHostData = $m.member_data.recovery_service.hostData
        $mark = $checkMark
        if ($null -eq $memberDataHostData) {
            $failures += "$($m.member_data.identifier) memberData.hostData is not set"
            $mark = $crossMark
        }
        elseif ($memberInfo.recoveryService.hostData -ne $memberDataHostData) {
            $failures += "$($m.member_data.identifier) memberData.hostData value '$memberDataHostData' " +
            "does not match recovery service member's value '$($memberInfo.recoveryService.hostData)'"
            $mark = $crossMark
        }
        Write-Output "Member's hostData value matches member present in recovery service $mark"

        $mark = $checkMark
        if ($null -eq $recoveryServiceReport.hostData) {
            $failures += "Recovery service's report.hostData is not set"
            $mark = $crossMark
        }
        elseif ($recoveryServiceReport.hostData -ne $memberDataHostData) {
            $failures += "$($m.member_data.identifier) hostData value '$memberDataHostData' " +
            "does not match recovery service's report.hostData value '$($recoveryServiceReport.hostData)'"
            $mark = $crossMark
        }
        Write-Output "Member's hostData value matches recovery service's hostData value $mark"

        $mark = $checkMark
        if ($m.cert.TrimEnd("`n") -ne $memberInfo.signingCert) {
            $failures += "signing cert mismatch for $($m.member_data.identifier) between CCF and recovery service"
            $mark = $crossMark
        }
        Write-Output "Signing cert matches value present in recovery service $mark"

        $mark = $checkMark
        if ($m.public_encryption_key.TrimEnd("`n") -ne $memberInfo.encryptionPublicKey) {
            $failures += "encryption key mismatch for $($m.member_data.identifier) between CCF and recovery service"
            $mark = $crossMark
        }
        Write-Output "Encryption key matches value present in recovery service $mark"

        if ($failures.Count -eq 0) {
            Write-Output "$($m.member_data.identifier) verified successfully."
            $verifiedMembers++
        }
        else {
            Write-Output "$($m.member_data.identifier) failed verification: $($failures | ConvertTo-Json)."
            throw "Recovery operator verification failed."
        }
    }
}

if ($recoveryMemberName -ne "" -and $verifiedMembers -eq 0) {
    throw "Did not find any recovery operator '$recoveryMemberName' in recovery service '$recoveryServiceName'."
}

Write-Output "$verifiedMembers recovery operator member(s) verified."