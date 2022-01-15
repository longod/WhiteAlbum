namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Interop;
    using Microsoft.Extensions.Logging;
    using ZLogger;

    public class PluginManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly Susie.StringConverter _stringConverter;
        private bool _disposed = false;
        private List<string> _pluginDirectories; // todo use settings
        private bool _searchSubDirectory = true; // directory単位で持つかも
        private string[] _pluginPaths = null;
        private ReadOnlyMemory<string> _leftPaths;
        private List<IPluginProxy> _plugins = new List<IPluginProxy>();

        public ObservableCollection<string> PluginDirectories { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> PluginList { get; } = new ObservableCollection<string>();

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
            // todo optimize
            _pluginDirectories = settings.Data.PluginDirectories;
            foreach (var d in _pluginDirectories)
            {
                PluginDirectories.Add(d);
            }
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

        public void ScanPluginDirectory(bool rescan = false)
        {
            if (!rescan && _pluginPaths != null)
            {
                return;
            }

            using (new StopwatchScope("Scan plugin directory", _logger))
            {
                _pluginPaths = EnumeratePluginPath().ToArray();
            }

            PluginList.Clear();
            foreach (var path in _pluginPaths)
            {
                PluginList.Add(path);
            }

            _logger.ZLogInformation("Find plugin count: {0}", _pluginPaths.Length);
            _leftPaths = _pluginPaths;
        }

        public void ShowConfig(string path, WindowInteropHelper handle)
        {
            // todo find loaded plugin and more
            using (var plugin = new SusiePluginProxy(new Susie.SusiePlugin(path, _stringConverter)))
            {
                if (plugin.ShowConfig(handle.Handle) == false)
                {
                    _logger.ZLogInformation("Has not config: {0}", path);
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

        public void SwapDirectory(int from, int to)
        {
            // todo write settings
            var temp = _pluginDirectories[from];
            _pluginDirectories[from] = _pluginDirectories[to];
            _pluginDirectories[to] = temp;
            PluginDirectories[from] = _pluginDirectories[from];
            PluginDirectories[to] = _pluginDirectories[to];
            // rescan?
        }

        public void RemoveDirectory(int index)
        {
            // todo write settings
            _pluginDirectories.RemoveAt(index);
            PluginDirectories.RemoveAt(index);
            // rescan?
        }

        public void AddDirectory(string path)
        {
            // todo write settings
            _pluginDirectories.Add(path);
            PluginDirectories.Add(path);
            // rescan?
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
                                try
                                {
                                    if (p.IsSupported(loader))
                                    {
                                        // todo どこかに対応付けをしておく
                                        return p;
                                    }
                                }
                                catch (Exception e)
                                {
                                    // 様子見
                                    // ここで死ぬと、後続のプラグインが対応しているかどうか確認できないので
                                    _logger.LogError(e, "Error was occured into plugin {0}", p);
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
