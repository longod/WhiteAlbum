using BenchmarkDotNet.Running;
using System;

namespace WA.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<BitmapCreation>();
        }
    }
}
