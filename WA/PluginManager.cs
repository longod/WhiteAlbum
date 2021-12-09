namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Linq;
    using System.Diagnostics.CodeAnalysis;

    public class PluginManager
    {
        private List<string> _pluginDirectories = new List<string>() { @"..\..\..\..\Temp\spi\" };

        private bool _searchSubDirectory = true; // directory単位で持つかも

        private string[] _pluginPaths = null;

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

        public void FindPlugins()
        {
            var eq = new SameNameFileInfoEQ();
            using (new StopwatchScope("FindPlugins"))
            {
                _pluginPaths = _pluginDirectories.SelectMany(x => SearchPlugin(new DirectoryInfo(x), _searchSubDirectory))
                    .Distinct(eq) // unique
                    .Select(x => x.FullName)
                    .ToArray();
            }
        }

        // test
        public void LoadAllPlugins()
        {
            using (new StopwatchScope("LoadAllPlugins"))
            {
                foreach (var path in _pluginPaths)
                {
                    try
                    {
                        new SusiePlugin(path);
                    }
                    catch (DllNotFoundException e)
                    {
                        // 多分 dependency dllが読めていない
                    }
                }
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
    }
}
