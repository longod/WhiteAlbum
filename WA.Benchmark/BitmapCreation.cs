using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WA.Benchmark
{
    public class BitmapCreation
    {

        byte[] _binary = null;

        [GlobalSetup]
        public void Setup()
        {
            byte[] binary = null;
            string path = @"..\..\..\..\Temp\image\big.bmp";
            using (var stream = File.OpenRead(path))
            {
                binary = new byte[stream.Length];
                var ret = stream.Read(binary); // or async
            }

            _binary = binary;
        }

        [Benchmark]
        public void CreateBitmapImage()
        {
            ViewerModel.CreateBitmapImage(_binary);
        }

        [Benchmark]
        public void CreateBitmapFrame()
        {
            ViewerModel.CreateBitmapFrame(_binary);
        }

    }
}
