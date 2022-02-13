namespace TinyCart.Eric;

using System;
using Microsoft.Extensions.Logging;

public class Logging
{
    public Logging(ILoggerFactory logFactory)
        => m_loggerFactory = logFactory;

    public Logger GetLogger(string name)
        => new Logger(m_loggerFactory.CreateLogger($"{this.GetType().Namespace}.{name}"));

    public Logger GetLogger<T>()
        => GetLogger(typeof(T).Name);

    private ILoggerFactory m_loggerFactory;
}

public class Logger
{
    public Logger(ILogger logger) => m_logger = logger;

    public void Log(LogLevel level, string format, params object[] args)
        => m_logger.Log(level, format, args);

    public void Critical(string format, params object[] args)
        => m_logger.LogCritical(format, args);
    public void Critical(Exception ex, string format, params object[] args)
        => m_logger.LogCritical(ex, format, args);
    public void Error(string format, params object[] args)
        => m_logger.LogError(format, args);
    public void Error(Exception ex, string format, params object[] args)
        => m_logger.LogError(ex, format, args);
    public void Warning(string format, params object[] args)
        => m_logger.LogWarning(format, args);
    public void Warning(Exception ex, string format, params object[] args)
        => m_logger.LogWarning(ex, format, args);
    public void Info(string format, params object[] args)
        => m_logger.LogInformation(format, args);
    public void Info(Exception ex, string format, params object[] args)
        => m_logger.LogInformation(ex, format, args);
    public void Debug(string format, params object[] args)
        => m_logger.LogDebug(format, args);
    public void Debug(Exception ex, string format, params object[] args)
        => m_logger.LogDebug(ex, format, args);
    public void Trace(string format, params object[] args)
        => m_logger.LogTrace(format, args);
    public void Trace(Exception ex, string format, params object[] args)
        => m_logger.LogTrace(ex, format, args);

    private ILogger m_logger;
}