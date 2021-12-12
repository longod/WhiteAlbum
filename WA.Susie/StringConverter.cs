// (c) longod, MIT License

namespace WA.Susie
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class StringConverter
    {
        private Decoder _decoder;
        private Encoder _encoder;
        private Dictionary<string, byte[]> _cache;

        public StringConverter(string codePage, int cacheCapacity = 256)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // for sjis
            var encoding = Encoding.GetEncoding(codePage);
            _decoder = encoding.GetDecoder();
            _encoder = encoding.GetEncoder();

            if (cacheCapacity > 0)
            {
                _cache = new Dictionary<string, byte[]>(cacheCapacity);
            }
        }

        public static StringConverter SJIS { get; } = new StringConverter("shift-jis");

        public string Decode(ReadOnlySpan<byte> src)
        {
            // 入力の寿命が不明なので、キャッシュの場合コピーコストが生じる
            bool flush = false;
            var count = _decoder.GetCharCount(src, false);
            Span<char> desc = (count < 64) ? stackalloc char[count] : new char[count]; // avoid large stack allocation
            _decoder.Convert(src, desc, flush, out int bytesUsed, out int charsUsed, out bool completed);

            return desc.ToString();
        }

        // 用途が限定されているのなら ReadOnlySpan でも成立するはず
        public ReadOnlyMemory<byte> Encode(string text)
        {
            // fixme calling from multi thread. lock or concurrent
            if (_cache != null && _cache.TryGetValue(text, out var v))
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
