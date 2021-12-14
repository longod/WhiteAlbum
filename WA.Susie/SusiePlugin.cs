// (c) longod, MIT License

namespace WA.Susie
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    // todo susie以外のプラグインに対応する場合にインターフェイスを揃えたい
    internal interface IPlugin : IDisposable
    {
    }

    public class SusiePlugin : IPlugin
    {
        public enum PluginType
        {
            ImportFilter,
            ArchiveExtractor,
            ExportFilter, // 存在しない？
        }

        private enum PluginTarget
        {
            Normal, // 恐らく事実上 ImportFilter と固定
            MultiPicture, // 恐らく事実上 ArchiveExtractor と固定
        }

        private struct Function
        {
            // common
            internal API.GetPluginInfo GetPluginInfo;
            internal API.IsSupported IsSupported;
            internal API.ConfigurationDlg ConfigurationDlg;

            // IN
            internal API.GetPictureInfo GetPictureInfo;
            internal API.GetPicture GetPicture;
            internal API.GetPreview GetPreview;

            // AM
            internal API.GetArchiveInfo GetArchiveInfo;
            internal API.GetFileInfo GetFileInfo;
            internal API.GetFile GetFile;
        }

        private readonly StringConverter _stringConverter = null;

        private IntPtr _handle;
        private int _version = 0;
        private PluginTarget _pluginTarget;
        private string _pluginName;
        private Function _func;
        private List<Tuple<string, string>> _fileFormats;

        public PluginType Type { get; private set; }

        private static int AlwaysContinueProgressCallback(int nNum, int nDenom, int lData)
        {
            return 0; // always continue
        }

        public SusiePlugin(string path, StringConverter stringConverter)
        {
            _stringConverter = stringConverter;
            _handle = NativeLibrary.Load(path);

            GetPluginVersion();

            // GetPluginName();
            // GetFileFormats();
        }

        public void Dispose()
        {
            NativeLibrary.Free(_handle);
        }

        public bool IsSupported(string path, ReadOnlyMemory<byte> binary)
        {
            if (binary.Length < API.Constant.MinFileSize)
            {
                throw new ArgumentException($"binary.Length larger than {API.Constant.MinFileSize} (has {binary.Length}).");
            }

            if (_func.IsSupported == null)
            {
                _func.IsSupported = GetFunction<API.IsSupported>(_handle);
            }

            unsafe
            {
                int result = 0;
                var mbpath = _stringConverter.Encode(path);
                using (var ptr = mbpath.Pin())
                {
                    using (var handle = binary.Pin())
                    {
                        result = _func.IsSupported(ptr.Pointer, handle.Pointer);
                    }
                }

                return result != 0;
            }
        }

        public void ConfigurationDlg()
        {
            if (_func.ConfigurationDlg == null)
            {
                _func.ConfigurationDlg = GetFunction<API.ConfigurationDlg>(_handle);
            }

            // need hwnd
            // https://stackoverflow.com/questions/10675305/how-to-get-the-hwnd-of-window-instance
            throw new NotImplementedException();
        }

        public bool GetPicture(ReadOnlyMemory<byte> binary, out byte[] image, out BitMapInfoHeader info)
        {
            if (Type != PluginType.ImportFilter)
            {
                throw new InvalidOperationException();
            }

            if (_func.GetPicture == null)
            {
                _func.GetPicture = GetFunction<API.GetPicture>(_handle);
            }

            image = null;
            info = default;

            const uint flag = API.Constant.OnMemory;

            unsafe
            {
                void* pHBInfo = null;
                void* pHBm = null;
                int result = 0;
                using (var handle = binary.Pin())
                {
                    result = _func.GetPicture(handle.Pointer, binary.Length, flag, &pHBInfo, &pHBm, AlwaysContinueProgressCallback, 0);
                }

                if (result == 0)
                {
                    if (pHBInfo != null)
                    {
                        var ptr = (BitMapInfoHeader*)NativeMethods.LocalLock(pHBInfo);

                        // copy to managed memory
                        Unsafe.Copy(ref info, ptr);

                        NativeMethods.LocalUnlock(pHBInfo);
                    }

                    if (pHBm != null)
                    {
                        var ptr = NativeMethods.LocalLock(pHBm);

                        // copy to managed memory
                        image = new byte[info.biSizeImage];
                        fixed (void* p = image)
                        {
                            Unsafe.CopyBlock(p, ptr, (uint)image.Length);
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
        }

        public bool GetPictureInfo(ReadOnlyMemory<byte> binary, out PictureInfo info)
        {
            if (Type != PluginType.ImportFilter)
            {
                throw new InvalidOperationException();
            }

            if (_func.ConfigurationDlg == null)
            {
                _func.GetPictureInfo = GetFunction<API.GetPictureInfo>(_handle);
            }

            const uint flag = API.Constant.OnMemory;

            unsafe
            {
                API.PictureInfo lpInfo;
                int result = 0;
                using (var handle = binary.Pin())
                {
                    result = _func.GetPictureInfo(handle.Pointer, binary.Length, flag, &lpInfo);
                }

                if (result == 0)
                {
                    info = new PictureInfo(&lpInfo, _stringConverter);
                }
                else
                {
                    info = default;
                }

                if (lpInfo.hInfo != null)
                {
                    // Spi_api.txt には Globalメモリーのハンドルと書いてあるが、実際の型は HGLOBAL ではなく、HLOCAL である。どっちだ？
                    NativeMethods.LocalFree(lpInfo.hInfo);
                }

                return result == 0;
            }
        }

        public bool GetPreview(ReadOnlyMemory<byte> binary, out byte[] image, out BitMapInfoHeader info)
        {
            if (Type != PluginType.ImportFilter)
            {
                throw new InvalidOperationException();
            }

            if (_func.GetPreview == null)
            {
                _func.GetPreview = GetFunction<API.GetPreview>(_handle);
            }

            image = null;
            info = default;

            const uint flag = API.Constant.OnMemory;

            unsafe
            {
                void* pHBInfo = null;
                void* pHBm = null;
                int result = 0;
                using (var handle = binary.Pin())
                {
                    result = _func.GetPreview(handle.Pointer, binary.Length, flag, &pHBInfo, &pHBm, AlwaysContinueProgressCallback, 0);
                }

                if (result == 0)
                {
                    if (pHBInfo != null)
                    {
                        var ptr = (BitMapInfoHeader*)NativeMethods.LocalLock(pHBInfo);

                        // copy to managed memory
                        Unsafe.Copy(ref info, ptr);

                        NativeMethods.LocalUnlock(pHBInfo);
                    }

                    if (pHBm != null)
                    {
                        var ptr = NativeMethods.LocalLock(pHBm);

                        // copy to managed memory
                        image = new byte[info.biSizeImage];
                        fixed (void* p = image)
                        {
                            Unsafe.CopyBlock(p, ptr, (uint)image.Length);
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
        }

        public bool GetArchiveInfo(ReadOnlyMemory<byte> binary, out FileInfo[] infos)
        {
            if (Type != PluginType.ArchiveExtractor)
            {
                throw new InvalidOperationException();
            }

            if (_func.GetArchiveInfo == null)
            {
                _func.GetArchiveInfo = GetFunction<API.GetArchiveInfo>(_handle);
            }

            infos = null;

            const uint flag = API.Constant.OnMemory;

            unsafe
            {
                void* lphInf = null;
                int result = 0;
                using (var handle = binary.Pin())
                {
                    result = _func.GetArchiveInfo(handle.Pointer, binary.Length, flag, &lphInf);
                }

                if (result == 0)
                {
                    if (lphInf != null)
                    {
                        var ptr = (API.FileInfo*)NativeMethods.LocalLock(lphInf);
                        uint count = 0;
                        {
                            var p = ptr;
                            while (p != null && p->method[0] != 0)
                            {
                                ++count;
                                ++p;
                            }
                        }

                        infos = new FileInfo[count];
                        for (int i = 0; i < infos.Length; ++i)
                        {
                            infos[i] = new FileInfo(&ptr[i], _stringConverter);
                        }

                        NativeMethods.LocalUnlock(lphInf);
                    }
                }

                if (lphInf != null)
                {
                    NativeMethods.LocalFree(lphInf);
                }

                return result == 0;
            }
        }

        public bool GetFileInfo(ReadOnlyMemory<byte> binary, string path, out FileInfo info, bool caseSensitive = false)
        {
            if (Type != PluginType.ArchiveExtractor)
            {
                throw new InvalidOperationException();
            }

            if (_func.GetFileInfo == null)
            {
                _func.GetFileInfo = GetFunction<API.GetFileInfo>(_handle);
            }

            uint flag = API.Constant.OnMemory | (caseSensitive ? API.Constant.CaseSensitive : API.Constant.CaseInsensitive);

            unsafe
            {
                API.FileInfo lpInfo;
                int result = 0;
                var mbpath = _stringConverter.Encode(path);
                using (var ptr = mbpath.Pin())
                {
                    using (var handle = binary.Pin())
                    {
                        result = _func.GetFileInfo(handle.Pointer, binary.Length, ptr.Pointer, flag, &lpInfo);
                    }
                }

                if (result == 0)
                {
                    info = new FileInfo(&lpInfo, _stringConverter);
                }
                else
                {
                    info = default;
                }

                return result == 0;
            }
        }

        public bool GetFile(ReadOnlyMemory<byte> binary, in FileInfo info, out byte[] file)
        {
            if (Type != PluginType.ArchiveExtractor)
            {
                throw new InvalidOperationException();
            }

            if (_func.GetFile == null)
            {
                _func.GetFile = GetFunction<API.GetFile>(_handle);
            }

            file = null;

            const uint flag = API.Constant.SrcOnMemory | API.Constant.DestOnMemory;

            unsafe
            {
                // ここで与えるバッファ範囲は [info.Position, info.CompSize) ではない！
                // プラグインによって動作することもあれば、失敗やハングアップすることもある
                // [info.Position, Lengh - info.Position) である
                var src = binary.Slice((int)info.Position);
                void* dest = null;
                int result = 0;
                using (var handle = src.Pin())
                {
                    result = _func.GetFile(handle.Pointer, src.Length, &dest, flag, AlwaysContinueProgressCallback, 0);
                }

                if (result == 0)
                {
                    if (dest != null)
                    {
                        file = new byte[info.FileSize];
                        var ptr = (API.FileInfo*)NativeMethods.LocalLock(dest);
                        fixed (void* p = file)
                        {
                            Unsafe.CopyBlock(p, ptr, (uint)file.Length);
                        }

                        NativeMethods.LocalUnlock(dest);
                    }
                }

                if (dest != null)
                {
                    NativeMethods.LocalFree(dest);
                }

                return result == 0;
            }
        }

        private static T GetFunction<T>(IntPtr handle)
        {
            // https://qiita.com/kenichiuda/items/613766f56e5ecd1de856
            var func = NativeLibrary.GetExport(handle, typeof(T).Name);
            return Marshal.GetDelegateForFunctionPointer<T>(func);
        }

        private void GetPluginVersion()
        {
            if (_func.GetPluginInfo == null)
            {
                _func.GetPluginInfo = GetFunction<API.GetPluginInfo>(_handle);
            }

            Span<byte> buf = stackalloc byte[16];
            unsafe
            {
                fixed (void* ptr = buf)
                {
                    var length = _func.GetPluginInfo(API.Constant.PluginVersion, ptr, buf.Length);

                    // 4byts固定のはずだが、終端を含めてさらに余分に返すケースがある, 6 byteまで確認
                    if (length < 4)
                    {
                        throw new Exception("failed to get plugin info");
                    }
                }
            }

            // ascii number 0 to 9
            // sjisだが二桁のascii数字なので、そのままオフセットして求める
            _version = ((buf[0] - 0x30) * 10) + (buf[1] - 0x30);

            // ascii alphabet A to Z
            // sjisだが、ascii範囲内なので、そのままキャストして判別する
            switch ((char)buf[2])
            {
                case 'I':
                    Type = PluginType.ImportFilter;
                    break;
                case 'X':
                    Type = PluginType.ExportFilter;
                    break;
                case 'A':
                    Type = PluginType.ArchiveExtractor;
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

            if (Type == PluginType.ImportFilter && _pluginTarget == PluginTarget.Normal)
            {
                // 00IN plugin
            }
            else if (Type == PluginType.ArchiveExtractor && _pluginTarget == PluginTarget.MultiPicture)
            {
                // 00AM plugin
            }
            else
            {
                // ? 仕様上は作れるが存在するのか？
                throw new NotSupportedException();
            }
        }

        private void GetPluginName()
        {
            if (_pluginName != null)
            {
                return;
            }

            if (_func.GetPluginInfo == null)
            {
                _func.GetPluginInfo = GetFunction<API.GetPluginInfo>(_handle);
            }

            unsafe
            {
                Span<byte> buf = stackalloc byte[128];
                int length = 0;
                fixed (void* ptr = buf)
                {
                    length = _func.GetPluginInfo(API.Constant.PluginName, ptr, buf.Length);
                }

                if (length > 0)
                {
                    _pluginName = _stringConverter.Decode(buf.Slice(0, length));
                }
                else
                {
                    // Not Implemented
                    // _pluginName = "Not Implemented";
                    throw new Exception("failed to get plugin name");
                }
            }
        }

        // これで取得できるのは、open dialogに表示する用の拡張子であって、実際にサポートしているかどうかは別
        private void GetFileFormats()
        {
            if (_fileFormats != null)
            {
                return;
            }

            if (_func.GetPluginInfo == null)
            {
                _func.GetPluginInfo = GetFunction<API.GetPluginInfo>(_handle);
            }

            _fileFormats = new List<Tuple<string, string>>();

            unsafe
            {
                Span<byte> buf = stackalloc byte[128];

                fixed (void* ptr = buf)
                {
                    int offset = 0;
                    int length = 0;
                    do
                    {
                        length = _func.GetPluginInfo(API.Constant.PluginFileFormat + offset, ptr, buf.Length);
                        if (length > 0)
                        {
                            var ext = _stringConverter.Decode(buf.Slice(0, length));
                            length = _func.GetPluginInfo(API.Constant.PluginFileFormat + offset + 1, ptr, buf.Length);
                            if (length > 0)
                            {
                                var format = _stringConverter.Decode(buf.Slice(0, length));
                                _fileFormats.Add(new Tuple<string, string>(ext, format));
                            }
                            else
                            {
                                // ペアで存在するはず
                                throw new Exception("failed to get plugin file format name");
                            }

                            offset += 2;
                        }
                    }
                    while (length > 0);
                }
            }
        }
    }
}
