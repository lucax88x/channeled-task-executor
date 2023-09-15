using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Channel.Tests;

public record ChanneledTaskExecutorOpts(TimeSpan DelayBetweenJobs, TimeSpan MaxExecutionTime, int MaxParallelJobs);

public class ChanneledTaskExecutor
{
    readonly ILogger<ChanneledTaskExecutor> _logger;

    public ChanneledTaskExecutor(ILogger<ChanneledTaskExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<ICollection<TR>> Run<TR>(ChanneledTaskExecutorOpts opts, params Func<Task<TR>>[] jobs)
    {
        ArgumentNullException.ThrowIfNull(opts);
        ArgumentNullException.ThrowIfNull(jobs);

        var channelOpts = new BoundedChannelOptions(1)
        {
            SingleWriter = true, SingleReader = false, FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        };

        var responseChannelOpts = new BoundedChannelOptions(jobs.Length)
        {
            SingleWriter = false, SingleReader = true, FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        };

        var channel = System.Threading.Channels.Channel.CreateBounded<Func<Task<TR>>>(channelOpts);
        var responseChannel = System.Threading.Channels.Channel.CreateBounded<TR>(responseChannelOpts);

        var writer = channel.Writer;
        var reader = channel.Reader;

        async Task EnqueueJobs()
        {
            foreach (var job in jobs)
            {
                await writer.WriteAsync(job);

                _logger.LogDebug("Enqueued job");

                await Task.Delay(opts.DelayBetweenJobs);
            }

            writer.Complete();

            _logger.LogDebug("Writer done");
        }

        async Task ExecuteJobs()
        {
            while (await reader.WaitToReadAsync())
            {
                try
                {
                    var jobToExecute = await reader.ReadAsync();

                    _logger.LogDebug("Executing job");

                    var result = await jobToExecute()
                        .WaitAsync(opts.MaxExecutionTime);

                    await responseChannel.Writer.WriteAsync(result);

                    _logger.LogDebug("Executed job");
                }
                catch (TimeoutException ex)
                {
                    _logger.LogError(ex, "Timeout waiting");
                    _logger.LogDebug("Timeout awaiting");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "General exception running task");
                }
            }
        }

        var writerThread = Task.Run(EnqueueJobs);
        var readerThreads = Enumerable.Range(0, Math.Min(jobs.Length, opts.MaxParallelJobs))
            .Select(_ => Task.Run(ExecuteJobs));

        await Task.WhenAll(new List<Task>(readerThreads) { writerThread });

        responseChannel.Writer.Complete();

        var responses = new List<TR>();
        
        await foreach (var response in responseChannel.Reader.ReadAllAsync())
        {
            responses.Add(response);
        }

        return responses;
    }

    public IEnumerable<Func<Task<TR>>> Wrap<T, TR>(IEnumerable<T> items, Func<T, Task<TR>> func)
    {
        return items
            .Select<T, Func<Task<TR>>>(item => () => func(item));
    }
}