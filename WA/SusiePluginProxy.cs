// (c) longod, MIT License

namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using WA.Susie;

    internal class SusiePluginProxy : IPluginProxy
    {
        private SusiePlugin _plugin;

        internal SusiePluginProxy(SusiePlugin plugin)
        {
            _plugin = plugin;
        }

        public void Dispose()
        {
            _plugin.Dispose();
        }

        public bool Decode(FileLoader loader, out DecodedImage image)
        {
            if (_plugin.GetPicture(loader.Binary, out var binary, out var info))
            {
                // index colorの場合、ファイルに埋まっているpaletteは、どこからとってきてる？
                // consider biCompression?
                image = new DecodedImage();
                image.Width = (uint)info.biWidth;
                image.Height = (uint)info.biHeight;
                image.DepthOrArray = 1;
                image.MipLevels = 1;
                image.BitsPerPixel = info.biBitCount;
                switch (info.biBitCount)
                {
                    case 24:
                        image.Format = DecodedImage.PixelFormat.BGR;
                        break;
                    case 32:
                        image.Format = DecodedImage.PixelFormat.BGRA;
                        break;
                    case 4:
                        image.Format = DecodedImage.PixelFormat.Index;
                        break;
                    default:
                        throw new NotSupportedException($"info.biBitCount: {info.biBitCount}");
                }
                image.Dimension = DecodedImage.ImageDimension.Texture2D;
                image.Orientation = DecodedImage.ImageOrientation.BottomLeft;
                image.Rotation = DecodedImage.ImageRotation.None;
                image.Binary = binary;
                return true;
            }

            image = null;
            return false;
        }

        public bool IsSupported(FileLoader loader)
        {
            // peek file
            return _plugin.IsSupported(loader.Path, loader.Binary);
        }
    }
}
