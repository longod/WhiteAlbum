
namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class DirectoryDecoder
    {
        public DirectoryDecoder()
        {
        }

        // temp returning type
        public PackedFile[] Decode(string path)
        {
            var di = new DirectoryInfo(path);
            if (!di.Exists)
            {
            }

            // recursive?
            // var dirs = di.EnumerateDirectories();
            var files = di.EnumerateFiles();
            return files.Select(x => new PackedFile() { Path = x.Name, Date = x.LastWriteTime, FileSize = x.Length }).ToArray();
        }
    }
}
