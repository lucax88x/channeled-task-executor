using FluentAssertions;
using Xunit.Abstractions;

namespace Channel.Tests;

[Trait("Integration", "true")]
[Trait("LocalOnly", "true")]
public class ChanneledTaskExecutorTests
{
    readonly ITestOutputHelper _testOutputHelper;
    readonly ChanneledTaskExecutor _sut;

    public ChanneledTaskExecutorTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        _sut = new ChanneledTaskExecutor(
            XunitLoggerFactory.CreateMicrosoftLogger<ChanneledTaskExecutor>(_testOutputHelper));
    }

    [Fact]
    public async Task should_run_only_twice_per_time()
    {
        var results = await _sut.Run(
            new ChanneledTaskExecutorOpts(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), 2),
            () => DelayTask((1, 1)),
            () => DelayTask((2, 1)),
            () => DelayTask((3, 1)),
            () => DelayTask((4, 1))
        );

        results.Should().Contain(1);
        results.Should().Contain(2);
        results.Should().Contain(3);
        results.Should().Contain(4);
    }

    [Fact]
    public async Task should_run_instant_but_only_two_per_time()
    {
        var results = await _sut.Run(
            new ChanneledTaskExecutorOpts(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 2),
            () => DelayTask((1, 1)),
            () => DelayTask((2, 1)),
            () => DelayTask((3, 1)),
            () => DelayTask((4, 1))
        );

        results.Should().Contain(1);
        results.Should().Contain(2);
        results.Should().Contain(3);
        results.Should().Contain(4);
    }

    [Fact]
    public async Task should_crash_for_timeout_and_return_only_goods()
    {
        var results = await _sut.Run(
            new ChanneledTaskExecutorOpts(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), 2),
            () => DelayTask((1, 1)),
            () => DelayTask((2, 2)),
            () => DelayTask((3, 10)),
            () => DelayTask((4, 4))
        );

        results.Should().Contain(1);
        results.Should().Contain(2);
        results.Should().NotContain(3);
        results.Should().Contain(4);
    }

    [Fact]
    public async Task if_a_task_crashes_the_other_are_still_returning_data()
    {
        var results = await _sut.Run(
            new ChanneledTaskExecutorOpts(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), 2),
            () => DelayTask((1, 1)),
            ThrowException
        );

        results.Should().Contain(1);
        results.Should().HaveCount(1);
    }

    async Task<int> DelayTask((int id, int seconds) tuples)
    {
        var id = tuples.id;
        var seconds = tuples.seconds;

        _testOutputHelper.WriteLine($"{id}: I will take {seconds} seconds");
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        _testOutputHelper.WriteLine($"{id}: I took {seconds} seconds");

        return id;
    }

    Task<int> ThrowException()
    {
        throw new Exception("catastrophic");
    }
}