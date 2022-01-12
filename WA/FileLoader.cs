namespace WA
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    // fixme loaderとmemoryは分けた方がよい, filehandleは速やかにdisposeしたいが、memoryは維持したい
    internal class FileLoader : IDisposable
    {
        private FileStream _stream;
        private FileInfo _file;
        private byte[] _binary;
        private long _actualFileSize = 0;
        private long _minFileSize = 0;

        internal FileLoader(string path, int minFileSize)
        {
            _file = new FileInfo(path);
            _minFileSize = minFileSize;
        }

        // todo 別の派生streamとすることも検討する
        internal FileLoader(string path, byte[] binary, int minFileSize)
        {
            _file = new FileInfo(path);
            // FIXME minsizeに満たない場合、詰め直す必要がある
            _binary = binary;
            _actualFileSize = _binary.Length;
            _minFileSize = minFileSize;
        }

        internal string Extension => _file.Extension.ToLower();

        // fixme temp
        internal Stream Stream => new MemoryStream(_binary, 0, (int)_actualFileSize, false);

        internal ReadOnlyMemory<byte> Binary => new ReadOnlyMemory<byte>(_binary, 0, (int)_actualFileSize);

        internal ReadOnlyMemory<byte> RawBinary => new ReadOnlyMemory<byte>(_binary);

        internal ReadOnlyMemory<byte> BinaryWithoutMacBinary => MacBinaryUtility.CutHeader(new ReadOnlyMemory<byte>(_binary, 0, (int)_actualFileSize));

        internal ReadOnlyMemory<byte> RawBinaryWithoutMacBinary => MacBinaryUtility.CutHeader(new ReadOnlyMemory<byte>(_binary));

        internal string Path => _file.FullName;

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
