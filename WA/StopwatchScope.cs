// (c) longod, MIT License

namespace WA
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics;
    using ZLogger;

    public struct StopwatchScope : IDisposable
    {
        private Stopwatch _sw;
        private string _marker;
        private readonly ILogger _logger;

        public StopwatchScope(string marker)
        {
            _marker = marker;
            _logger = null;
            _sw = new Stopwatch();
            _sw.Start();
        }

        public StopwatchScope(string marker, ILogger logger)
        {
            _marker = marker;
            _logger = logger;
            _sw = new Stopwatch();
            _sw.Start();
        }

        public void Dispose()
        {
            if (_sw != null)
            {
                _sw.Stop();
                if (_logger != null)
                {
                    _logger.ZLogDebug("{0}: {1}ms", _marker, _sw.ElapsedMilliseconds);
                }
                else
                {
                    Trace.WriteLine($"{_marker}: {_sw.ElapsedMilliseconds}ms");
                }

                _sw = null;
            }
        }
    }
}
