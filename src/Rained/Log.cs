namespace RainEd;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

static class Log
{
    /// <summary>
    /// Logger whose contents will be shown in the in-app log window.
    /// </summary>
    public static Serilog.Core.Logger UserLogger = null!;
    private static Serilog.Core.Logger _logger = null!;

    public static List<LogEntry> UserLog = [];

    public static void Setup(bool logToStdout)
    {
        Directory.CreateDirectory(Path.Combine(Boot.AppDataPath, "logs"));

        #if DEBUG
        var loggerConfig = new LoggerConfiguration().MinimumLevel.Debug();
        #else
        var loggerConfig = new LoggerConfiguration();
        #endif

        List<string> logSetupErrors = [];

        var logLatest = Path.Combine(Boot.AppDataPath, "logs", "latest.log.txt");
        if (File.Exists(logLatest))
        {
            try
            {
                File.Delete(logLatest);
                loggerConfig.WriteTo.File(logLatest, retainedFileCountLimit: 1);
            }
            catch (Exception e)
            {
                logSetupErrors.Add("Could not write to latest.log.txt: " + e);
            }
        }

        loggerConfig.WriteTo.File(
            Path.Combine(Boot.AppDataPath, "logs", "log.txt"),
            rollingInterval: RollingInterval.Hour,
            retainedFileCountLimit: 10
        );

        if (logToStdout)
            loggerConfig = loggerConfig.WriteTo.Console();

        _logger = loggerConfig.CreateLogger();

        var logStream = new MemoryStream();
        var writer = new StreamWriter(logStream);
        var reader = new StreamReader(logStream);

        var wrapperLogConfig = new LoggerConfiguration()
            .WriteTo.Sink(new UserLogSink(UserLog, Boot.UserCulture))
            .WriteTo.Logger(_logger);

        UserLogger = wrapperLogConfig.CreateLogger();

        Serilog.Log.Logger = UserLogger;

        foreach (var msg in logSetupErrors)
            Error(msg);
    }

    public static void Close()
    {
        _logger.Dispose();
        UserLogger.Dispose();
        _logger = null!;
        UserLogger = null!;
    }

    public enum LogLevel
    {
        Verbose,
        Debug,
        Information,
        Warning,
        Error,
        Fatal
    };

    public readonly record struct LogEntry(LogLevel Level, string Message);

    class UserLogSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;
        private readonly List<LogEntry> _log;

        public UserLogSink(List<LogEntry> log, IFormatProvider formatProvider)
        {
            _log = log;
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            var msg = logEvent.RenderMessage(_formatProvider);
            var prefix = logEvent.Level switch
            {
                LogEventLevel.Verbose => "vrb",
                LogEventLevel.Debug => "dbg",
                LogEventLevel.Information => "inf",
                LogEventLevel.Warning => "wrn",
                LogEventLevel.Error => "err",
                LogEventLevel.Fatal => "ftl",
                _ => "???"
            };

            msg = $"[{prefix}] {msg}";
            lock (_log)
            {
                _log.Add(new LogEntry((LogLevel)logEvent.Level, msg));
            }
        }
    }

    public static void Debug(string msgTemplate, params object[] args) => _logger.Debug(msgTemplate, args);
    public static void Verbose(string msgTemplate, params object[] args) => _logger.Verbose(msgTemplate, args);
    public static void Information(string msgTemplate, params object[] args) => _logger.Information(msgTemplate, args);
    public static void Warning(string msgTemplate, params object[] args) => _logger.Warning(msgTemplate, args);
    public static void Error(string msgTemplate, params object[] args) => _logger.Error(msgTemplate, args);
    public static void Fatal(string msgTemplate, params object[] args) => _logger.Fatal(msgTemplate, args);
}