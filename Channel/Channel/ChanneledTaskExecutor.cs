namespace Channel;

using System;
using System.Threading.Channels;
using System.Threading.Tasks;

public record ChanneledTaskExecutorOpts(TimeSpan DelayBetweenJobs, TimeSpan MaxExecutionTime, int MaxParallelJobs);

public static class ChanneledTaskExecutor
{
    public static async Task Run(ChanneledTaskExecutorOpts opts, params Func<Task>[] jobs)
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
        var readerThreads = Enumerable.Range(0, Math.Min(jobs.Length, opts.MaxParallelJobs))
            .Select(_ => Task.Run(ExecuteJobs));

        await Task.WhenAll(new List<Task>(readerThreads) { writerThread });

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
            while (await reader.WaitToReadAsync())
            {
                var jobToExecute = await reader.ReadAsync();

                try
                {
                    Console.WriteLine("Executing job");

                    await jobToExecute()
                        .WaitAsync(opts.MaxExecutionTime);

                    Console.WriteLine("Executed job");
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Timeout awaiting");
                }
            }
        }
    }
}