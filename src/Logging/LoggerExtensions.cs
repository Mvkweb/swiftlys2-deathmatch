using Microsoft.Extensions.Logging;

namespace SwiftlyS2_Deathmatch.Logging;

public static class LoggerExtensions
{
    public static void LogPluginInformation(this ILogger logger, string message, params object?[] args)
    {
        logger.LogInformation(message, args);
    }

    public static void LogPluginWarning(this ILogger logger, string message, params object?[] args)
    {
        logger.LogWarning(message, args);
    }

    public static void LogPluginError(this ILogger logger, string message, params object?[] args)
    {
        logger.LogError(message, args);
    }

    public static void LogPluginError(this ILogger logger, Exception exception, string message, params object?[] args)
    {
        logger.LogError(exception, message, args);
    }
}
