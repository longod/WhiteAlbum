// (c) longod, MIT License

namespace WA
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    internal class FileLoader : IDisposable
    {
        private FileInfo _file;
        private byte[] _binary;

        internal FileLoader(string path)
        {
            _file = new FileInfo(path);
        }

        internal string Extension => _file.Extension.ToLower();

        // temp
        internal Stream Stream => new MemoryStream(_binary, false);
        // temp, spanとかmemoryとかで要件を満たしてほしい
        internal byte[] Binary => _binary;

        public string Path => _file.FullName;

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
