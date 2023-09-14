using System.Threading.Channels;
using Channel;

Console.WriteLine("Hello, World!");

await new ChanneledTaskExecutor().Run(TimeSpan.FromSeconds(1), 15, async () =>
    {
        Console.WriteLine("I will take 1 seconds");
        await Task.Delay(TimeSpan.FromSeconds(1));
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
    }
);