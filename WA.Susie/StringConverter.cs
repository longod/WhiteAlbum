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
        private Decoder _decoder;
        private Encoder _encoder;
        private Dictionary<string, byte[]> _cache = new Dictionary<string, byte[]>();

        public StringConverter(string codePage)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // for sjis
            _target = Encoding.GetEncoding(codePage);
            _decoder = _target.GetDecoder();
            _encoder = _target.GetEncoder();
        }

        public static StringConverter SJIS { get; } = new StringConverter("shift-jis");

        [Obsolete]
        public string Decode(byte[] encodedBinaary)
        {
            return Decode(encodedBinaary, encodedBinaary.Length);
        }

        [Obsolete]
        public string Decode(byte[] encodedBinary, int length)
        {
            return Decode(encodedBinary.AsSpan(0, length));
        }

        public string Decode(ReadOnlySpan<byte> src)
        {
            // 入力の寿命が不明なので、キャッシュの場合コピーコストが生じる
            bool flush = false;
            var count = _decoder.GetCharCount(src, false);
            Span<char> desc = stackalloc char[count];
            _decoder.Convert(src, desc, flush, out int bytesUsed, out int charsUsed, out bool completed);

            return desc.ToString();
        }

        // FIXME プラグイン探索で何度も同じ文字を変換されるので効率が悪い。キャッシュとか
        public ReadOnlySpan<byte> Encode(string text)
        {
            if (_cache.TryGetValue(text, out var v))
            {
                return v;
            }

            // 恐らく、終端が文字の途中のcharで終わるような場合にflushする必要があるが、
            // stringから有効な文字を入力する正常系では殆ど必要なさそう
            bool flush = false;
            var src = text.AsSpan();
            var count = _encoder.GetByteCount(src, flush);
            byte[] dest = new byte[count]; // lengthを見ないで終端nullを期待しているコードがあるかもしれない
            _encoder.Convert(src, dest, flush, out int charsUsed, out int bytesUsed, out bool completed);
            if (!completed)
            {
                _encoder.Reset(); // flush
            }

            _cache.Add(text, dest);
            return dest;
        }
    }
}
