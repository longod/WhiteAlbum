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

        private FileStream _stream;
        private FileInfo _file;
        private byte[] _binary;
        private long _actualFileSize = 0;
        private long _minFileSize = 0;

        internal FileLoader(string path, int minFileSize)
        {
            _file = new FileInfo(path);
            _minFileSize = minFileSize + _macBinaryHeaderSize;
        }

        internal string Extension => _file.Extension.ToLower();

        // fixme temp
        internal Stream Stream => new MemoryStream(_binary, 0, (int)_actualFileSize, false);

        internal ReadOnlyMemory<byte> Binary => new ReadOnlyMemory<byte>(_binary, 0, (int)_actualFileSize);

        internal ReadOnlyMemory<byte> RawBinary => new ReadOnlyMemory<byte>(_binary);

        // IsMacBinary 判定が正確になれば Binary 内で判定してオフセットをすればよい
        internal ReadOnlyMemory<byte> BinaryWithoutMacBinary => new ReadOnlyMemory<byte>(_binary, _macBinaryHeaderSize, (int)(_actualFileSize - _macBinaryHeaderSize));

        internal ReadOnlyMemory<byte> RawBinaryWithoutMacBinary => new ReadOnlyMemory<byte>(_binary, _macBinaryHeaderSize, _binary.Length - _macBinaryHeaderSize);

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

                // zero field and filename length
                if (h[0] == 0 && h[74] == 0 && h[82] == 0 && h[1] > 0)
                {
                    // MacBinary II minimum version
                    if (h[123] == 129)
                    {
                        // V3? MacBinary II version
                        if (h[122] == 130)
                        {
                            // 'mBIN' big-endian?
                            var mbin = MemoryMarshal.Cast<byte, uint>(h.Slice(102))[0];
                            if (mbin == 0x6F42494E)
                            {
                                return true; // V3
                            }

                        }
                        // V2? MacBinary II version
                        else if (h[122] == 129)
                        {
                            // CRC-16-CCITT of [0-123] byte
                            // CRCアルゴリズムは指定されていないのと、実装によっては計算せずに0になっていることがあるのでアテにならない
                            var crc = MemoryMarshal.Cast<byte, ushort>(h.Slice(124))[0];

                            // swap for BE?
                            if (crc == 0 || crc == Crc16(h.Slice(0, 124)))
                            {
                                return true; // V2
                            }
                        }
                    }

                    return null; // possible V1
                }

                return false;
            }
        }

        // for MacBinary CRC-16-CCITT
        // 左送りだが、右送りかどちらだ？
        private ushort Crc16(ReadOnlySpan<byte> bin)
        {
            ushort crc = 0;
            foreach (var b in bin)
            {
                crc ^= (ushort)(b << 8);
                for (int j = 0; j < 8; ++j)
                {
                    // msb
                    if ((crc & 0x8000) == 0x8000)
                    {
                        const ushort polynomial = 0x1201;
                        crc = (ushort)((crc << 1) ^ polynomial);
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }
            return crc;
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }

        // 先頭から指定バイト読み取る, 前回から読み進めるのではない
        internal async Task PeekAsync(long peekSize)
        {
            OpenFile();

            // done
            if (_stream == null)
            {
                return;
            }

            if (peekSize < _stream.Position)
            {
                return;
            }

            var remainSize = _actualFileSize - _stream.Position;
            if (remainSize == 0)
            {
                return;
            }

            if (remainSize < peekSize)
            {
                peekSize = remainSize;
            }

            var offset = _stream.Position;

            // todo try catch
            await _stream.ReadAsync(_binary, (int)offset, (int)peekSize)
               .ContinueWith(async x =>
               {
                   if (_stream.Position == _actualFileSize)
                   {
                       await _stream.DisposeAsync();
                       _stream = null;
                   }
               });
        }

        internal async Task PeekAsync()
        {
            await PeekAsync(_minFileSize);
        }

        internal async Task ReadAsync()
        {
            OpenFile();

            // done
            if (_stream == null)
            {
                return;
            }

            var remainSize = _actualFileSize - _stream.Position;
            if (remainSize == 0)
            {
                return;
            }
            var offset = _stream.Position;

            // todo try catch
            await _stream.ReadAsync(_binary, (int)offset, (int)remainSize)
                .ContinueWith(async x =>
                {
                    // 済んだら解放しておく
                    if (_stream.Position == _actualFileSize)
                    {
                        await _stream.DisposeAsync();
                        _stream = null;
                    }
                });
        }

        private void OpenFile()
        {
            // todo try catch
            if (_binary == null)
            {
                _stream = File.OpenRead(_file.FullName);

                _actualFileSize = _stream.Length;

                long allocSize = _actualFileSize;
                if (_actualFileSize < _minFileSize)
                {
                    allocSize = _minFileSize;
                }

                _binary = new byte[allocSize];
            }
        }


    }
}
