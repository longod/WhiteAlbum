// (c) longod, MIT License

namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class SusiePluginDecoder : IDecoder
    {
        private SusiePlugin _plugin;
        private readonly StringConverter _stringConverter = null;

        internal SusiePluginDecoder(SusiePlugin plugin)
        {
            _plugin = plugin;
            _stringConverter = StringConverter.SJIS;
        }

        public void Dispose()
        {
            _plugin.Dispose();
        }

        public bool Decode(FileLoader loader, out IntermediateImage image)
        {
            byte[] binary = null;
            Susie.BitMapInfo info;
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
            // FIXME 何度も変換されるので効率が悪い。キャッシュとか
            var path = _stringConverter.Encode(loader.Path);
            return _plugin.IsSupported(path, loader.Binary);
        }
    }
}
