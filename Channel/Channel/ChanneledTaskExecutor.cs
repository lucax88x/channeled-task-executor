namespace Channel;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public record ExecutorOpts(TimeSpan DelayBetweenJobs, TimeSpan MaxExecutionTime, int MaxParallelJobs);

public static class ChanneledTaskExecutor
{
    public static async Task Run(ExecutorOpts opts, params Func<Task>[] jobs)
    {
        var channelOpts = new BoundedChannelOptions(1)
        {
            SingleWriter = true,
            SingleReader = false,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        };

        var channel = Channel.CreateBounded<Func<Task>>(channelOpts);

        var writer = channel.Writer;
        var reader = channel.Reader;

        var writerThread = Task.Run(EnqueueJobs);

        var threads = Enumerable.Range(0, opts.MaxParallelJobs)
            .Select(_ => Task.Run(ExecuteJobs))
            .ToList();

        threads.Add(writerThread);

        await Task.WhenAll(threads);

        async Task EnqueueJobs()
        {
            foreach (var job in jobs)
            {
                await writer.WriteAsync(job);

                Console.WriteLine("Enqueued job");

                await Task.Delay(opts.DelayBetweenJobs);
            }

            writer.Complete();

            Console.WriteLine("Writer done");
        }

        async Task ExecuteJobs()
        {
            while (await reader.WaitToReadAsync()) // TODO: Inactivity timeout ?
            {
                var jobToExecute = await reader.ReadAsync();

                //TODO: Handle other errors

                try
                {
                    Console.WriteLine("Executing job");

                    await jobToExecute()
                        .WaitAsync(opts.MaxExecutionTime);
                    
                    Console.WriteLine("Executed job");
                }
                catch (TimeoutException)
                {
                    // TODO: What now ?
                }
            }
        }
    }
}
