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
        private Dictionary<string, ImageDecoder> _imageDecoders = new Dictionary<string, ImageDecoder>();

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
            if (_imageDecoders.TryGetValue(ext, out instance))
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
                _imageDecoders.Add(ext, instance);
            }

            return instance;
        }

        private void RegisterBuiltInDecoders()
        {
            _imageDecoders.Add(".bmp", new BuiltInImageDecoder(typeof(BmpBitmapDecoder)));
            _imageDecoders.Add(".png", new BuiltInImageDecoder(typeof(PngBitmapDecoder)));
            _imageDecoders.Add(".jpg", new BuiltInImageDecoder(typeof(JpegBitmapDecoder)));
            _imageDecoders.Add(".jpeg", _imageDecoders[".jpg"]);
            _imageDecoders.Add(".gif", new BuiltInImageDecoder(typeof(GifBitmapDecoder)));
            _imageDecoders.Add(".tif", new BuiltInImageDecoder(typeof(TiffBitmapDecoder)));
            _imageDecoders.Add(".tiff", _imageDecoders[".tif"]);
            _imageDecoders.Add(".hdp", new BuiltInImageDecoder(typeof(WmpBitmapDecoder)));
            _imageDecoders.Add(".wdp", _imageDecoders[".hdp"]);
        }

    }
}
