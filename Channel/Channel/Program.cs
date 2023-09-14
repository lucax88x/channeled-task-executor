using System.Threading.Channels;
using Channel;

var opts = new ExecutorOpts(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1), 3);

await ChanneledTaskExecutor.Run(opts, async () =>
{
    Console.WriteLine("I will take 10 seconds");
    await Task.Delay(TimeSpan.FromSeconds(10));
    Console.WriteLine("I took 10 seconds");
}, async () =>
{
    Console.WriteLine("I will take 2 seconds");
    await Task.Delay(TimeSpan.FromSeconds(2));
}, async () =>
{
    Console.WriteLine("I will take 3 seconds");
    await Task.Delay(TimeSpan.FromSeconds(3));
}, async () =>
{
    Console.WriteLine("I will take 4 seconds");
    await Task.Delay(TimeSpan.FromSeconds(4));
});

Console.WriteLine("All done");