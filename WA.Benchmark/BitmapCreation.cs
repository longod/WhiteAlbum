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
            string path = @"E:\SS\bmp\WA2_Special_2021_11_19_19_58_09_364.bmp";
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
