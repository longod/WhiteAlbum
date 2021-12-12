// (c) longod, MIT License
namespace WA
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using ZLogger;

    /// <summary>
    /// WPFなど非コンソールでのログ出力を考慮した LogProcessor.
    /// </summary>
    public class TraceLogProcessor : IAsyncLogProcessor
    {
        private readonly ZLoggerOptions _options;

        public TraceLogProcessor(ZLoggerOptions options)
        {
            _options = options;
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        public void Post(IZLoggerEntry log)
        {
            try
            {
                switch (log.LogInfo.LogLevel)
                {
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                    case LogLevel.Information:
                    case LogLevel.Warning:
                    case LogLevel.Critical:
                        {
                            var msg = log.FormatToString(_options, null);
                            Trace.WriteLine(msg);
                        }

                        break;
                    case LogLevel.Error:
                        if (log.LogInfo.Exception != null)
                        {
                            Trace.WriteLine(log.LogInfo.Exception);
                        }
                        else
                        {
                            var msg = log.FormatToString(_options, null);
                            Trace.WriteLine(msg);
                        }

                        break;
                    case LogLevel.None:
                        break;
                    default:
                        break;
                }
            }
            finally
            {
                log.Return();
            }
        }
    }
}
