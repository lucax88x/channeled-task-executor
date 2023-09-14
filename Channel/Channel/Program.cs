using Channel;

var opts = new ChanneledTaskExecutorOpts(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), 2);

await ChanneledTaskExecutor.Run(opts, CreateTask(1, 10), CreateTask(2, 15), CreateTask(3, 5), CreateTask(4, 10));

Console.WriteLine("All done");

Func<Task> CreateTask(int id, int seconds)
{
    return async () =>
    {
        Console.WriteLine($"{id}: I will take {seconds} seconds");
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        Console.WriteLine($"{id}: I took {seconds} seconds");
    };
}