using System.Text;

namespace WA
{
    internal class StringConverter
    {
        Encoding target;
        Encoding original = Encoding.Unicode;

        internal static StringConverter SJIS { get; } = new StringConverter("shift-jis");


        internal StringConverter(string codepage)
        {
            // for sjis
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            target = Encoding.GetEncoding(codepage);
        }

        internal string Decode(byte[] encodedBinaary)
        {
            return Decode(encodedBinaary, encodedBinaary.Length);
        }

        internal string Decode(byte[] encodedBinaary, int length)
        {
            return original.GetString(Encoding.Convert(target, original, encodedBinaary, 0, length));
        }

        internal byte[] Encode(string text)
        {
            return Encoding.Convert(original, target, original.GetBytes(text));
        }
    }
}
