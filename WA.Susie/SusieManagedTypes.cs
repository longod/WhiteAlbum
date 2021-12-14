// (c) longod, MIT License
namespace WA.Susie
{
    using System;

    public readonly struct PictureInfo
    {
        public readonly int Left;         // long 画像を展開する位置
        public readonly int Top;          // long 画像を展開する位置
        public readonly int Width;        // long 画像の幅(pixel)
        public readonly int Height;       // long 画像の高さ(pixel)
        public readonly ushort DensityX;  // WORD 画素の水平方向密度
        public readonly ushort DensityY;  // WORD 画素の垂直方向密度
        public readonly short ColorDepth; // short 画素当たりのbit数
        public readonly string Info;      // HLOCAL 画像内のテキスト情報

        internal unsafe PictureInfo(API.PictureInfo* info, StringConverter stringConverter)
        {
            Left = info->left;
            Top = info->top;
            Width = info->width;
            Height = info->height;
            DensityX = info->x_density;
            DensityY = info->y_density;
            ColorDepth = info->colorDepth;
            if (info->hInfo != null)
            {
                // Spi_api.txt には Globalメモリーのハンドルと書いてあるが、実際の型は HGLOBAL ではなく、HLOCAL である。どっちだ？
                var ptr = (byte*)NativeMethods.LocalLock(info->hInfo);
                Info = stringConverter.DecodeWithZeroTerminate(ptr);
                NativeMethods.LocalUnlock(info->hInfo);
            }
            else
            {
                Info = null;
            }
        }
    }

    public readonly struct FileInfo
    {
        public readonly string Method;   // unsigned char[8] 圧縮法の種類
        public readonly uint Position;   // unsigned long  ファイル上での位置
        public readonly uint CompSize;    // unsigned long  圧縮されたサイズ
        public readonly uint FileSize;   // unsigned long  元のファイルサイズ
        public readonly int Timestamp;   // time_t ファイルの更新日時
        public readonly string Path;     // char[200]  相対パス
        public readonly string FileName; // char[200] ファイルネーム
        public readonly uint Crc;        // unsigned long CRC

        internal unsafe FileInfo(API.FileInfo* info, StringConverter stringConverter)
        {
            Method = stringConverter.DecodeWithZeroTerminate(new ReadOnlySpan<byte>(info->method, API.FileInfo.methodSize));
            Position = info->position;
            CompSize = info->compsize;
            FileSize = info->filesize;
            Timestamp = info->timestamp;
            Path = stringConverter.DecodeWithZeroTerminate(new ReadOnlySpan<byte>(info->path, API.FileInfo.pathSize));
            FileName = stringConverter.DecodeWithZeroTerminate(new ReadOnlySpan<byte>(info->filename, API.FileInfo.filenameSize));
            Crc = info->crc;
        }
    }
}
