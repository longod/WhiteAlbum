namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media.Imaging;
    using Microsoft.Extensions.Logging;

    public class EncoderManager
    {
        private readonly ILogger _logger;
        private Dictionary<string, BuiltInImageEncoder> _encoders = null;
        private string _exportFilter = null;

        public string ExportFilter
        {
            get
            {
                if (_exportFilter == null)
                {
                    if (_encoders == null)
                    {
                        _encoders = new Dictionary<string, BuiltInImageEncoder>();
                        RegisterBuiltinEncoders();
                    }

                    // | 区切りのフィルタ生成
                    // 複数拡張子は ; 区切り
                    // fixme 複数の拡張子を表現できない
                    var filter = _encoders.Select(x => string.Concat(x.Value.FormatName, "|", "*", x.Key));
                    _exportFilter = string.Join('|', filter);
                }

                return _exportFilter;
            }
        }

        public EncoderManager(AppSettings settings, PluginManager pluginManager, ILogger logger)
        {
            _logger = logger;
        }

        internal BuiltInImageEncoder FindEncoder(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                // return default or failed
                throw new ArgumentException(nameof(extension));
            }

            if (_encoders == null)
            {
                _encoders = new Dictionary<string, BuiltInImageEncoder>();
                RegisterBuiltinEncoders();
            }

            // find encoder
            // todo tweakable quality
            if (_encoders.TryGetValue(extension, out var encoder))
            {
                return encoder;
            }

            return null;
        }

        private void RegisterBuiltinEncoders()
        {
            _encoders.Add(".bmp", new BuiltInImageEncoder("Bitmap", new BmpBitmapEncoder()));
            _encoders.Add(".png", new BuiltInImageEncoder("PNG", new PngBitmapEncoder() { Interlace = PngInterlaceOption.Default }));
            _encoders.Add(".jpg", new BuiltInImageEncoder("Jpeg", new JpegBitmapEncoder() { QualityLevel = 100 }));
            _encoders.Add(".gif", new BuiltInImageEncoder("GIF", new GifBitmapEncoder()));
            _encoders.Add(".tif", new BuiltInImageEncoder("TIFF", new TiffBitmapEncoder() { Compression = TiffCompressOption.Default }));
            _encoders.Add(".hdp", new BuiltInImageEncoder("HDP", new WmpBitmapEncoder()));
        }

    }
}
