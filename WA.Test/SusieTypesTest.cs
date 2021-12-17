using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace WA.Test
{
    public class SusieTypesTest
    {
        [Fact]
        public void TestBitMapInfoHeaderSize()
        {
            Assert.Equal(40, Marshal.SizeOf<Susie.BitMapInfoHeader>());
        }

        [Fact]
        public void TestRGBQuadSize()
        {
            Assert.Equal(4, Marshal.SizeOf<Susie.RGBQuad>());
        }

        [Fact]
        public void TestPictureInfoSize()
        {
            Assert.Equal(26, Marshal.SizeOf<Susie.API.PictureInfo>());
        }

        [Fact]
        public void TestFileInfoSize()
        {
            Assert.Equal(20 + 8 + 200 + 200, Marshal.SizeOf<Susie.API.FileInfo>());
        }

    }
}
