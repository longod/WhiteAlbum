// (c) longod, MIT License

namespace WA
{
    using System;
    using System.Diagnostics;

    public struct StopwatchScope : IDisposable
    {
        private Stopwatch _sw;
        private string _marker;

        public StopwatchScope(string marker)
        {
            _marker = marker;
            _sw = new Stopwatch();
            _sw.Start();
        }

        public void Dispose()
        {
            if (_sw != null)
            {
                _sw.Stop();
                Trace.WriteLine($"{_marker}: {_sw.ElapsedMilliseconds}ms");
            }
        }
    }
}
