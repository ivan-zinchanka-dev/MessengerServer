using Microsoft.Extensions.Logging;

namespace MessengerServer.Core.Services.FileLogging;

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filePath)
    {
        return builder.AddProvider(new FileLoggerProvider(filePath));
    }
}