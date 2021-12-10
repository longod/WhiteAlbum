namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Linq;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    public class PluginManager : IDisposable
    {
        private List<string> _pluginDirectories = new List<string>() { @"..\..\..\..\Temp\spi\spi32008" };

        private bool _searchSubDirectory = true; // directory単位で持つかも

        private string[] _pluginPaths = null;

        private List<IDecoder> _loadedDecoder = new List<IDecoder>();

        // 主キー重複の場合、タイムスタンプを次に優先する可能性もある
        private class SameNameFileInfoEQ : IEqualityComparer<FileInfo>
        {
            public bool Equals([AllowNull] FileInfo x, [AllowNull] FileInfo y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode([DisallowNull] FileInfo obj)
            {
                return obj.Name.GetHashCode();
            }
        }

        public PluginManager()
        {
        }

        public PluginManager(List<string> pluginDirectories)
        {
            _pluginDirectories = pluginDirectories;
        }

        public async Task FindPlugins()
        {
            var eq = new SameNameFileInfoEQ();
            using (new StopwatchScope("FindPlugins"))
            {
                await Task.Run(() =>
               {
                   _pluginPaths = _pluginDirectories.SelectMany(x => SearchPlugin(new DirectoryInfo(x), _searchSubDirectory))
                       .Distinct(eq) // unique
                       .Select(x => x.FullName)
                       .ToArray();
               });
            }
        }

        // test
        public async Task LoadAllPlugins()
        {
            using (new StopwatchScope("LoadAllPlugins"))
            {
                await Task.Run(() =>
                {
                    foreach (var path in _pluginPaths)
                    {
                        SusiePluginDecoder decoder = null;
                        try
                        {
                            decoder = new SusiePluginDecoder(new SusiePlugin(path));
                        }
                        catch (DllNotFoundException e)
                        {
                            // 多分 dependency dllが読めていない
                            System.Diagnostics.Trace.WriteLine(e.Message);
                            decoder?.Dispose();
                            decoder = null;
                        }

                        if (decoder != null)
                        {
                            _loadedDecoder.Add(decoder);
                        }

                    }
                });
            }
        }

        private static IEnumerable<FileInfo> SearchPlugin(DirectoryInfo directory, bool searchSubDirectory)
        {
            // ディレクトリ単位でソート
            var plugins = directory.EnumerateFiles("*.spi").OrderBy(x => x.Name).AsEnumerable();

            if (searchSubDirectory)
            {
                // ディレクトリ単位でソート
                var sub = directory.EnumerateDirectories()
                    .OrderBy(x => x.Name)
                    .SelectMany(x => SearchPlugin(x, searchSubDirectory));
                plugins = plugins.Concat(sub);
            }

            return plugins;
        }

        internal async Task<IDecoder> ResolveAsync(FileLoader loader/*, bool enumerateAll = false*/)
        {
            using (new StopwatchScope("ResolveAsync"))
            {
                // test
                await FindPlugins();
                await LoadAllPlugins();

                foreach (var d in _loadedDecoder)
                {
                    if (d.IsSupported(loader))
                    {
                        // todo どこかに対応付けをしておく
                        return d;
                    }
                }

            }

            return null;
        }

        public void Dispose()
        {
            foreach (var d in _loadedDecoder)
            {
                d.Dispose();
            }
        }
    }
}
