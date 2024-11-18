// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CcfProvider;

public class OrasClient
{
    private ILogger logger;

    public OrasClient(ILogger logger)
    {
        this.logger = logger;
    }

    public async Task Pull(string registryUrl, string outDir)
    {
        await this.Bash($"scripts/oras-pull.sh --registryUrl {registryUrl} --out {outDir}");
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
            this.logger.LogWarning(process.StandardError.ReadToEnd());
            this.logger.LogInformation(process.StandardOutput.ReadToEnd());
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
            this.logger.LogError(e, "Command {} failed", cmd);
            source.SetException(e);
        }

        return source.Task;
    }
}
