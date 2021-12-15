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
        private bool _disposed = false;

        private List<string> _pluginDirectories;

        private bool _searchSubDirectory = true; // directory単位で持つかも

        private string[] _pluginPaths = null;

        private List<IPluginProxy> _loadedDecoder = new List<IPluginProxy>();

        private Susie.StringConverter _stringConverter;

        ~PluginManager()
        {
            Dispose(false);
        }

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

        public PluginManager(AppSettings settings, Susie.StringConverter stringConverter)
        {
            _stringConverter = stringConverter;
            _pluginDirectories = settings.PluginDirectories;
        }

        public IEnumerable<string> EnumeratePlugins()
        {
            return _pluginDirectories.SelectMany(x => SearchPlugin(new DirectoryInfo(x), _searchSubDirectory))
                        // .Distinct(new SameNameFileInfoEQ()) // unique
                        .Select(x => x.FullName);
        }

        public async Task FindAllPlugins(bool rescan = false)
        {
            if (!rescan && _pluginPaths != null)
            {
                return;
            }

            await Task.Run(() =>
            {
                using (new StopwatchScope("FindPlugins"))
                {
                    _pluginPaths = EnumeratePlugins().ToArray();
                }
            });

            System.Diagnostics.Trace.WriteLine($"find plugins: {_pluginPaths.Length}");
        }

        public void ShowConfigTest(IntPtr hWnd)
        {
            foreach (var d in _loadedDecoder)
            {
                var susie = d as SusiePluginProxy;
                if (susie != null)
                {
                    if (susie.ShowConfigTest(hWnd))
                    {
                        break;
                    }
                }
            }

        }

        // test
        public async Task LoadAllPlugins()
        {
            await Task.Run(() =>
            {
                using (new StopwatchScope("LoadAllPlugins"))
                {

                    foreach (var path in _pluginPaths)
                    {
                        SusiePluginProxy decoder = null;
                        try
                        {
                            decoder = new SusiePluginProxy(new Susie.SusiePlugin(path, _stringConverter));
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
                }
            });
        }

        private static IEnumerable<FileInfo> SearchPlugin(DirectoryInfo directory, bool searchSubDirectory)
        {
            if (!directory.Exists)
            {
                return Enumerable.Empty<FileInfo>();
            }

            // ディレクトリ単位でソート
            var plugins = directory.EnumerateFiles(Susie.API.Constant.SearchPattern).OrderBy(x => x.Name).AsEnumerable();

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

        internal async Task<IPluginProxy> ResolveAsync(FileLoader loader/*, bool enumerateAll = false*/)
        {
            using (new StopwatchScope("ResolveAsync"))
            {
                // fixme test load all plugins (not on demand)
                await FindAllPlugins();
                await LoadAllPlugins();

                var d = await Task.Run(() =>
                {
                    foreach (var d in _loadedDecoder)
                    {
                        if (d.IsSupported(loader))
                        {
                            // todo どこかに対応付けをしておく
                            return d;
                        }
                    }

                    return null;
                });

                return d;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // managed
                }

                // unmanaged
                foreach (var d in _loadedDecoder)
                {
                    d.Dispose();
                }
                _loadedDecoder.Clear();

                _disposed = true;
            }

        }
    }
}
