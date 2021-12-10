// (c) longod, MIT License

// TODO WA.Susie に分離する

namespace WA
{
    using System;
    using System.Runtime.InteropServices;

    namespace Susie
    {
        // https://www.digitalpad.co.jp/~takechin/
        // spec: http://www2f.biglobe.ne.jp/~kana/spi_api/index.html
        // Susie, 元のアプリケーションとそのプラグインが開発された時期的に、文字セットはunicodeではない。恐らくほとんど全てのプラグインが文字セットをsjis前提としているはずである
        // そのため文字列のやりとりにはバイト列として扱う

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
        //internal unsafe delegate Int32 GetPicture(Byte[] buf, Int32 len, UInt32 flag, void** pHBInfo, void** pHBm, ProgressCallback lpPrgressCallback, Int32 lData);
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

        internal static class ExportName
        {
            internal static readonly string GetPluginInfo = typeof(GetPluginInfo).Name;
            internal static readonly string IsSupported = typeof(IsSupported).Name;
            internal static readonly string ConfigurationDlg = typeof(ConfigurationDlg).Name;
            internal static readonly string GetPictureInfo = typeof(GetPictureInfo).Name;
            internal static readonly string GetPicture = typeof(GetPicture).Name;
            internal static readonly string GetPreview = typeof(GetPreview).Name;
            internal static readonly string GetArchiveInfo = typeof(GetArchiveInfo).Name;
            internal static readonly string GetFileInfo = typeof(GetFileInfo).Name;
            internal static readonly string GetFile = typeof(GetFile).Name;
        }

        // unsafe じゃないとだめかも
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

        // unsafe じゃないとだめかも
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

        // can readonly?

        // https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapinfoheader
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct BitMapInfo
        {
            public UInt32 biSize; // DWORD
            public Int32 biWidth; // LONG
            public Int32 biHeight; // LONG
            public UInt16 biPlanes; // WORD
            public UInt16 biBitCount; // WORD
            public UInt32 biCompression; // DWORD
            public UInt32 biSizeImage;  // DWORD
            public Int32 biXPelsPerMeter; // LONG
            public Int32 biYPelsPerMeter; //LONG
            public UInt32 biClrUsed; // DWORD
            public UInt32 biClrImportant; // DWORD
        }

        internal enum ReturnCode
        {
            Success = 0,                // 正常終了
            NotImplemented = -1,        // その機能はインプリメントされていない
            FailedToExtract = 1,        // コールバック関数が非0を返したので展開を中止した
            UnkinownFormat = 2,         // 未知のフォーマット
            CorruptedData = 3,          // データが壊れている
            FailedToAllocateMemory = 4, // メモリーが確保できない
            MemoryError = 5,            // メモリーエラー(Lock出来ない、等)
            FailedToReadFile = 6,       // ファイルリードエラー
            Reserved = 7,               // 予約
            InternalError = 8,          // 内部エラー
        }

    }

    internal interface IPlugin
    {
    }

    public class SusiePlugin : IPlugin, IDisposable
    {
        private enum PluginType
        {
            ImportFilter,
            ExportFilter, // 存在しない？
            ArchiveExtractor,
        }

        private enum PluginTarget
        {
            Normal, // 恐らく事実上 ImportFilter と固定
            MultiPicture, // 恐らく事実上 ArchiveExtractor と固定
        }

        private IntPtr _handle = default;
        private int _version = 0;
        private PluginType _pluginType;
        private PluginTarget _pluginTarget;
        private string _pluginName;

        private struct Function
        {
            // common
            internal Susie.GetPluginInfo GetPluginInfo;
            internal Susie.IsSupported IsSupported;
            internal Susie.ConfigurationDlg ConfigurationDlg;

            // IN
            internal Susie.GetPictureInfo GetPictureInfo;
            internal Susie.GetPicture GetPicture;
            internal Susie.GetPreview GetPreview;

            // AM
            internal Susie.GetArchiveInfo GetArchiveInfo;
            internal Susie.GetFileInfo GetFileInfo;
            internal Susie.GetFile GetFile;
        }

        private Function _func;

        private readonly StringConverter _stringConverter = null;

        private static readonly string _extension = ".spi";

        private const int _pluginVersionNum = 0;
        private const int _pluginNameNum = 1;

        private const int _minPeekSize = 2048; // 2 kbytes

        public SusiePlugin(string path)
        {
            _stringConverter = StringConverter.SJIS;
            _handle = NativeLibrary.Load(path);

            GetPluginVersion();


            // test
            //GetPluginName();
            //System.Diagnostics.Trace.WriteLine(_pluginName);
#if false
            {
                string jpg = @"..\..\..\..\Temp\image\no_animation.gif";
                var spath = _stringConverter.Encode(jpg);

                byte[] binary = null;
                using (var stream = System.IO.File.OpenRead(jpg))
                {
                    binary = new byte[stream.Length];
                    stream.Read(binary); // or async
                }

                if (IsSupported(spath, binary))
                {
                    var image = GetPicture(binary);
                }
            }
#endif
        }


        public void Free()
        {
            if (_handle != default)
            {
                NativeLibrary.Free(_handle);
                _handle = default;
            }
        }

