namespace WA
{
    using System;
    using System.Linq;
    using System.Windows.Media;
    using WA.Susie;

    internal class SusiePluginProxy : IPluginProxy
    {
        private SusiePlugin _plugin;
        private bool _disposed = false;

        internal SusiePluginProxy(SusiePlugin plugin)
        {
            _plugin = plugin;
        }

        ~SusiePluginProxy()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // managed
                }

                // unmanaged
                _plugin?.Dispose();
                _plugin = null;

                _disposed = true;
            }
        }

        public bool IsSupported(FileLoader loader)
        {
            // peek file
            return _plugin.IsSupported(loader.Path, loader.RawBinary);
        }

        public bool Decode(FileLoader loader, out IIntermediateResult result)
        {
            switch (_plugin.Type)
            {
                case SusiePlugin.PluginType.ImportFilter:
                    if (GetPicture(loader, out var image))
                    {
                        result = image;
                        return true;
                    }

                    break;
                case SusiePlugin.PluginType.ArchiveExtractor:
                    if (GetArchiveInfo(loader, out var files))
                    {
                        result = files;
                        return true;
                    }

                    break;
                case SusiePlugin.PluginType.ExportFilter:
                    throw new NotSupportedException();
                default:
                    throw new NotSupportedException();
            }

            result = null;
            return false;
        }

        public bool Decode(FileLoader loader, PackedFile packed, out IIntermediateResult result)
        {
            switch (_plugin.Type)
            {
                case SusiePlugin.PluginType.ArchiveExtractor:
                    if (GetFile(loader, packed.FileOffset, packed.FileSize, out var file))
                    {
                        result = new BinaryIntermediateResult() { Binary = file };
                        return true;
                    }

                    break;
                default:
                    throw new NotSupportedException();
            }

            result = null;
            return false;
        }

        public bool Decode(FileLoader loader, string path, out IIntermediateResult result)
        {
            switch (_plugin.Type)
            {
                case SusiePlugin.PluginType.ArchiveExtractor:
                    if (GetFileInfo(loader, path, out var info))
                    {
                        PackedFile packed = ToPackedFile(info);
                        return Decode(loader, packed, out result);
                    }

                    break;
                default:
                    throw new NotSupportedException();
            }

            result = null;
            return false;

        }

        internal bool ShowConfigTest(IntPtr hWnd)
        {
            return _plugin.ConfigurationDlg(hWnd);
        }

        private static PackedFile ToPackedFile(in FileInfo info)
        {
            var packed = new PackedFile();
            packed.Path = System.IO.Path.Combine(info.Path, info.FileName);
            packed.FileOffset = info.Position;
            packed.PackedSize = info.CompSize;
            packed.FileSize = info.FileSize;
            packed.Date = info.Timestamp;
            return packed;
        }

        private bool GetPicture(FileLoader loader, out ImageIntermediateResult result)
        {
            if (_plugin.GetPicture(loader.Binary, out var binary, out var info))
            {
                // index colorの場合、ファイルに埋まっているpaletteは、どこからとってきてる？
                // consider biCompression?
                result = new ImageIntermediateResult();
                result.Info.Width = (uint)info.bmiHeader.biWidth;
                result.Info.Height = (uint)info.bmiHeader.biHeight;
                result.Info.DepthOrArray = 1;
                result.Info.MipLevels = 1;
                result.Info.BitsPerPixel = info.bmiHeader.biBitCount;
                switch (info.bmiHeader.biBitCount)
                {
                    case 32:
                        result.Info.Format = ImageFormat.BGRA;
                        break;
                    case 24:
                        result.Info.Format = ImageFormat.BGR;
                        break;
                    case 16:
                        result.Info.Format = ImageFormat.BGR;
                        break;
                    case 8:
                        result.Info.Format = ImageFormat.Index;
                        break;
                    case 4:
                        result.Info.Format = ImageFormat.Index;
                        break;
                    default:
                        throw new NotSupportedException($"info.biBitCount: {info.bmiHeader.biBitCount}");
                }

                result.Info.Dimension = ImageDimension.Texture2D;
                result.Info.Orientation = ImageOrientation.BottomLeft;
                result.Info.Rotation = ImageRotation.None;
                result.Binary = binary;
                if (info.bmiColors != null)
                {
                    result.Palette = info.bmiColors.Select(x => Color.FromRgb(x.rgbRed, x.rgbGreen, x.rgbBlue)).ToArray();
                }

                return true;
            }

            result = null;
            return false;
        }

        private bool GetArchiveInfo(FileLoader loader, out ArchiveIntermediateResult result)
        {
            if (_plugin.GetArchiveInfo(loader.Binary, out var infos))
            {
                // test extract
                // _plugin.GetFile(loader.Binary, infos[0], out var dest);
                // _plugin.GetFileInfo(loader.Binary, infos[0].FileName, out var info);
                result = new ArchiveIntermediateResult();
                result.files = new PackedFile[infos.Length];

                // todo optimize
                for (int i = 0; i < result.files.Length; ++i)
                {
                    result.files[i] = ToPackedFile(infos[i]);
                }

                return true;
            }

            result = null;
            return false;
        }

        private bool GetFile(FileLoader loader, long offset, long fileSize, out byte[] result)
        {
            return _plugin.GetFile(loader.Binary, (uint)offset, (uint)fileSize, out result);
        }

        private bool GetFileInfo(FileLoader loader, string path, out FileInfo info)
        {
            return _plugin.GetFileInfo(loader.Binary, path, out info);
        }
    }
}
