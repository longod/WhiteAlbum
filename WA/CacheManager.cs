namespace WA
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    public class CacheManager<T>
    {
        private readonly ILogger _logger;

        // private int cacheCount = 8;

        // cache strategy
        // 1. 時間的局所性 最終参照時間
        // 2. 空間的局所性、ここでの局所性は、ファイルシステム上近いかどうか
        // スライドショーやシーク、ザッピングによって移動している場合に重要になりやすい （先読みが別途必要だが）
        // 3, ファイルサイズ、メモリ量 かなり優先度は低い。考慮しなくも恐らく問題ならない
        // 階層は一次でいいだろう

        readonly struct Key : IComparable<Key>
        {
            public int CompareTo([AllowNull] Key other)
            {
                throw new NotImplementedException();
            }
        }

        public CacheManager(AppSettings settings, ILogger logger)
        {
            _logger = logger;
        }

        internal bool TryQuery(string logicalPath, string virtualPath, out T hit)
        {
            using (new StopwatchScope("Query Cache", _logger))
            {
                // todo implement
                // todo always use fullpath
                // todo async?
                hit = default;
                return false;
            }
        }

        internal bool Entry(string logicalPath, string virtualPath, T entry)
        {
            using (new StopwatchScope("Entry Cache", _logger))
            {
                // todo implement
                // todo always use fullpath
                return false;
            }
        }
    }
}
