// (c) longod, MIT License

namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using WA.Susie;

    internal class SusiePluginDecoder : IDecoder
    {
        private SusiePlugin _plugin;

        internal SusiePluginDecoder(SusiePlugin plugin)
        {
            _plugin = plugin;
        }

        public void Dispose()
        {
            _plugin.Dispose();
        }

        public bool Decode(FileLoader loader, out IntermediateImage image)
        {
            byte[] binary = null;
            Susie.BitMapInfoHeader info;
            if (_plugin.GetPicture(loader.Binary, out binary, out info))
            {
                image = new IntermediateImage();
                image.desc.Width = (uint)info.biWidth;
                image.desc.Height = (uint)info.biHeight;
                image.desc.DepthOrArray = 1;
                image.desc.MipLevels = 1;
                // todo other descs
                image.binary = binary;
                return true;
            }

            image = null;
            return false;
        }

        public bool IsSupported(FileLoader loader)
        {
            return _plugin.IsSupported(loader.Path, loader.Binary);
        }
    }
}
