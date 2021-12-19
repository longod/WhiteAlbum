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

        public bool Decode(FileLoader loader, out DecodedImage image)
        {
            switch (_plugin.Type)
            {
                case SusiePlugin.PluginType.ImportFilter:
                    return GetPicture(loader, out image);
                case SusiePlugin.PluginType.ArchiveExtractor:
                    GetArchiveInfo(loader); // test
                    break;
                case SusiePlugin.PluginType.ExportFilter:
                    throw new NotSupportedException();
                default:
                    throw new NotSupportedException();
            }

            image = null;
            return false;
        }

        private bool GetPicture(FileLoader loader, out DecodedImage image)
        {
            if (_plugin.GetPicture(loader.Binary, out var binary, out var info))
            {
                // index colorの場合、ファイルに埋まっているpaletteは、どこからとってきてる？
                // consider biCompression?
                image = new DecodedImage();
                image.Width = (uint)info.bmiHeader.biWidth;
                image.Height = (uint)info.bmiHeader.biHeight;
                image.DepthOrArray = 1;
                image.MipLevels = 1;
                image.BitsPerPixel = info.bmiHeader.biBitCount;
                switch (info.bmiHeader.biBitCount)
                {
                    case 32:
                        image.Format = DecodedImage.PixelFormat.BGRA;
                        break;
                    case 24:
                        image.Format = DecodedImage.PixelFormat.BGR;
                        break;
                    case 16:
                        image.Format = DecodedImage.PixelFormat.BGR;
                        break;
                    case 8:
                        image.Format = DecodedImage.PixelFormat.Index;
                        break;
                    case 4:
                        image.Format = DecodedImage.PixelFormat.Index;
                        break;
                    default:
                        throw new NotSupportedException($"info.biBitCount: {info.bmiHeader.biBitCount}");
                }

                image.Dimension = DecodedImage.ImageDimension.Texture2D;
                image.Orientation = DecodedImage.ImageOrientation.BottomLeft;
                image.Rotation = DecodedImage.ImageRotation.None;
                image.Binary = binary;
                if (info.bmiColors != null)
                {
                    image.Palette = info.bmiColors.Select(x => Color.FromRgb(x.rgbRed, x.rgbGreen, x.rgbBlue)).ToArray();
                }

                return true;
            }

            image = null;
            return false;
        }

        internal bool ShowConfigTest(IntPtr hWnd)
        {
            return _plugin.ConfigurationDlg(hWnd);
        }

        private bool GetArchiveInfo(FileLoader loader)
        {
            if (_plugin.GetArchiveInfo(loader.Binary, out var infos))
            {
                // test extract
                // _plugin.GetFile(loader.Binary, infos[0], out var dest);
                // _plugin.GetFileInfo(loader.Binary, infos[0].FileName, out var info);

                return true;
            }

            return false;
        }
    }
}
