// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;

public class BackgroundWorkerQueue
{
    private ConcurrentQueue<Func<Task>> workItems = new();
    private SemaphoreSlim signal = new(0);

    public async Task<Func<Task>> DequeueAsync(CancellationToken token)
    {
        await this.signal.WaitAsync(token);
        this.workItems.TryDequeue(out var workItem);
        return workItem!;
    }

    public void QueueBackgroundWorkItem(Func<Task> workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        this.workItems.Enqueue(workItem);
        this.signal.Release();
    }
}
