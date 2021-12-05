// (c) longod, MIT License

namespace WA
{
    using System.Text;

    internal class StringConverter
    {
        private Encoding _target;
        private Encoding _original = Encoding.Unicode;

        internal StringConverter(string codepage)
        {
            // for sjis
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _target = Encoding.GetEncoding(codepage);
        }

        internal static StringConverter SJIS { get; } = new StringConverter("shift-jis");

        internal string Decode(byte[] encodedBinaary)
        {
            return Decode(encodedBinaary, encodedBinaary.Length);
        }

        internal string Decode(byte[] encodedBinary, int length)
        {
            return _original.GetString(Encoding.Convert(_target, _original, encodedBinary, 0, length));
        }

        internal byte[] Encode(string text)
        {
            return Encoding.Convert(_original, _target, _original.GetBytes(text));
        }
    }
}
