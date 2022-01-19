namespace WA
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;

    internal class CacheManager<T>
    {
        private readonly ILogger _logger;

        private int cacheSizeLimit = 8;

        // cache strategy
        // 1. 時間的局所性 最終参照時間
        // 2. 空間的局所性、ここでの局所性は、ファイルシステム上近いかどうか
        // スライドショーやシーク、ザッピングによって移動している場合に重要になりやすい （先読みが別途必要だが）
        // 3, ファイルサイズ、メモリ量 かなり優先度は低い。考慮しなくも恐らく問題ならない
        // 階層は一次でいいだろう

        // todo test
        // https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-3.1#use-setsize-size-and-sizelimit-to-limit-cache-size

        // todo concurrent
        // https://michaelscodingspot.com/cache-implementations-in-csharp-net/

        private IMemoryCache _cache;

        // objectだが、string想定されているようなのでstringでkeyを表現する
        static string GetKey(string logicalPath, string virtualPath)
        {
            logicalPath = Path.GetFullPath(logicalPath); // always fullpath
            if (string.IsNullOrEmpty(virtualPath))
            {
                return logicalPath;
            }

            // デリミタをvirtual pathと同様にして混ざらないだろうか？混ざるようならほかにつかえるのは ? or * あたり
            return string.Concat(logicalPath, "|", virtualPath);
        }

        public CacheManager(AppSettings settings, ILogger logger)
        {
            _logger = logger;

            // todo injection
            // genericじゃないのがやりにくい。どうやってもboxingが生じる。
            // keyが不明瞭 addressで評価されても困る。なんらかinterfaceでも要求してくれ
            var options = new MemoryCacheOptions();
            options.SizeLimit = cacheSizeLimit; // sizelimitは個数は全体の個数では無さそう
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        internal bool TryQuery(string logicalPath, string virtualPath, out T hit)
        {
            using (new StopwatchScope("Query Cache", _logger))
            {
                if (_cache.TryGetValue(GetKey(logicalPath, virtualPath), out var cacheEntry))
                {
                    if (cacheEntry is T t)
                    {
                        hit = t;
                        return true;
                    }
                }

                hit = default;
                return false;
            }
        }

        internal void Entry(string logicalPath, string virtualPath, T entry)
        {
            using (new StopwatchScope("Entry Cache", _logger))
            {
                _cache.Set(GetKey(logicalPath, virtualPath), entry);
            }
        }
    }
}
