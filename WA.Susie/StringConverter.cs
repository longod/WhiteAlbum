// (c) longod, MIT License

namespace WA.Susie
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class StringConverter
    {
        private Encoding _target;
        private Encoding _original = Encoding.Unicode;
        private Dictionary<string, byte[]> _cache = new Dictionary<string, byte[]>();

        public StringConverter(string codePage)
        {
            // for sjis
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _target = Encoding.GetEncoding(codePage);
        }

        public static StringConverter SJIS { get; } = new StringConverter("shift-jis");

        public string Decode(byte[] encodedBinaary)
        {
            return Decode(encodedBinaary, encodedBinaary.Length);
        }

        public string Decode(byte[] encodedBinary, int length)
        {
            return _original.GetString(Encoding.Convert(_target, _original, encodedBinary, 0, length));
        }

        // FIXME プラグイン探索で何度も同じ文字を変換されるので効率が悪い。キャッシュとか
        // todo ReadOnlySpan<byte>
        public byte[] Encode(string text)
        {
            if (_cache.TryGetValue(text, out var v))
            {
                return v;
            }

            var bin = Encoding.Convert(_original, _target, _original.GetBytes(text));
            _cache.Add(text, bin);
            return bin;
        }
    }
}
