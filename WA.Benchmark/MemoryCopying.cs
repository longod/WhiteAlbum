using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace WA.Benchmark
{
    public class MemoryCopying
    {

        private byte[] _src;
        private byte[] _dest;

        [GlobalSetup]
        public void Setup()
        {
            _src = new byte[2048];
            _dest = new byte[2048];
        }

        [Benchmark]
        public void BufferBlockCopy()
        {
            Buffer.BlockCopy(_src, 0, _dest, 0, _dest.Length * sizeof(byte));
        }

        [Benchmark]
        public void ArrayCopy()
        {
            Array.Copy(_src, _dest, _dest.Length);
        }

        // may be fastest
        [Benchmark]
        public void VectorCopy()
        {
            var v = new Vector<byte>(_src);
            v.TryCopyTo(_dest);
        }

        [Benchmark]
        public unsafe void UnsafeCopy()
        {
            fixed (void* d = _dest)
            {
                fixed (void* s = _src)
                {
                    Unsafe.CopyBlock(d, s, (uint)(_dest.Length * sizeof(byte)));
                }
            }
        }

    }
}
