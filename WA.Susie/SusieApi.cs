// (c) longod, MIT License
namespace WA.Susie.API
{
    using System;
    using System.Runtime.InteropServices;

    // https://www.digitalpad.co.jp/~takechin/
    // spec: http://www2f.biglobe.ne.jp/~kana/spi_api/index.html
    // Susie, 元のアプリケーションとそのプラグインが開発された時期的に、文字セットはunicodeではない。恐らくほとんど全てのプラグインが文字セットをsjis前提としているはずである
    // そのため文字列のやりとりにはバイト列として扱う

    // common
    // int _export PASCAL GetPluginInfo (int infono, LPSTR buf, int buflen);
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal unsafe delegate Int32 GetPluginInfo(Int32 infono, void* buf, Int32 buflen);

    // int _export PASCAL IsSupported (LPSTR filename, DWORD dw);
    // dw はwin32ファイルハンドルか、最小2kbの先頭からのバイナリメモリ
    // Susieでは後者のバイナリメモリとしてしか使われていないので、後者のみをサポートする
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal unsafe delegate Int32 IsSupported(void* filename, void* dw);

    // int _export PASCAL ConfigurationDlg (HWND parent, int fnc)
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal unsafe delegate Int32 ConfigurationDlg(void* parent, Int32 fnc);

    // 00IN Plug-in
    // int _export PASCAL GetPictureInfo (LPSTR buf, long len, unsigned int flag, PictureInfo *lpInfo);
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal unsafe delegate Int32 GetPictureInfo(void* buf, Int32 len, UInt32 flag, PictureInfo* info);

    // int _export PASCAL GetPicture (LPSTR buf, long len, unsigned int flag, HANDLE *pHBInfo, HANDLE *pHBm, FARPROC lpPrgressCallback, long lData);
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal unsafe delegate Int32 GetPicture(void* buf, Int32 len, UInt32 flag, void** pHBInfo, void** pHBm, ProgressCallback lpPrgressCallback, Int32 lData);

    // int _export PASCAL GetPreview (LPSTR buf, long len, unsigned int flag, HANDLE *pHBInfo, HANDLE *pHBm, FARPROC lpPrgressCallback, long lData);
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal unsafe delegate Int32 GetPreview(void* buf, Int32 len, UInt32 flag, void** pHBInfo, void** pHBm, ProgressCallback lpPrgressCallback, Int32 lData);

    // 00AM Plug-in
    // int _export PASCAL GetArchiveInfo (LPSTR buf, long len, unsigned int flag, HLOCAL *lphInf)
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal unsafe delegate Int32 GetArchiveInfo(void* buf, Int32 len, UInt32 flag, void** lphInf);

    // int _export PASCAL GetFileInfo (LPSTR buf, long len, LPSTR filename, unsigned int flag, fileInfo *lpInfo)
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal unsafe delegate Int32 GetFileInfo(void* buf, Int32 len, void* filename, UInt32 flag, FileInfo* lpInfo);

    // int _export PASCAL GetFile (LPSTR src, long len, LPSTR dest, unsigned int flag, FARPROC prgressCallback, long lData)
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto)]
    internal unsafe delegate Int32 GetFile(void* src, Int32 len, void* dest, UInt32 flag, ProgressCallback prgressCallback, Int32 lData);

    // int PASCAL ProgressCallback(int nNum, int nDenom, long lData)
    // Cdecl ではなく StdCall
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate Int32 ProgressCallback(Int32 nNum, Int32 nDenom, Int32 lData);

    public static class Constant
    {
        public const int MinFileSize = 2048; // 2 kbytes

        public const string Extension = ".spi";

        // GetPluginInfo
        internal const Int32 PluginVersion = 0;
        internal const Int32 PluginName = 1;
        internal const Int32 PluginFileFormat = 2;

        // GetPictureInfo, GetPicture, GetPreview, GetArchiveInfo, GetFileInfo, GetFile
        internal const UInt32 OnFile = 0b000;
        internal const UInt32 OnMemory = 0b001;

        // GetFile (alias)
        internal const UInt32 SrcOnFile = OnFile;
        internal const UInt32 SrcOnMemory = OnMemory;
        internal const UInt32 DestOnFile = OnFile << 16;
        internal const UInt32 DestOnMemory = OnMemory << 16;

        // GetFileInfo
        internal const UInt32 CaseSensitive = 0b0;
        internal const UInt32 CaseInsensitive = 0b10000000;

        // ConfigurationDlg
        internal const UInt32 DialogAbout = 0;
        internal const UInt32 DialogSettings = 1;
        internal const UInt32 DialogReserved = 2;
    }

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1310 // Field names should not contain underscore
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal unsafe struct PictureInfo
    {
        public Int32 left;       // long 画像を展開する位置
        public Int32 top;        // long 画像を展開する位置
        public Int32 width;      // long 画像の幅(pixel)
        public Int32 height;     // long 画像の高さ(pixel)
        public UInt16 x_density; // WORD 画素の水平方向密度
        public UInt16 y_density; // WORD 画素の垂直方向密度
        public Int16 colorDepth; // short 画素当たりのbit数
        public void* hInfo;      // HLOCAL 画像内のテキスト情報
    }
#pragma warning restore SA1310 // Field names should not contain underscore

    // or use https://ufcpp.net/study/csharp/sp_unsafe.html#fixed-buffer
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal unsafe struct FileInfo
    {
        public fixed Byte method[8];     // unsigned char[8] 圧縮法の種類
        public UInt32 position;          // unsigned long  ファイル上での位置
        public UInt32 compsize;          // unsigned long  圧縮されたサイズ
        public UInt32 filesize;          // unsigned long  元のファイルサイズ
        public Int32 timestamp;          // time_t ファイルの更新日時
        public fixed Byte path[200];     // char[200]  相対パス
        public fixed Byte filename[200]; // char[200] ファイルネーム
        public UInt32 crc;               // unsigned long CRC
    }
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

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
