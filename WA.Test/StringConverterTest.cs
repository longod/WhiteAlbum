using System;
using System.Collections;
using System.Collections.Generic;
using WA.Susie;
using Xunit;

namespace WA.Test
{

    public class StringConverterTest
    {
        public class TestDataClass : IEnumerable<object[]>
        {
            List<object[]> _testData = new List<object[]>();

            public TestDataClass()
            {
                _testData.Add(new object[] { new StringConverter("shift-jis") }); // with cache
                _testData.Add(new object[] { new StringConverter("shift-jis", 0) }); // without cache
            }

            public IEnumerator<object[]> GetEnumerator() => _testData.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(TestDataClass))]
        public void TestEncodeToDecode(StringConverter converter)
        {
            string expected = "テスト文字列 地図　";
            for (int j = 0; j < 2; ++j)
            {
                var e = converter.Encode(expected);
                string actual = converter.Decode(e.Span);
                Assert.Equal(expected, actual);
            }
        }

        [Theory]
        [ClassData(typeof(TestDataClass))]
        public void TestDecodeToEncode(StringConverter converter)
        {
            Span<byte> expected = stackalloc byte[4];
            expected[0] = 0x49; // I
            expected[1] = 0x4e; // N
            expected[2] = 0x41; // A
            expected[3] = 0x4d; // M

            for (int j = 0; j < 2; ++j)
            {
                var e = converter.Decode(expected);
                var actual = converter.Encode(e);
                Assert.Equal(expected.Length, actual.Span.Length);
                for (int i = 0; i < actual.Span.Length; ++i)
                {
                    Assert.Equal(expected[i], actual.Span[i]);
                }
            }
        }


        [Theory]
        [ClassData(typeof(TestDataClass))]
        public void TestDecodeToEncodeWithZero(StringConverter converter)
        {
            Span<byte> expected = stackalloc byte[5];
            expected[0] = 0x49; // I
            expected[1] = 0x4e; // N
            expected[2] = 0x41; // A
            expected[3] = 0x4d; // M
            expected[4] = 0;
            for (int j = 0; j < 2; ++j)
            {
                var e = converter.Decode(expected);
                var actual = converter.Encode(e);
                Assert.Equal(expected.Length - 1, actual.Span.Length);
                for (int i = 0; i < actual.Span.Length; ++i)
                {
                    Assert.Equal(expected[i], actual.Span[i]);
                }
            }
        }

        [Theory]
        [ClassData(typeof(TestDataClass))]
        public void TestDecodeWithZeroTerminate(StringConverter converter)
        {
            Span<byte> expected = stackalloc byte[5];
            expected[0] = 0x49; // I
            expected[1] = 0x4e; // N
            expected[2] = 0;
            expected[3] = 0x4d; // dummy
            expected[4] = 0;
            var actual = converter.DecodeWithZeroTerminate(expected);
            Assert.Equal(2, actual.Length);
            Assert.Equal("IN", actual);
        }

        [Theory]
        [ClassData(typeof(TestDataClass))]
        public void TestDecodeWithZeroTerminateUnsafe(StringConverter converter)
        {
            Span<byte> expected = stackalloc byte[5];
            expected[0] = 0x49; // I
            expected[1] = 0x4e; // N
            expected[2] = 0;
            expected[3] = 0x4d; // dummy
            expected[4] = 0;
            unsafe
            {
                fixed (byte* ptr = expected)
                {
                    var actual = converter.DecodeWithZeroTerminateUnsafe(ptr);
                    Assert.Equal(2, actual.Length);
                    Assert.Equal("IN", actual);
                }
            }
        }

        [Theory]
        [ClassData(typeof(TestDataClass))]
        public void TestEncodeNull(StringConverter converter)
        {
            Assert.Equal(0, converter.Encode(null).Length);
        }

        [Theory]
        [ClassData(typeof(TestDataClass))]
        public void TestDecodeNull(StringConverter converter)
        {
            Assert.Equal(string.Empty, converter.Decode(null));
            Assert.Equal(string.Empty, converter.DecodeWithZeroTerminate(new ReadOnlySpan<byte>(null)));
            unsafe
            {
                Assert.Equal(string.Empty, converter.DecodeWithZeroTerminateUnsafe((byte*)null));
            }
        }

        [Theory]
        [ClassData(typeof(TestDataClass))]
        public void TestEncodeEmpty(StringConverter converter)
        {
            Assert.Equal(0, converter.Encode(string.Empty).Length);
        }

        [Theory]
        [ClassData(typeof(TestDataClass))]
        public void TestDecodeEmpty(StringConverter converter)
        {
            ReadOnlySpan<byte> zero = stackalloc byte[0];
            Assert.Equal(string.Empty, converter.Decode(zero));
            Assert.Equal(string.Empty, converter.DecodeWithZeroTerminate(zero));
            unsafe
            {
                fixed (byte* p = zero)
                {
                    Assert.Equal(string.Empty, converter.DecodeWithZeroTerminateUnsafe(p));
                }
            }
        }

    }
}
