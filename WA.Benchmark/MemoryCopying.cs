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

        [Benchmark]
        public void VectorCopy()
        {
            // FIXME 正しい使い方ではない
            // SIMD レジスタに格納できる数(Vector<T>.Count)しか格納できないので、その回数分操作を繰り返す必要がある
            // 終端の調整も必要
            // https://stackoverflow.com/questions/55931034/system-numerics-vectort-on-large-data-sets
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
