using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace WA
{
    public struct StopwatchScope : IDisposable
    {
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
        Stopwatch _sw;
        string _marker;
    }
}
