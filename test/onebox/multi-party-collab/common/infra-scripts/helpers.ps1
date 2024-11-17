function CheckLastExitCode() {
    if ($LASTEXITCODE -gt 0) { exit 1 }
}