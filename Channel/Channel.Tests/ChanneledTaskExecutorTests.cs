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
    public async Task should_crash_for_timeout_and_return_only_goods()
    {
        var results = await _sut.Run(
            new ChanneledTaskExecutorOpts(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), 2),
            _sut.Wrap(
                    new List<(int, int)>
                    {
                        (1, 1), (2, 2), (3, 10), (4, 4),
                    },
                    DelayTask
                )
                .ToArray()
        );

        results.Should().Contain(1);
        results.Should().Contain(2);
        results.Should().NotContain(3);
        results.Should().Contain(4);
    }

    [Fact]
    public async Task should_run_only_twice_per_time()
    {
        var results = await _sut.Run(
            new ChanneledTaskExecutorOpts(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), 2),
            _sut.Wrap(
                    new List<(int, int)>
                    {
                        (1, 1), (2, 1), (3, 1), (4, 1),
                    },
                    DelayTask
                )
                .ToArray()
        );

        results.Should().Contain(1);
        results.Should().Contain(2);
        results.Should().Contain(3);
        results.Should().Contain(4);
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
}