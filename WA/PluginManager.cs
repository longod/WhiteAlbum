namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Windows.Interop;
    using Microsoft.Extensions.Logging;
    using ZLogger;

    public class PluginManager : IDisposable
    {
        private readonly AppSettings _settings;
        private readonly ILogger _logger;
        private readonly Susie.StringConverter _stringConverter;
        private bool _disposed = false;
        private string[] _pluginPaths = null;
        private ReadOnlyMemory<string> _leftPaths;
        private List<IPluginProxy> _plugins = new List<IPluginProxy>();

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
            _settings = settings;
            _logger = logger;
            _stringConverter = stringConverter;
        }

        private IEnumerable<string> EnumeratePluginPath()
        {
            // 現在の探索ルール
            // _pluginDirectories の上から順を優先する
            // 浅い階層を優先する
            // 同一階層のファイルは名前昇順を優先する
            // 同一階層のディレクトリは名前昇順を優先する
            return _settings.Data.PluginDirectories.SelectMany(x => SearchPlugin(new DirectoryInfo(x), _settings.Data.ScanSubDirectory))
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
            foreach (var p in _pluginPaths)
            {
                PluginList.Add(p);
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

        internal IPluginProxy FindDecodablePlugin(FileLoader loader/*, bool enumerateAll = false*/)
        {
            using (new StopwatchScope("Find decodable plugin", _logger))
            {
                // try already loaded plugin
                if (_plugins?.Count > 0)
                {
                    using (new StopwatchScope("Try to find plugins on memory", _logger))
                    {
                        // TODO parallel or sequenceial option
                        foreach (var plugin in _plugins)
                        {
                            try
                            {
                                if (plugin.IsSupported(loader))
                                {
                                    // todo どこかに対応付けをしておく
                                    return plugin;
                                }
                            }
                            catch (Exception e)
                            {
                                // 様子見
                                // ここで死ぬと、後続のプラグインが対応しているかどうか確認できないので
                                _logger.LogError(e, "Error was occured into plugin {0}", plugin);
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

                        var plugin = LoadPlugin(span[i]);
                        if (plugin != null && plugin.IsSupported(loader))
                        {
                            // slide
                            _leftPaths = _leftPaths.Slice(i + 1);

                            // todo どこかに対応付けをしておく
                            return plugin;
                        }
                    }

                    // not found
                    _leftPaths = _leftPaths.Slice(_leftPaths.Length); // slide to end
                }

                return null;
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
