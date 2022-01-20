namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;

    // 古のMacBinaryを考慮するutility
    // susie pluginは issupport 内でそれぞれMacBinaryの場合を考慮しているらしいが、実際のでコード処理は考慮していないこともあるらしい
    // いずれにせよ実装依存なのでこちらが考慮してやった方がいいだろう
    internal static class MacBinaryUtility
    {
        private const int HeaderSize = 128;

        internal static ReadOnlyMemory<byte> CutHeader(ReadOnlyMemory<byte> binary)
        {
            // IsMacBinary 判定が正確になれば Binary 内で判定してオフセットをすればよい
            return binary.Slice(HeaderSize);
        }

        // todo test
        // V3以外は検出が複雑
        // https://entropymine.wordpress.com/2019/02/13/detecting-macbinary-format/
        // https://code.google.com/archive/p/theunarchiver/wikis/MacBinarySpecs.wiki
        // https://www.wdic.org/w/TECH/%E3%83%9E%E3%83%83%E3%82%AF%E3%83%90%E3%82%A4%E3%83%8A%E3%83%AA
        internal static bool? IsMacBinary(ReadOnlySpan<byte> binary)
        {
            // fixme get actual size
            if (binary.Length < HeaderSize)
            {
                return false;
            }

            var h = binary;

            // zero field and filename length
            if (h[0] == 0 && h[74] == 0 && h[82] == 0 && h[1] > 0)
            {
                // MacBinary II minimum version
                if (h[123] == 129)
                {
                    if (h[122] == 130)
                    {
                        // V3? MacBinary II version

                        // 'mBIN' big-endian?
                        var mbin = MemoryMarshal.Cast<byte, uint>(h.Slice(102))[0];
                        if (mbin == 0x6F42494E)
                        {
                            return true; // V3
                        }
                    }
                    else if (h[122] == 129)
                    {
                        // V2? MacBinary II version

                        // CRC-16-CCITT of [0-123] byte
                        // CRCアルゴリズムは指定されていないのと、実装によっては計算せずに0になっていることがあるのでアテにならない
                        var crc = MemoryMarshal.Cast<byte, ushort>(h.Slice(124))[0];

                        // swap for BE?
                        if (crc == 0 || crc == Crc16(h.Slice(0, 124)))
                        {
                            return true; // V2
                        }
                    }
                }

                return null; // possible V1
            }

            return false;
        }

        // todo test
        // for MacBinary CRC-16-CCITT
        // 左送りだが、右送りかどちらだ？
        private static ushort Crc16(ReadOnlySpan<byte> bin)
        {
            ushort crc = 0;
            foreach (var b in bin)
            {
                crc ^= (ushort)(b << 8);
                for (int j = 0; j < 8; ++j)
                {
                    // msb
                    if ((crc & 0x8000) == 0x8000)
                    {
                        const ushort polynomial = 0x1201;
                        crc = (ushort)((crc << 1) ^ polynomial);
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }

            return crc;
        }
    }
}
