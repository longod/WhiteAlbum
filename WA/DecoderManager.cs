namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows.Media.Imaging;
    using Microsoft.Extensions.Logging;

    public class DecoderManager
    {
        private readonly ILogger _logger;
        private readonly PluginManager _pluginManager;
        private Dictionary<string, ImageDecoder> _decoders = new Dictionary<string, ImageDecoder>();

        public DecoderManager(AppSettings settings, PluginManager pluginManager, ILogger logger)
        {
            _logger = logger;
            _pluginManager = pluginManager;
            if (settings.Data.EnableBuiltInDecoders)
            {
                RegisterBuiltInDecoders();
            }
        }

        internal ImageDecoder FindDecoder(FileLoader loader)
        {
            // find decoder extension and header

            // 拡張子にマッピングされたデコーダーがヒットするかどうか
            var ext = loader.Extension;
            ImageDecoder instance = null;
            if (_decoders.TryGetValue(ext, out instance))
            {
                bool continueFinding = false; // みつかっても残りのプラグインを調べるかどうか
                if (!continueFinding)
                {
                    return instance;
                }
            }

            // ヒットしない、プラグインで該当するかどうか解決を試みる
            // 解決できる場合は、対応する拡張子にマッピングする
            // この拡張子でマッピング済みということがplugin maneger側にも分からないと、continueFinding時に同じものがでてくる
            var decoder = _pluginManager.FindDecodablePlugin(loader);
            if (decoder != null)
            {
                if (instance == null)
                {
                    instance = new ImageDecoder();
                }

                instance.RegisterDecoder(decoder);
                _decoders.Add(ext, instance);
            }

            return instance;
        }

        private void RegisterBuiltInDecoders()
        {
            _decoders.Add(".bmp", new BuiltInImageDecoder(typeof(BmpBitmapDecoder)));
            _decoders.Add(".png", new BuiltInImageDecoder(typeof(PngBitmapDecoder)));
            _decoders.Add(".jpg", new BuiltInImageDecoder(typeof(JpegBitmapDecoder)));
            _decoders.Add(".jpeg", _decoders[".jpg"]);
            _decoders.Add(".gif", new BuiltInImageDecoder(typeof(GifBitmapDecoder)));
            _decoders.Add(".tif", new BuiltInImageDecoder(typeof(TiffBitmapDecoder)));
            _decoders.Add(".tiff", _decoders[".tif"]);
            _decoders.Add(".hdp", new BuiltInImageDecoder(typeof(WmpBitmapDecoder)));
            _decoders.Add(".wdp", _decoders[".hdp"]);
        }

    }
}