        public void Dispose()
        {
            Free();
        }

        private static T GetFunction<T>(IntPtr handle, string name)
        {
            // https://qiita.com/kenichiuda/items/613766f56e5ecd1de856
            var func = NativeLibrary.GetExport(handle, name);
            return Marshal.GetDelegateForFunctionPointer<T>(func);
        }

        private void GetPluginVersion()
        {
            // get mandatory function
            if (_func.GetPluginInfo == null)
            {
                _func.GetPluginInfo = GetFunction<Susie.GetPluginInfo>(_handle, Susie.ExportName.GetPluginInfo);
            }

            byte[] buf = new byte[32]; // or stackalloc
            var length = _func.GetPluginInfo(_pluginVersionNum, buf, buf.Length);
            if (length > 6) // 4byts固定だが、終端を含めてさらに余分に返すケースがある
            {
                throw new Exception("failed to get plugin info");
            }

            // ascii number 0 to 9
            // sjisだが二桁のascii数字なので、そのままオフセットして求める
            _version = ((buf[0] - 0x30) * 10) + (buf[1] - 0x30);

            // ascii alphabet A to Z
            // sjisだが、ascii範囲内なので、そのままキャストして判別する
            switch ((char)buf[2])
            {
                case 'I':
                    _pluginType = PluginType.ImportFilter;
                    break;
                case 'X':
                    _pluginType = PluginType.ExportFilter;
                    break;
                case 'A':
                    _pluginType = PluginType.ArchiveExtractor;
                    break;
                default:
                    throw new Exception("failed to get plugin version [2]");
            }

            switch ((char)buf[3])
            {
                case 'N':
                    _pluginTarget = PluginTarget.Normal;
                    break;
                case 'M':
                    _pluginTarget = PluginTarget.MultiPicture;
                    break;
                default:
                    throw new Exception("failed to get plugin version [3]");
            }
        }

        private void GetPluginName()
        {
            if (_pluginName != null)
            {
                return;
            }

            // get mandatory function
            if (_func.GetPluginInfo == null)
            {
                _func.GetPluginInfo = GetFunction<Susie.GetPluginInfo>(_handle, Susie.ExportName.GetPluginInfo);
            }

            byte[] buf = new byte[256]; // or stackalloc
            var length = _func.GetPluginInfo(_pluginNameNum, buf, buf.Length);
            if (length > 0)
            {
                _pluginName = _stringConverter.Decode(buf, length);
            }
            else
            {
                // Not Implemented
                _pluginName = "Not Implemented";
            }
        }

        internal bool IsSupported(byte[] path, byte[] binary)
        {
            byte[] peek;
            if (binary.Length < _minPeekSize)
            {
                throw new ArgumentException($"binary.Length larger than {_minPeekSize} (has {binary.Length}).");

                //peek = new byte[minPeekSize];
                //// todo benchmark copy moethods
                //Buffer.BlockCopy(binary, 0, peek, 0, binary.Length);
            }
            else
            {
                peek = binary;
            }

            if (_func.IsSupported == null)
            {
                _func.IsSupported = GetFunction<Susie.IsSupported>(_handle, Susie.ExportName.IsSupported);
            }

            var result = _func.IsSupported(path, peek);
            return result != 0;
        }

        // [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions] // coreは効かないはず

        private static int AlwaysContinueProgressCallback(int nNum, int nDenom, int lData)
        {
            return 0; // always continue
        }

        internal bool GetPicture(byte[] binary, out byte[] image, out Susie.BitMapInfo info)
        {
            if (_func.GetPicture == null)
            {
                _func.GetPicture = GetFunction<Susie.GetPicture>(_handle, Susie.ExportName.GetPicture);
            }
            const uint flag = 1;// 0: filehandle, 1:on memory

            image = null;
            info = default;

            unsafe
            {
                //IntPtr ptr = (IntPtr)p;
                //IntPtr ptr;
                void* pHBInfo = null;
                void* pHBm = null;

                var result = _func.GetPicture(binary, binary.Length, flag, &pHBInfo, &pHBm, AlwaysContinueProgressCallback, 0);
                if (result == 0)
                {
                    if (pHBInfo != null)
                    {
                        var ptr = (Susie.BitMapInfo*)NativeMethods.LocalLock(pHBInfo);

                        Susie.BitMapInfo bi = default;
                        System.Runtime.CompilerServices.Unsafe.Copy(ref bi, ptr);
                        info = bi;

                        NativeMethods.LocalUnlock(pHBInfo);
                    }

                    if (pHBm != null)
                    {
                        var ptr = NativeMethods.LocalLock(pHBm);
                        // copy managed memory

                        image = new byte[info.biSizeImage];
                        fixed (void* p = image)
                        {
                            System.Runtime.CompilerServices.Unsafe.CopyBlock(p, ptr, info.biSizeImage);
                        }

                        NativeMethods.LocalUnlock(pHBm);
                    }
                }

                if (pHBInfo != null)
                {
                    NativeMethods.LocalFree(pHBInfo);
                }

                if (pHBm != null)
                {
                    NativeMethods.LocalFree(pHBm);
                }

                return result == 0;
            }
            return false;
        }
    }
}
