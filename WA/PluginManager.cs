namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using ZLogger;

    public class PluginManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly Susie.StringConverter _stringConverter;
        private bool _disposed = false;
        private List<string> _pluginDirectories;
        private bool _searchSubDirectory = true; // directory単位で持つかも
        private string[] _pluginPaths = null;
        private ReadOnlyMemory<string> _leftPaths;
        private List<IPluginProxy> _plugins = new List<IPluginProxy>();

        ~PluginManager()
        {
            Dispose(false);
        }

        // 主キー重複の場合、タイムスタンプを次に優先する可能性もある
        private class SameNameFileInfoEqualityComparer : IEqualityComparer<FileInfo>
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

        public PluginManager(AppSettings settings, Susie.StringConverter stringConverter, ILogger logger)
        {
            _logger = logger;
            _stringConverter = stringConverter;
            _pluginDirectories = settings.Data.PluginDirectories;
        }

        private IEnumerable<string> EnumeratePluginPath()
        {
            // 現在の探索ルール
            // _pluginDirectories の上から順を優先する
            // 浅い階層を優先する
            // 同一階層のファイルは名前昇順を優先する
            // 同一階層のディレクトリは名前昇順を優先する
            return _pluginDirectories.SelectMany(x => SearchPlugin(new DirectoryInfo(x), _searchSubDirectory))
                        // .Distinct(new SameNameFileInfoEQ()) // unique
                        .Select(x => x.FullName);
        }

        internal void ScanPluginDirectory(bool rescan = false)
        {
            if (!rescan && _pluginPaths != null)
            {
                return;
            }

            using (new StopwatchScope("Scan plugin directory", _logger))
            {
                _pluginPaths = EnumeratePluginPath().ToArray();
            }

            _logger.ZLogInformation("Find plugin count: {0}", _pluginPaths.Length);
            _leftPaths = _pluginPaths;
        }

        // test
        public void ShowConfigTest(IntPtr hWnd)
        {
            foreach (var d in _plugins)
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

        private IPluginProxy LoadPlugin(string path)
        {
            SusiePluginProxy plugin = null;
            try
            {
                plugin = new SusiePluginProxy(new Susie.SusiePlugin(path, _stringConverter));
                if (plugin != null)
                {
                    _plugins.Add(plugin);
                }
            }
            catch (BadImageFormatException e)
            {
                // architectureが一致していない
                _logger.ZLogError(e, $"Mismatch architecture: {path}");
                plugin?.Dispose();
            }
            catch (DllNotFoundException e)
            {
                // 多分 依存しているdllが読めていない
                _logger.ZLogError(e, $"Failed to load dependency DLLs: {path}");
                plugin?.Dispose();
            }

            return plugin;
        }

        // test
        private void LoadAllPlugins()
        {
            using (new StopwatchScope("LoadAllPlugins", _logger))
            {
                foreach (var path in _pluginPaths)
                {
                    LoadPlugin(path);
                }
            }
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

        internal async Task<IPluginProxy> FindDecodablePluginAsync(FileLoader loader/*, bool enumerateAll = false*/)
        {
            using (new StopwatchScope("Find decodable plugin", _logger))
            {
                var plugin = await Task.Run(() =>
                {
                    // try already loaded plugin
                    if (_plugins?.Count > 0)
                    {
                        using (new StopwatchScope("Try to find plugins on memory", _logger))
                        {
                            // TODO parallel or sequenceial option
                            foreach (var p in _plugins)
                            {
                                if (p.IsSupported(loader))
                                {
                                    // todo どこかに対応付けをしておく
                                    return p;
                                }
                            }
                        }
                    }

                    // on demand plugin
                    ScanPluginDirectory();

                    using (new StopwatchScope("Try to find plugins on drive", _logger))
                    {
                        // TODO parallel or sequenceial option
                        // todo discard option when done、失敗したやつは破棄するオプション
                        var span = _leftPaths.Span;
                        for (int i = 0; i < span.Length; ++i)
                        {

                            var p = LoadPlugin(span[i]);
                            if (p != null && p.IsSupported(loader))
                            {
                                // slide
                                _leftPaths = _leftPaths.Slice(i + 1);

                                // todo どこかに対応付けをしておく
                                return p;
                            }
                        }

                        // not found
                        _leftPaths = _leftPaths.Slice(_leftPaths.Length); // slide to end
                    }

                    return null;
                });

                return plugin;
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
                foreach (var d in _plugins)
                {
                    d.Dispose();
                }

                _plugins.Clear();

                _disposed = true;
            }
        }
    }
}
