using System.Globalization;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;

namespace Channel;

public static class XunitLoggerFactory
{
    public static Logger CreateLogger(ITestOutputHelper testOutputHelper)
    {
        var logger = new LoggerConfiguration();

        logger = logger
            .WriteTo
            .TestOutput(testOutputHelper, formatProvider: CultureInfo.InvariantCulture);

        return logger.CreateLogger();
    }

    public static Logger CreateLogger(IMessageSink messageSink)
    {
        var logger = new LoggerConfiguration();

        logger = logger
            .WriteTo
            .TestOutput(messageSink, formatProvider: CultureInfo.InvariantCulture);

        return logger.CreateLogger();
    }

    public static ILogger<T> CreateMicrosoftLogger<T>(ITestOutputHelper testOutputHelper)
    {
        using var logger = CreateLogger(testOutputHelper);

        return ToMicrosoftLogger<T>(logger);
    }

    public static ILogger<T> CreateMicrosoftLogger<T>(IMessageSink messageSink)
    {
        using var logger = CreateLogger(messageSink);

        return ToMicrosoftLogger<T>(logger);
    }

    static ILogger<T> ToMicrosoftLogger<T>(Logger logger)
    {
        using var loggerFactory = new SerilogLoggerFactory(logger);

        return new Logger<T>(loggerFactory);
    }
}