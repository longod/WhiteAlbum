using System;
using WA.Susie;
using Xunit;

namespace WA.Test
{
    public class StringConverterTest
    {
        [Fact]
        public void TestEncodeToDecode()
        {
            string expected = "テスト文字列 地図　";
            var e = StringConverter.SJIS.Encode(expected);
            string actual = StringConverter.SJIS.Decode(e.Span);

            Assert.Equal(expected, actual);
        }

        // decode to encode
        // long word
        // short word

        // todo cache test
        // bench w/ cache w/o cache
    }
}
