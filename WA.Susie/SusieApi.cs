// (c) longod, MIT License
namespace WA.Susie.API
{
    using System;
    using System.Runtime.InteropServices;

    // https://www.digitalpad.co.jp/~takechin/
    // spec: http://www2f.biglobe.ne.jp/~kana/spi_api/index.html
    // Susie, 元のアプリケーションとそのプラグインが開発された時期的に、文字セットはunicodeではない。恐らくほとんど全てのプラグインが文字セットをsjis前提としているはずである
    // そのため文字列のやりとりにはバイト列として扱う

    // todo full unmanaged

    // common
    // int _export PASCAL GetPluginInfo (int infono, LPSTR buf, int buflen);
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal delegate Int32 GetPluginInfo(Int32 infono, Byte[] buf, Int32 buflen);

    // int _export PASCAL IsSupported (LPSTR filename, DWORD dw);
    // dw はwin32ファイルハンドルか、最小2kbの先頭からのバイナリメモリ
    // Susieでは後者のバイナリメモリとしてしか使われていないので、後者のみをサポートする
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal delegate Int32 IsSupported(Byte[] filename, Byte[] dw);

    // int _export PASCAL ConfigurationDlg (HWND parent, int fnc)
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal delegate Int32 ConfigurationDlg(IntPtr parent, Int32 fnc);

    // 00IN Plug-in
    // int _export PASCAL GetPictureInfo (LPSTR buf, long len, unsigned int flag, PictureInfo *lpInfo);
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal delegate Int32 GetPictureInfo(Byte[] buf, Int32 len, UInt32 flag, PictureInfo info);

    // int _export PASCAL GetPicture (LPSTR buf, long len, unsigned int flag, HANDLE *pHBInfo, HANDLE *pHBm, FARPROC lpPrgressCallback, long lData);
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal unsafe delegate Int32 GetPicture(Byte[] buf, Int32 len, UInt32 flag, void** pHBInfo, void** pHBm, ProgressCallback lpPrgressCallback, Int32 lData);

    // int _export PASCAL GetPreview (LPSTR buf, long len, unsigned int flag, HANDLE *pHBInfo, HANDLE *pHBm, FARPROC lpPrgressCallback, long lData);
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal unsafe delegate Int32 GetPreview(Byte[] buf, Int32 len, UInt32 flag, void** pHBInfo, void** pHBm, ProgressCallback lpPrgressCallback, Int32 lData);

    // 00AM Plug-in
    // int _export PASCAL GetArchiveInfo (LPSTR buf, long len, unsigned int flag, HLOCAL *lphInf)
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal delegate Int32 GetArchiveInfo(Byte[] buf, Int32 len, UInt32 flag, IntPtr lphInf);

    // int _export PASCAL GetFileInfo (LPSTR buf, long len, LPSTR filename, unsigned int flag, fileInfo *lpInfo)
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal delegate Int32 GetFileInfo(Byte[] buf, Int32 len, Byte[] filename, UInt32 flag, FileInfo lpInfo);

    // int _export PASCAL GetFile (LPSTR src, long len, LPSTR dest, unsigned int flag, FARPROC prgressCallback, long lData)
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal delegate Int32 GetFile(Byte[] src, Int32 len, Byte[] dest, UInt32 flag, IntPtr prgressCallback, Int32 lData);

    // int PASCAL ProgressCallback(int nNum, int nDenom, long lData)
    // Cdecl ではなく StdCall
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate Int32 ProgressCallback(Int32 nNum, Int32 nDenom, Int32 lData);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct PictureInfo
    {
        public Int32 left;       // long 画像を展開する位置
        public Int32 top;        // long 画像を展開する位置
        public Int32 width;      // long 画像の幅(pixel)
        public Int32 height;     // long 画像の高さ(pixel)
        public UInt16 x_density; // WORD 画素の水平方向密度
        public UInt16 y_density; // WORD 画素の垂直方向密度
        public Int16 colorDepth; // short 画素当たりのbit数
        public IntPtr hInfo;     // HLOCAL 画像内のテキスト情報
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct FileInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public Byte[] method;   // unsigned char[8] 圧縮法の種類
        public UInt32 position; // unsigned long  ファイル上での位置
        public UInt32 compsize; // unsigned long  圧縮されたサイズ
        public UInt32 filesize; // unsigned long  元のファイルサイズ
        public Int32 timestamp; // time_t ファイルの更新日時
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
        public Byte[] path;     // char[200]  相対パス
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
        public Byte[] filename; // char[200] ファイルネーム
        public UInt32 crc;      // unsigned long CRC
    }

    internal enum ReturnCode
    {
        Success = 0,                // 正常終了
        NotImplemented = -1,        // その機能はインプリメントされていない
        FailedToProcess = 1,        // コールバック関数が非0を返したので展開を中止した
        UnkinownFormat = 2,         // 未知のフォーマット
        CorruptedData = 3,          // データが壊れている
        FailedToAllocateMemory = 4, // メモリーが確保できない
        MemoryError = 5,            // メモリーエラー(Lock出来ない、等)
        FailedToReadFile = 6,       // ファイルリードエラー
        Reserved = 7,               // 予約
        InternalError = 8,          // 内部エラー
    }
}
