// (c) longod, MIT License

namespace WA
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    internal class FileLoader : IDisposable
    {
        private const int _macBinaryHeaderSize = 128;

        private FileInfo _file;
        private byte[] _binary;
        private int _minFileSize = 0;

        internal FileLoader(string path, int minFileSize)
        {
            _file = new FileInfo(path);
            _minFileSize = minFileSize;
        }

        internal string Extension => _file.Extension.ToLower();

        // fixme temp
        internal Stream Stream => new MemoryStream(_binary, false);

        // todo set actual length
        internal ReadOnlyMemory<byte> Binary => new ReadOnlyMemory<byte>(_binary);

        // todo set actual length
        // IsMacBinary 判定が正確になれば Binary 内で判定してオフセットをすればよい
        internal ReadOnlyMemory<byte> BinaryWithoutMacBinary => new ReadOnlyMemory<byte>(_binary, _macBinaryHeaderSize, _binary.Length);

        internal string Path => _file.FullName;

        // V3以外は検出が複雑
        // https://entropymine.wordpress.com/2019/02/13/detecting-macbinary-format/
        // https://code.google.com/archive/p/theunarchiver/wikis/MacBinarySpecs.wiki
        // https://www.wdic.org/w/TECH/%E3%83%9E%E3%83%83%E3%82%AF%E3%83%90%E3%82%A4%E3%83%8A%E3%83%AA
        internal bool? IsMacBinary
        {
            get
            {
                // fixme get actual size
                if (_binary.Length < _macBinaryHeaderSize)
                {
                    return false;
                }

                var h = Binary.Span.Slice(0, _macBinaryHeaderSize);

                // 'mBIN' big-endian?
                var mbin = MemoryMarshal.Cast<byte, uint>(h.Slice(102))[0];
                if (mbin == 0x6F42494E)
                {
                    return true; // V3
                }

                // zero field
                if (h[0] == 0 && h[74] == 0 && h[82] == 0)
                {

                    // possible V1 or V2
                    // MacBinary II version
                    if (h[122] == 129 && h[123] == 129)
                    {
                        // CRC16 of [0-123] byte
                        var crc = MemoryMarshal.Cast<byte, ushort>(h.Slice(124))[0];
                        // swap for BE?
                        if (crc == Crc16(h.Slice(0, 124)))
                        {
                            return true; // V2
                        }
                    }

                    return null; // possible V1
                }

                return false;
            }
        }

        private ushort Crc16(ReadOnlySpan<byte> readOnlySpan)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        // peekAsync
        // ここで2kbだけ読んで処理してつづきを読むとか
        internal async ValueTask<int> PeekAsync(int peekSize)
        {
            // todo reuse peek and load buffer
            _binary = null;
            using (var stream = File.OpenRead(_file.FullName))
            {
                long allocSize = 0;
                int readSize = 0;
                if (stream.Length > peekSize)
                {
                    allocSize = stream.Length;
                    readSize = peekSize;
                }
                else
                {
                    allocSize = peekSize;
                    readSize = (int)stream.Length;
                }

                _binary = new byte[allocSize];
                return await stream.ReadAsync(_binary, 0, readSize); // or async
            }
        }

        internal async ValueTask<int> LoadAsync()
        {
            _binary = null;
            using (var stream = File.OpenRead(_file.FullName))
            {
                _binary = new byte[stream.Length];
                return await stream.ReadAsync(_binary); // or async
            }
        }
    }
}
