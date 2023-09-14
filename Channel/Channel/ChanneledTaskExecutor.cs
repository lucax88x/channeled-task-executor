namespace Channel;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public class ChanneledTaskExecutor
{
    public async Task Run(TimeSpan delay, TimeSpan timeoutExpectations, int maxParallelJobs, params Func<Task>[] jobs)
    {
        var timeoutExpiration = TimeSpan.FromMinutes(4);

        var channel = Channel.CreateBounded<Func<Task>>(new BoundedChannelOptions(maxParallelJobs)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        // timeout?
        var taskCompletionSources = jobs
            .Select(_ => new TaskCompletionSource())
            .ToList();

        var queue = new Queue<Func<Task>>(jobs);

        await using var timer = new Timer(_ =>
            {
                while (queue.Count > 0)
                {
                    var func = queue.Dequeue();

                    if (channel.Writer.TryWrite(func)) Console.WriteLine($"Enqueued job at {DateTimeOffset.Now}");
                }
            }, null,
            TimeSpan.Zero,
            delay);

        await foreach (var func in channel.Reader.ReadAllAsync())
        {
            Console.WriteLine($"Job started at {DateTime.Now}");

            await func();

            Console.WriteLine($"Job completed at {DateTime.Now}");
        }

        // Complete the channel and wait for all consumers to finish
        channel.Writer.Complete();

        // try
        // {
        //     response = await taskCompletionSource.Task.WaitAsync(timeoutExpiration);
        // }
        // catch (TimeoutException)
        // {
        //     Console.WriteLine("Timeout");
        // }

        // await taskCompletionSources.ElementAt(0).
        // await Task.WhenAll(jobs);
    }
}