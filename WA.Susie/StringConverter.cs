// (c) longod, MIT License

namespace WA.Susie
{
    using System.Text;

    public class StringConverter
    {
        private Encoding _target;
        private Encoding _original = Encoding.Unicode;

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
        public byte[] Encode(string text)
        {
            return Encoding.Convert(_original, _target, _original.GetBytes(text));
        }
    }
}
