// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

public class LongRunningService : BackgroundService
{
    private readonly BackgroundWorkerQueue queue;

    public LongRunningService(BackgroundWorkerQueue queue)
    {
        this.queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await this.queue.DequeueAsync(stoppingToken);

            // Don't want to wait so that we can kick off the work items in parallel.
            _ = Task.Run(async () => await workItem());
        }
    }
}