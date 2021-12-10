// (c) longod, MIT License

// TODO WA.Susie に分離する

namespace WA.Susie
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    // todo
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

        private const int _pluginVersionNum = 0;
        private const int _pluginNameNum = 1;
        private const int _pluginFileFormatNum = 2;
        private const int _minPeekSize = 2048; // 2 kbytes
        private const string _extension = ".spi";

        private readonly StringConverter _stringConverter = null;

        private IntPtr _handle;
        private int _version = 0;
        private PluginType _pluginType;
        private PluginTarget _pluginTarget;
        private string _pluginName;
        private Function _func;
        private List<Tuple<string, string>> _fileFormats;


        private static int AlwaysContinueProgressCallback(int nNum, int nDenom, int lData)
        {
            return 0; // always continue
        }

        public SusiePlugin(string path)
            : this(path, StringConverter.SJIS)
        {
        }

        public SusiePlugin(string path, StringConverter stringConverter)
        {
            _stringConverter = stringConverter;
            _handle = NativeLibrary.Load(path);

            GetPluginVersion();
            //GetPluginName();
            //GetFileFormats();
        }

        public void Dispose()
        {
            NativeLibrary.Free(_handle);
        }

        public bool IsSupported(string filepath, byte[] binary)
        {
            var path = _stringConverter.Encode(filepath);
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
                _func.IsSupported = GetFunction<API.IsSupported>(_handle);
            }

            var result = _func.IsSupported(path, peek);
            return result != 0;
        }

        public bool GetPicture(byte[] binary, out byte[] image, out BitMapInfoHeader info)
        {
            if (_func.GetPicture == null)
            {
                _func.GetPicture = GetFunction<API.GetPicture>(_handle);
            }
            const uint flag = 1;// 0: filehandle, 1:on memory

            image = null;
            info = default;

            unsafe
            {
                void* pHBInfo = null;
                void* pHBm = null;

                var result = _func.GetPicture(binary, binary.Length, flag, &pHBInfo, &pHBm, AlwaysContinueProgressCallback, 0);
                if (result == 0)
                {
                    if (pHBInfo != null)
                    {
                        var ptr = (BitMapInfoHeader*)NativeMethods.LocalLock(pHBInfo);

                        BitMapInfoHeader bi = default;
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

            byte[] buf = new byte[32]; // or stackalloc
            var length = _func.GetPluginInfo(_pluginVersionNum, buf, buf.Length);
            if (length < 4)
            {
                throw new Exception("failed to get plugin info");
            }

            length = 4; // 4byts固定だが、終端を含めてさらに余分に返すケースがある, 6 byteまで確認

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

            if (_func.GetPluginInfo == null)
            {
                _func.GetPluginInfo = GetFunction<API.GetPluginInfo>(_handle);
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
                //_pluginName = "Not Implemented";
                throw new Exception("failed to get plugin name");
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

            byte[] buf = new byte[256]; // or stackalloc

            int offset = 0;
            int length = 0;
            do
            {
                length = _func.GetPluginInfo(_pluginFileFormatNum + offset, buf, buf.Length);
                if (length > 0)
                {
                    var ext = _stringConverter.Decode(buf, length);
                    length = _func.GetPluginInfo(_pluginFileFormatNum + offset + 1, buf, buf.Length);
                    if (length > 0)
                    {
                        var format = _stringConverter.Decode(buf, length);
                        _fileFormats.Add(new Tuple<string, string>(ext, format));
                    }
                    else
                    {
                        // ペアで存在するはず
                        throw new Exception("failed to get plugin file format name");
                    }
                    offset += 2;
                }
            } while (length > 0);
        }
    }
}
