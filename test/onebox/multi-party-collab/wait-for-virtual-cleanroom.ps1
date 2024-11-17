[CmdletBinding()]
param
(
    [Parameter(Mandatory)]
    [string]
    $containerName
)

function Get-TimeStamp {
    return "[{0:MM/dd/yy} {0:HH:mm:ss}]" -f (Get-Date)
}

do {
    kubectl get pod virtual-cleanroom

    $codeLauncherStatus = kubectl get pod virtual-cleanroom `
        -o=jsonpath="{.status.containerStatuses[?(@.name==`"$containerName`")]}"
    if ($codeLauncherStatus -ne "" -and $null -ne $codeLauncherStatus) {
        $status = $codeLauncherStatus | ConvertFrom-Json
        if ($status.state.PSObject.Properties.Name -contains "terminated") {
            Write-Host -ForegroundColor Yellow "$(Get-TimeStamp) Code launcher is terminated. Checking exit code."
            if ($status.state.terminated.exitCode -ne 0) {
                Write-Host -ForegroundColor DarkRed "$(Get-TimeStamp) Code launcher exited with non-zero exit code $exitCode"
                exit $exitCode
            }

            Write-Host -ForegroundColor DarkGreen "$(Get-TimeStamp) Code launcher exited successfully."
            exit 0
        }

        if ($status.state.PSObject.Properties.Name -contains "running") {
            Write-Host -ForegroundColor Green "$(Get-TimeStamp) Code launcher is running..."
        }
        else {
            Write-Host -ForegroundColor Green "$(Get-TimeStamp) Code launcher is in state:"
            Write-Host ($status.state | ConvertTo-Json)
        }
    }
    else {
        Write-Host "$(Get-TimeStamp) code-launcher container status was not found."
    }

    Write-Host "Waiting for 5 seconds before checking status again..."
    Start-Sleep -Seconds 5
} while ($true)

