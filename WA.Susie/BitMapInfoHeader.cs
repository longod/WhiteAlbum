// (c) longod, MIT License
namespace WA.Susie
{
    using System;
    using System.Runtime.InteropServices;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter

    // https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapinfoheader
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public readonly struct BitMapInfoHeader
    {
        public readonly UInt32 biSize;         // DWORD
        public readonly Int32 biWidth;         // LONG
        public readonly Int32 biHeight;        // LONG
        public readonly UInt16 biPlanes;       // WORD
        public readonly UInt16 biBitCount;     // WORD
        public readonly UInt32 biCompression;  // DWORD
        public readonly UInt32 biSizeImage;    // DWORD
        public readonly Int32 biXPelsPerMeter; // LONG
        public readonly Int32 biYPelsPerMeter; // LONG
        public readonly UInt32 biClrUsed;      // DWORD
        public readonly UInt32 biClrImportant; // DWORD
    }

#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter
}
