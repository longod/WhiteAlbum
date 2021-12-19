namespace WA.Susie
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    // todo susie以外のプラグインに対応する場合にインターフェイスを揃えたい
    internal interface IPlugin
    {
    }

    public class SusiePlugin : IPlugin, IDisposable
    {
        public enum PluginType
        {
            ImportFilter,
            ArchiveExtractor,
            ExportFilter, // 存在しない？
        }

        public enum PluginTarget
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

        private readonly StringConverter _stringConverter;

        private IntPtr _handle;
        private bool _disposed = false;
        private string _name;
        private Function _func;
        private List<Tuple<string, string>> _fileFormats;

        public int Version { get; private set; }

        public PluginType Type { get; private set; }

        public PluginTarget Target { get; private set; }

        public string Name
        {
            get
            {
                return GetPluginName();
            }
        }

        public IEnumerable<Tuple<string, string>> FileFormats
        {
            get
            {
                var formats = GetFileFormats();
                foreach (var f in formats)
                {
                    yield return f;
                }
            }
        }

        private static int AlwaysContinueProgressCallback(int nNum, int nDenom, int lData)
        {
            return 0; // always continue
        }

        public SusiePlugin(string path, StringConverter stringConverter)
        {
            _stringConverter = stringConverter;
            _handle = NativeLibrary.Load(path);
            GetPluginVersion();
        }

        ~SusiePlugin()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool IsSupported(string path, ReadOnlyMemory<byte> binary)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path must not be empty.");
            }

            if (binary.IsEmpty)
            {
                throw new ArgumentNullException("binary must not be empty.");
            }

            if (binary.Length < API.Constant.MinFileSize)
            {
                throw new ArgumentException($"binary size larger than {API.Constant.MinFileSize} (has {binary.Length}).");
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

        public bool ConfigurationDlg(IntPtr hWnd)
        {
            if (hWnd == default)
            {
                throw new ArgumentException("hWnd must be valid.");
            }

            if (_func.ConfigurationDlg == null)
            {
                try
                {
                    _func.ConfigurationDlg = GetFunction<API.ConfigurationDlg>(_handle);
                }
                catch (EntryPointNotFoundException)
                {
                    // optional function
                    return false;
                }
            }

            unsafe
            {
                const int fnc = (int)API.Constant.DialogSettings;
                var result = _func.ConfigurationDlg(hWnd.ToPointer(), fnc);
                return result == 0;
            }
        }

        public bool GetPicture(ReadOnlyMemory<byte> binary, out byte[] image, out BitMapInfo info)
        {
            if (Type != PluginType.ImportFilter)
            {
                throw new InvalidOperationException();
            }

            if (binary.IsEmpty)
            {
                throw new ArgumentNullException("binary must not be empty.");
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

                return PostProcessPicture(result, pHBInfo, pHBm, ref image, ref info);
            }
        }

        public bool GetPictureInfo(ReadOnlyMemory<byte> binary, out PictureInfo info)
        {
            if (Type != PluginType.ImportFilter)
            {
                throw new InvalidOperationException();
            }

            if (binary.IsEmpty)
            {
                throw new ArgumentNullException("binary must not be empty.");
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

        public bool GetPreview(ReadOnlyMemory<byte> binary, out byte[] image, out BitMapInfo info)
        {
            if (Type != PluginType.ImportFilter)
            {
                throw new InvalidOperationException();
            }

            if (binary.IsEmpty)
            {
                throw new ArgumentNullException("binary must not be empty.");
            }

            if (_func.GetPreview == null)
            {
                try
                {
                    _func.GetPreview = GetFunction<API.GetPreview>(_handle);
                }
                catch (EntryPointNotFoundException)
                {
                    // optional function
                    image = null;
                    info = default;
                    return false;
                }
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

                return PostProcessPicture(result, pHBInfo, pHBm, ref image, ref info);
            }
        }

        public bool GetArchiveInfo(ReadOnlyMemory<byte> binary, out FileInfo[] infos)
        {
            if (Type != PluginType.ArchiveExtractor)
            {
                throw new InvalidOperationException();
            }

            if (binary.IsEmpty)
            {
                throw new ArgumentNullException("binary must not be empty.");
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

            if (binary.IsEmpty)
            {
                throw new ArgumentNullException("binary must not be empty.");
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path must not be empty.");
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

            if (binary.IsEmpty)
            {
                throw new ArgumentNullException("binary must not be empty.");
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

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // managed
                }

                // unmanaged
                Free();

                _disposed = true;
            }
        }

        private void Free()
        {
            NativeLibrary.Free(_handle);
            _handle = default;
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
                        throw new SusieException("Failed to get plugin info.");
                    }
                }
            }

            // ascii number 0 to 9
            // sjisだが二桁のascii数字なので、そのままオフセットして求める
            Version = ((buf[0] - 0x30) * 10) + (buf[1] - 0x30);

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
                    throw new SusieException($"Failed to get plugin version: {buf[2]}.");
            }

            switch ((char)buf[3])
            {
                case 'N':
                    Target = PluginTarget.Normal;
                    break;
                case 'M':
                    Target = PluginTarget.MultiPicture;
                    break;
                default:
                    throw new SusieException($"Failed to get plugin version: {buf[3]}");
            }

            if (Type == PluginType.ImportFilter && Target == PluginTarget.Normal)
            {
                // 00IN plugin
            }
            else if (Type == PluginType.ArchiveExtractor && Target == PluginTarget.MultiPicture)
            {
                // 00AM plugin
            }
            else
            {
                // ? 仕様上は作れるが存在するのか？
                throw new NotSupportedException($"invalid plugin type: {Type} and {Target}");
            }
        }

        private string GetPluginName()
        {
            if (_name != null)
            {
                return _name;
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
                    _name = _stringConverter.Decode(buf.Slice(0, length));
                }
                else
                {
                    // Not Implemented
                    // _pluginName = "Not Implemented";
                    throw new SusieException("Failed to get plugin name.");
                }
            }

            return _name;
        }

        // これで取得できるのは、open dialogに表示する用の拡張子であって、実際にサポートしているかどうかは別
        private List<Tuple<string, string>> GetFileFormats()
        {
            if (_fileFormats != null)
            {
                return _fileFormats;
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
                                throw new SusieException($"Failed to get plugin file format with {ext}");
                            }

                            offset += 2;
                        }
                    }
                    while (length > 0);
                }
            }

            return _fileFormats;
        }

        private unsafe bool PostProcessPicture(int result, void* pHBInfo, void* pHBm, ref byte[] image, ref BitMapInfo info)
        {
            if (result == 0)
            {
                if (pHBInfo != null)
                {
                    var ptr = (BitMapInfoHeader*)NativeMethods.LocalLock(pHBInfo);

                    // copy to managed memory
                    Unsafe.Copy(ref info.bmiHeader, ptr);

                    // gettin palette
                    // biClrUsed は適切に設定されていないので自力で判定する
                    // TODO validate header
                    var bitCount = info.bmiHeader.biBitCount;
                    if (bitCount < 24)
                    {
                        var clrUsed = 1 << bitCount;
                        info.bmiColors = new RGBQuad[clrUsed];
                        Span<RGBQuad> palette = info.bmiColors.AsSpan();

                        fixed (void* dest = palette)
                        {
                            var src = ptr + 1; // next
                            Unsafe.CopyBlock(dest, src, (uint)(sizeof(RGBQuad) * palette.Length));
                        }
                    }

                    NativeMethods.LocalUnlock(pHBInfo);
                }

                if (pHBm != null)
                {
                    var ptr = NativeMethods.LocalLock(pHBm);

                    // copy to managed memory
                    image = new byte[info.bmiHeader.biSizeImage];
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
}
