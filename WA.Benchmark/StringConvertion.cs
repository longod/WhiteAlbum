using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using WA.Susie;

namespace WA.Benchmark
{
    public class StringConvertion
    {
        StringConverter _cache;
        StringConverter _without;

        // parts of sample text from aozora bunko
        // public domain
        // https://www.aozora.gr.jp/cards/000879/card3817.html
        static readonly string[] _texts = {
               "或冬曇りの午後、わたしは中央線の汽車の窓に一列の山脈を眺めてゐた。",
               "山脈は勿論まつ白だつた。",
               "が、それは雪と言ふよりも山脈の皮膚に近い色をしてゐた。わたしはかう言ふ山脈を見ながら、ふと或小事件を思ひ出した。",
            };

        static readonly string[] _shortTexts = {
               "00IN",
               "00AM",
               "",
               "*.*",
            };

        [GlobalSetup]
        public void Setup()
        {
            _cache = new StringConverter("shift-jis", 256);
            _without = new StringConverter("shift-jis", 0);
        }

        [Benchmark]
        public void EncodeTextWithCache()
        {
            foreach (var text in _texts)
            {
                var _ = _cache.Encode(text);
            }
        }

        [Benchmark]
        public void EncodeTextWithoutCache()
        {
            foreach (var text in _texts)
            {
                var _ = _without.Encode(text);
            }
        }


        [Benchmark]
        public void EncodeShortTextWithCache()
        {
            foreach (var text in _shortTexts)
            {
                var _ = _cache.Encode(text);
            }
        }

        [Benchmark]
        public void EncodeShortTextWithoutCache()
        {
            foreach (var text in _shortTexts)
            {
                var _ = _without.Encode(text);
            }
        }

    }
}
